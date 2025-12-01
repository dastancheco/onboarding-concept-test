using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.App;
using System;
using System.Threading.Tasks;

namespace Onboarding.Core.Events.Handlers
{
    /// <summary>
    /// Handler para el evento de sumisión de datos de un step.
    /// Valida y persiste los datos del step actual del prospecto.
    /// </summary>
    public class StepDataSubmittedHandler : IEventHandler
    {
        private readonly IRepository<Prospect> _prospectRepo;
        private readonly IStepValidationService _validator;
        private readonly IProspectDataService _prospectDataService;
        private readonly ILogger<StepDataSubmittedHandler> _logger;

        public string EventType => "StepDataSubmitted";

        public StepDataSubmittedHandler(
            IRepository<Prospect> prospectRepo,
            IStepValidationService validator,
            IProspectDataService prospectDataService,
            ILogger<StepDataSubmittedHandler> logger)
        {
            _prospectRepo = prospectRepo;
            _validator = validator;
            _prospectDataService = prospectDataService;
            _logger = logger;
        }

        public async Task HandleAsync(EventContext context)
        {
            _logger.LogInformation(
                "Handling step data submission for ProspectId: {ProspectId}. CorrelationId: {CorrelationId}",
                context.ProspectId, context.CorrelationId);

            if (!context.ProspectId.HasValue)
            {
                _logger.LogError("ProspectId is required for StepDataSubmitted event");
                throw new InvalidOperationException("ProspectId es requerido para este evento");
            }

            // 1. Obtener el prospecto
            var prospect = await _prospectRepo.GetByIdAsync(context.ProspectId.Value);

            if (prospect == null)
            {
                _logger.LogError("Prospect not found: {ProspectId}", context.ProspectId.Value);
                throw new InvalidOperationException($"Prospecto no encontrado: {context.ProspectId.Value}");
            }

            // 2. Verificar que tenga un step activo
            if (prospect.CurrentStepId == null)
            {
                _logger.LogError("Prospect {ProspectId} has no active step assigned", context.ProspectId.Value);
                throw new InvalidOperationException("El prospecto no tiene un paso activo asignado");
            }

            // 3. Validar los datos del step
            var validationResult = await _validator.ValidateStepAsync(
                prospect.CurrentStepId.Value,
                context.PayloadJson);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Validation failed for Prospect {ProspectId}, Step {StepId}. Errors: {ErrorCount}",
                    context.ProspectId.Value, prospect.CurrentStepId.Value, validationResult.Errors.Count);

                foreach (var error in validationResult.Errors)
                {
                    _logger.LogWarning(
                        "  - [{ErrorCode}] {FieldKey}: {Message}",
                        error.ErrorCode, error.FieldKey, error.Message);
                }

                throw new InvalidOperationException($"Validación fallida: {validationResult.Message}");
            }

            _logger.LogInformation("Structural validation successful for Step {StepId}", prospect.CurrentStepId.Value);

            // 4. Persistir datos
            var updatedData = await _prospectDataService.UpdateProspectDataAsync(
                context.ProspectId.Value,
                context.PayloadJson);

            _logger.LogInformation(
                "ProspectData updated successfully for {ProspectId}. Total data: {DataLength} chars",
                context.ProspectId.Value, updatedData.Length);

            // 5. Actualizar contexto
            context.WorkflowId = prospect.WorkflowId;
        }
    }
}
