using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Validation;
using OvexDataModelingTest.Entities.App;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onboarding.Core.Events.Handlers
{
    /// <summary>
    /// Handler para el evento de registro de nuevo usuario.
    /// Crea el usuario, determina el workflow y crea el prospecto inicial.
    /// </summary>
    public class UserRegisteredHandler : IEventHandler
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IWorkflowRoutingService _workflowRoutingService;
        private readonly IRepository<Prospect> _prospectRepo;
        private readonly IProspectDataService _prospectDataService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationOrchestrator _validationOrchestrator;
        private readonly ILogger<UserRegisteredHandler> _logger;

        public string EventType => "UserRegistered";

        public UserRegisteredHandler(
            IUserManagementService userManagementService,
            IWorkflowRoutingService workflowRoutingService,
            IRepository<Prospect> prospectRepo,
            IProspectDataService prospectDataService,
            IUnitOfWork unitOfWork,
            IValidationOrchestrator validationOrchestrator,
            ILogger<UserRegisteredHandler> logger)
        {
            _userManagementService = userManagementService;
            _workflowRoutingService = workflowRoutingService;
            _prospectRepo = prospectRepo;
            _prospectDataService = prospectDataService;
            _unitOfWork = unitOfWork;
            _validationOrchestrator = validationOrchestrator;
            _logger = logger;
        }

        public async Task HandleAsync(EventContext context)
        {
            _logger.LogInformation(
                "Handling user registration. CorrelationId: {CorrelationId}",
                context.CorrelationId);

            // 1. Gestión de usuarios
            string? email = _userManagementService.ExtractEmailFromPayload(context.PayloadJson);

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogError("Cannot create user: email not found in payload");
                throw new InvalidOperationException("Email no encontrado en el payload");
            }

            // 2. Determinar workflow
            int selectedWorkflowId = await _workflowRoutingService.DetermineWorkflowAsync(context.PayloadJson);

            if (selectedWorkflowId == 0)
            {
                _logger.LogError("No routing rule matched for this user");
                throw new InvalidOperationException("No se encontró workflow para este usuario");
            }

            // ?? NUEVO: Ejecutar validaciones configurables
            var validationContext = new ValidationContext
            {
                TriggerContext = "PRE_USER_REGISTRATION",
                Email = email,
                WorkflowId = selectedWorkflowId,
                InputData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    context.PayloadJson) ?? new(),
                CorrelationId = context.CorrelationId
            };

            var validationResult = await _validationOrchestrator.ExecuteAsync(
                "PRE_USER_REGISTRATION", validationContext);

            // ?? NUEVO: Manejar resultado de validación
            if (!validationResult.Success)
            {
                var firstError = validationResult.FirstError;

                _logger.LogWarning(
                    "User registration validation failed: {Message} (CorrelationId: {CorrelationId})",
                    firstError?.Message, context.CorrelationId);

                // Si hay un prospecto existente detectado, usar ese en lugar de crear uno nuevo
                if (validationContext.EnrichedData.ContainsKey("duplicate_detected") &&
                    validationContext.EnrichedData.ContainsKey("recent_prospect_id"))
                {
                    var existingProspectId = Guid.Parse(
                        validationContext.EnrichedData["recent_prospect_id"].ToString() ?? "");

                    _logger.LogInformation(
                        "Email already registered. Using existing Prospect: {ProspectId}",
                        existingProspectId);

                    // Actualizar contexto con el prospecto existente
                    context.ProspectId = existingProspectId;
                    context.WorkflowId = selectedWorkflowId;

                    // Actualizar timestamp del prospecto existente
                    var existingProspect = await _prospectRepo.GetByIdAsync(existingProspectId);
                    if (existingProspect != null)
                    {
                        existingProspect.UpdatedAt = DateTime.UtcNow;
                        await _prospectRepo.UpdateAsync(existingProspect);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    return; // No crear nuevo prospecto
                }

                // Si no es un duplicado manejable, lanzar excepción
                throw new ValidationException(firstError?.Message ?? "Validation failed");
            }

            _logger.LogInformation("All validations passed. Proceeding with user creation.");

            // 3. Crear usuario
            User user = await _userManagementService.GetOrCreateUserAsync(email, context.PayloadJson);
            _logger.LogInformation("User resolved: {UserId} ({Email})", user.UserId, user.Email);

            // 4. Crear la Instancia (Prospect)
            var newProspect = new Prospect
            {
                ProspectId = Guid.NewGuid(),
                UserId = user.UserId,
                WorkflowId = selectedWorkflowId,
                Status = "STARTED",
                CreatedAt = DateTime.UtcNow
            };

            await _prospectRepo.AddAsync(newProspect);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Prospect created: {ProspectId} for Workflow {WorkflowId}",
                newProspect.ProspectId, selectedWorkflowId);

            // 5. Inicializar ProspectData
            await _prospectDataService.UpdateProspectDataAsync(newProspect.ProspectId, context.PayloadJson);
            _logger.LogInformation("Initial ProspectData created for {ProspectId}", newProspect.ProspectId);

            // 6. Actualizar contexto para evaluación de reglas posterior
            context.ProspectId = newProspect.ProspectId;
            context.WorkflowId = selectedWorkflowId;
        }
    }

    /// <summary>
    /// Excepción para validaciones fallidas
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
