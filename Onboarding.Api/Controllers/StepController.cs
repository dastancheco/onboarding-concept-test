using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onboarding.Api.Controllers
{
    /// <summary>
    /// Controlador para gestionar la ejecución paso a paso del onboarding.
    /// </summary>
    [ApiController]
    [Route("api/prospects/{prospectId}/steps")]
    public class StepController(
        IRepository<Prospect> prospectRepo,
        IRepository<Step> stepRepo,
        IRepository<Phase> phaseRepo,
        IStepValidationService validationService,
        IProspectDataService prospectDataService,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<StepController> logger) : ControllerBase
    {
        private readonly IRepository<Prospect> _prospectRepo = prospectRepo;
        private readonly IRepository<Step> _stepRepo = stepRepo;
        private readonly IRepository<Phase> _phaseRepo = phaseRepo;
        private readonly IStepValidationService _validationService = validationService;
        private readonly IProspectDataService _prospectDataService = prospectDataService;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IEventPublisher _eventPublisher = eventPublisher;
        private readonly ILogger<StepController> _logger = logger;

        /// <summary>
        /// Obtiene información del step actual del prospecto
        /// </summary>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentStep(Guid prospectId)
        {
            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            if (!prospect.CurrentStepId.HasValue)
            {
                return Ok(new
                {
                    prospectId = prospectId,
                    message = "No hay step activo. El onboarding puede estar completado o no iniciado."
                });
            }

            var step = await _stepRepo.GetByIdAsync(prospect.CurrentStepId.Value);
            if (step == null)
            {
                return NotFound(new { error = "Step no encontrado" });
            }

            var phase = await _phaseRepo.GetByIdAsync(step.PhaseId);

            return Ok(new
            {
                prospectId = prospectId,
                stepId = step.StepId,
                stepName = step.Name,
                stepOrder = step.Order,
                phaseId = phase?.PhaseId,
                phaseName = phase?.Name,
                phaseOrder = phase?.Order
            });
        }

        /// <summary>
        /// Obtiene todos los steps del workflow del prospecto
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllSteps(Guid prospectId)
        {
            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            var phases = (await _phaseRepo.GetAllAsync())
                .Where(p => p.WorkflowId == prospect.WorkflowId)
                .OrderBy(p => p.Order)
                .ToList();

            var allSteps = await _stepRepo.GetAllAsync();
            
            var workflow = phases.Select(phase => new
            {
                phaseId = phase.PhaseId,
                phaseName = phase.Name,
                phaseOrder = phase.Order,
                steps = allSteps
                    .Where(s => s.PhaseId == phase.PhaseId)
                    .OrderBy(s => s.Order)
                    .Select(s => new
                    {
                        stepId = s.StepId,
                        stepName = s.Name,
                        stepOrder = s.Order,
                        isCurrent = s.StepId == prospect.CurrentStepId
                    })
                    .ToList()
            }).ToList();

            return Ok(new
            {
                prospectId = prospectId,
                workflowId = prospect.WorkflowId,
                currentStepId = prospect.CurrentStepId,
                workflow = workflow
            });
        }

        /// <summary>
        /// Envía datos para el step actual y valida
        /// </summary>
        [HttpPost("current/submit")]
        public async Task<IActionResult> SubmitStepData(
            Guid prospectId,
            [FromBody] SubmitStepDataRequest request)
        {
            _logger.LogInformation("Submitting step data for ProspectId: {ProspectId}", prospectId);

            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            if (!prospect.CurrentStepId.HasValue)
            {
                return BadRequest(new { error = "No hay step activo para este prospecto" });
            }

            try
            {
                // 1. Validar datos del step
                var validationResult = await _validationService.ValidateStepAsync(
                    prospect.CurrentStepId.Value,
                    request.DataJson);

                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        "Step validation failed for ProspectId: {ProspectId}, StepId: {StepId}",
                        prospectId, prospect.CurrentStepId.Value);

                    return BadRequest(new
                    {
                        error = "Validación fallida",
                        validationErrors = validationResult.Errors
                    });
                }

                // 2. Persistir datos en ProspectData
                await _prospectDataService.UpdateProspectDataAsync(prospectId, request.DataJson);

                _logger.LogInformation(
                    "Step data saved successfully for ProspectId: {ProspectId}, StepId: {StepId}",
                    prospectId, prospect.CurrentStepId.Value);

                // 3. Emitir evento para que las reglas de negocio se ejecuten
                await _eventPublisher.PublishAsync("StepDataSubmitted", new
                {
                    prospect_id = prospectId,
                    user_id = prospect.UserId,
                    workflow_id = prospect.WorkflowId,
                    step_id = prospect.CurrentStepId.Value,
                    data = JsonDocument.Parse(request.DataJson).RootElement
                });

                return Ok(new
                {
                    message = "Datos del step guardados exitosamente",
                    prospectId = prospectId,
                    stepId = prospect.CurrentStepId.Value,
                    validationResult = "PASSED"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting step data for ProspectId: {ProspectId}", prospectId);
                return StatusCode(500, new
                {
                    error = "Error al procesar los datos",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// Avanza manualmente al siguiente step
        /// </summary>
        [HttpPost("advance")]
        public async Task<IActionResult> AdvanceToNextStep(Guid prospectId)
        {
            _logger.LogInformation("Advancing to next step for ProspectId: {ProspectId}", prospectId);

            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            if (!prospect.CurrentStepId.HasValue)
            {
                return BadRequest(new { error = "No hay step activo" });
            }

            try
            {
                var currentStep = await _stepRepo.GetByIdAsync(prospect.CurrentStepId.Value);
                if (currentStep == null)
                {
                    return NotFound(new { error = "Step actual no encontrado" });
                }

                // Buscar el siguiente step en la misma fase
                var allSteps = await _stepRepo.GetAllAsync();
                var nextStep = allSteps
                    .Where(s => s.PhaseId == currentStep.PhaseId && s.Order > currentStep.Order)
                    .OrderBy(s => s.Order)
                    .FirstOrDefault();

                if (nextStep == null)
                {
                    // No hay más steps en esta fase, buscar siguiente fase
                    var currentPhase = await _phaseRepo.GetByIdAsync(currentStep.PhaseId);
                    var allPhases = await _phaseRepo.GetAllAsync();
                    
                    var nextPhase = allPhases
                        .Where(p => p.WorkflowId == currentPhase.WorkflowId && p.Order > currentPhase.Order)
                        .OrderBy(p => p.Order)
                        .FirstOrDefault();

                    if (nextPhase != null)
                    {
                        nextStep = allSteps
                            .Where(s => s.PhaseId == nextPhase.PhaseId)
                            .OrderBy(s => s.Order)
                            .FirstOrDefault();
                    }
                }

                if (nextStep == null)
                {
                    // No hay más steps, el workflow está completo
                    prospect.CurrentStepId = null;
                    prospect.UpdatedAt = DateTime.UtcNow;
                    await _prospectRepo.UpdateAsync(prospect);
                    await _unitOfWork.SaveChangesAsync();  // ? NUEVO: Persistir cambios

                    _logger.LogInformation(
                        "Workflow completed for ProspectId: {ProspectId}",
                        prospectId);

                    // Emitir evento de completitud
                    await _eventPublisher.PublishAsync("WorkflowCompleted", new
                    {
                        prospect_id = prospectId,
                        user_id = prospect.UserId,
                        workflow_id = prospect.WorkflowId
                    });

                    return Ok(new
                    {
                        message = "Workflow completado exitosamente",
                        prospectId = prospectId,
                        completed = true
                    });
                }

                // Avanzar al siguiente step
                prospect.CurrentStepId = nextStep.StepId;
                prospect.UpdatedAt = DateTime.UtcNow;
                await _prospectRepo.UpdateAsync(prospect);
                await _unitOfWork.SaveChangesAsync();  // ? NUEVO: Persistir cambios

                _logger.LogInformation(
                    "Advanced to next step. ProspectId: {ProspectId}, NewStepId: {StepId}",
                    prospectId, nextStep.StepId);

                return Ok(new
                {
                    message = "Avanzado al siguiente step",
                    prospectId = prospectId,
                    previousStepId = currentStep.StepId,
                    currentStepId = nextStep.StepId,
                    stepName = nextStep.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error advancing step for ProspectId: {ProspectId}", prospectId);
                return StatusCode(500, new
                {
                    error = "Error al avanzar al siguiente step",
                    detail = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// DTO para enviar datos de un step
    /// </summary>
    public class SubmitStepDataRequest
    {
        public string DataJson { get; set; } = string.Empty;
    }
}
