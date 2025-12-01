using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onboarding.Core.Domain;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Validation;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onboarding.Api.Controllers
{
    /// <summary>
    /// Controlador para gestionar prospectos.
    /// Aplica OCP: Open/Closed Principle - Extensible sin modificar PubSubController.
    /// </summary>
    [ApiController]
    [Route("api/prospects")]
    public class ProspectController : ControllerBase
    {
        private readonly IProspectDataService _prospectDataService;
        private readonly IProspectStatusService _prospectStatusService;
        private readonly IRepository<Prospect> _prospectRepo;
        private readonly IRepository<Phase> _phaseRepo;
        private readonly IRepository<Step> _stepRepo;
        private readonly IWorkflowRoutingService _workflowRoutingService;
        private readonly IUserManagementService _userManagementService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventPublisher _eventPublisher;
        private readonly IValidationOrchestrator _validationOrchestrator;
        private readonly ILogger<ProspectController> _logger;

        public ProspectController(
            IProspectDataService prospectDataService,
            IProspectStatusService prospectStatusService,
            IRepository<Prospect> prospectRepo,
            IRepository<Phase> phaseRepo,
            IRepository<Step> stepRepo,
            IWorkflowRoutingService workflowRoutingService,
            IUserManagementService userManagementService,
            IUnitOfWork unitOfWork,
            IEventPublisher eventPublisher,
            IValidationOrchestrator validationOrchestrator,
            ILogger<ProspectController> logger)
        {
            _prospectDataService = prospectDataService;
            _prospectStatusService = prospectStatusService;
            _prospectRepo = prospectRepo;
            _phaseRepo = phaseRepo;
            _stepRepo = stepRepo;
            _workflowRoutingService = workflowRoutingService;
            _userManagementService = userManagementService;
            _unitOfWork = unitOfWork;
            _eventPublisher = eventPublisher;
            _validationOrchestrator = validationOrchestrator;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo prospecto (comando REST para el wizard).
        /// Este es un comando sincrónico que retorna el prospectId inmediatamente.
        /// Opcionalmente emite eventos para que el orquestador ejecute reglas de negocio.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProspect([FromBody] CreateProspectRequest request)
        {
            _logger.LogInformation("Creating prospect for email: {Email}", request.Email);

            try
            {
                // 1. Construir payload para determinar workflow
                var payloadJson = JsonSerializer.Serialize(new
                {
                    email = request.Email,
                    app_id = request.AppId,
                    client_type = request.ClientType,
                    country = request.Country ?? "MX"
                });

                // 2. Determinar workflow mediante reglas de ruteo
                var workflowId = await _workflowRoutingService.DetermineWorkflowAsync(payloadJson);

                if (workflowId == 0)
                {
                    _logger.LogWarning("No workflow found for: AppId={AppId}, ClientType={ClientType}",
                        request.AppId, request.ClientType);
                    return BadRequest(new
                    {
                        error = "No se encontró un workflow válido para estos datos",
                        appId = request.AppId,
                        clientType = request.ClientType
                    });
                }

                // 3. Crear o recuperar usuario
                var user = await _userManagementService.GetOrCreateUserAsync(request.Email, payloadJson);
                _logger.LogInformation("User resolved: {UserId} ({Email})", user.UserId, user.Email);

                // 4. Ejecutar el pipeline de validacion
                var validationContext = new ValidationContext
                {
                    Email = request.Email,
                    WorkflowId = workflowId
                };

                var validationResult = await _validationOrchestrator.ExecutePipelineAsync("USER_REGISTRATION_PIPELINE", validationContext);

                if (!validationResult.Success)
                {
                    var errors = validationResult.StepResults
                        .Where(step => !step.IsValid)
                        .Select(step => new
                        {
                            step.ProviderKey,
                            step.Message,
                            step.Severity
                        })
                        .ToList();

                    _logger.LogWarning("Validation failed for email: {Email}. Errors: {Errors}", request.Email, errors);

                    return BadRequest(new
                    {
                        error = "Validation failed",
                        validationErrors = errors
                    });
                }

                // 5. Obtener el primer step del workflow
                var firstPhase = (await _phaseRepo.GetAllAsync())
                    .Where(p => p.WorkflowId == workflowId)
                    .OrderBy(p => p.Order)
                    .FirstOrDefault();

                int? firstStepId = null;
                if (firstPhase != null)
                {
                    var firstStep = (await _stepRepo.GetAllAsync())
                        .Where(s => s.PhaseId == firstPhase.PhaseId)
                        .OrderBy(s => s.Order)
                        .FirstOrDefault();

                    if (firstStep != null)
                    {
                        firstStepId = firstStep.StepId;
                        _logger.LogInformation(
                            "First step determined: StepId={StepId}, StepName={StepName}",
                            firstStep.StepId, firstStep.Name);
                    }
                    else
                    {
                        _logger.LogWarning("No steps found for first phase of Workflow {WorkflowId}", workflowId);
                    }
                }
                else
                {
                    _logger.LogWarning("No phases found for Workflow {WorkflowId}", workflowId);
                }

                // 6. Crear el prospecto con el primer step asignado
                var prospect = new Prospect
                {
                    ProspectId = Guid.NewGuid(),
                    UserId = user.UserId,
                    WorkflowId = workflowId,
                    Status = "STARTED",
                    CurrentStepId = firstStepId,  // Asignar el primer step del workflow
                    CreatedAt = DateTime.UtcNow
                };

                await _prospectRepo.AddAsync(prospect);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Prospect created successfully: ProspectId={ProspectId}, WorkflowId={WorkflowId}, CurrentStepId={CurrentStepId}",
                    prospect.ProspectId, workflowId, firstStepId);

                // 7. Inicializar ProspectData con datos iniciales
                await _prospectDataService.UpdateProspectDataAsync(prospect.ProspectId, payloadJson);
                _logger.LogInformation("Initial ProspectData created for {ProspectId}", prospect.ProspectId);

                // 8. (Opcional) Emitir evento para que se ejecuten reglas de negocio
                // Este evento NO bloquea la respuesta
                await _eventPublisher.PublishAsync("UserRegistered", new
                {
                    prospect_id = prospect.ProspectId,
                    user_id = user.UserId,
                    workflow_id = workflowId,
                    email = request.Email,
                    app_id = request.AppId,
                    client_type = request.ClientType,
                    country = request.Country
                });

                // 9. Retornar respuesta inmediata con los datos necesarios
                return Ok(new
                {
                    prospectId = prospect.ProspectId,
                    userId = user.UserId,
                    workflowId = workflowId,
                    currentStepId = firstStepId,  // Incluir el stepId actual
                    status = prospect.Status,
                    email = user.Email,
                    createdAt = prospect.CreatedAt,
                    message = "Prospecto creado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prospect for email: {Email}", request.Email);
                return StatusCode(500, new
                {
                    error = "Error al crear el prospecto",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene los datos completos de un prospecto
        /// </summary>
        [HttpGet("{prospectId}")]
        public async Task<IActionResult> GetProspect(Guid prospectId)
        {
            _logger.LogInformation("Getting prospect data for ProspectId: {ProspectId}", prospectId);

            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                _logger.LogWarning("Prospect not found: {ProspectId}", prospectId);
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            var data = await _prospectDataService.GetProspectDataAsync(prospectId);

            return Ok(new
            {
                prospectId = prospect.ProspectId,
                userId = prospect.UserId,
                workflowId = prospect.WorkflowId,
                status = prospect.Status,
                currentStepId = prospect.CurrentStepId,
                createdAt = prospect.CreatedAt,
                updatedAt = prospect.UpdatedAt,
                data = data
            });
        }

        /// <summary>
        /// Obtiene solo los datos JSON de un prospecto
        /// </summary>
        [HttpGet("{prospectId}/data")]
        public async Task<IActionResult> GetProspectData(Guid prospectId)
        {
            _logger.LogInformation("Getting prospect data for ProspectId: {ProspectId}", prospectId);

            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            var data = await _prospectDataService.GetProspectDataAsync(prospectId);

            return Ok(new
            {
                prospectId = prospectId,
                data = data,
                retrievedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Obtiene el historial de cambios de estado de un prospecto
        /// </summary>
        [HttpGet("{prospectId}/status-history")]
        public async Task<IActionResult> GetStatusHistory(Guid prospectId)
        {
            _logger.LogInformation("Getting status history for ProspectId: {ProspectId}", prospectId);

            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            var history = await _prospectStatusService.GetStatusHistoryAsync(prospectId);

            return Ok(new
            {
                prospectId = prospectId,
                currentStatus = prospect.Status,
                historyCount = history.Count,
                history = history.Select(h => new
                {
                    historyId = h.HistoryId,
                    oldStatus = h.OldStatus,
                    newStatus = h.NewStatus,
                    reason = h.Reason,
                    changedBy = h.ChangedBy,
                    changedAt = h.ChangedAt,
                    metadata = h.Metadata
                })
            });
        }

        /// <summary>
        /// Obtiene las transiciones de estado disponibles para un prospecto
        /// </summary>
        [HttpGet("{prospectId}/available-transitions")]
        public async Task<IActionResult> GetAvailableTransitions(Guid prospectId)
        {
            _logger.LogInformation("Getting available transitions for ProspectId: {ProspectId}", prospectId);

            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            var currentStatus = await _prospectStatusService.GetCurrentStatusAsync(prospectId);
            var availableTransitions = await _prospectStatusService.GetAvailableTransitionsAsync(prospectId);
            var isTerminal = ProspectStateMachine.IsTerminalState(currentStatus ?? "");

            return Ok(new
            {
                prospectId = prospectId,
                currentStatus = currentStatus,
                isTerminalState = isTerminal,
                availableTransitions = availableTransitions
            });
        }

        /// <summary>
        /// Actualiza manualmente el estado de un prospecto
        /// </summary>
        [HttpPut("{prospectId}/status")]
        public async Task<IActionResult> UpdateStatus(
            Guid prospectId,
            [FromBody] UpdateStatusRequest request)
        {
            _logger.LogInformation(
                "Updating status for ProspectId: {ProspectId} to {NewStatus}",
                prospectId, request.NewStatus);

            try
            {
                var success = await _prospectStatusService.UpdateStatusAsync(
                    prospectId,
                    request.NewStatus,
                    request.Reason,
                    request.ChangedBy ?? "API_USER",
                    request.Metadata);

                if (success)
                {
                    return Ok(new
                    {
                        message = "Estado actualizado exitosamente",
                        prospectId = prospectId,
                        newStatus = request.NewStatus
                    });
                }

                return BadRequest(new { error = "No se pudo actualizar el estado" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid status transition for ProspectId: {ProspectId}", prospectId);
                return BadRequest(new
                {
                    error = "Transición de estado inválida",
                    detail = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// DTO para crear un nuevo prospecto (comando del wizard)
    /// </summary>
    public class CreateProspectRequest
    {
        public string Email { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
        public string ClientType { get; set; } = string.Empty;
        public string? Country { get; set; }
    }

    /// <summary>
    /// DTO para actualizar el estado de un prospecto
    /// </summary>
    public class UpdateStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? ChangedBy { get; set; }
        public string? Metadata { get; set; }
    }
}
