using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Onboarding.Infrastructure.Strategies.Internal
{
    /// <summary>
    /// Estrategia que cambia el estado de un prospecto.
    /// Puede ser invocada desde reglas para automatizar transiciones de estado.
    /// </summary>
    public class UpdateProspectStatusStrategy : IConcreteStrategy
    {
        private readonly IProspectStatusService _prospectStatusService;
        private readonly ILogger<UpdateProspectStatusStrategy> _logger;

        public UpdateProspectStatusStrategy(
            IProspectStatusService prospectStatusService,
            ILogger<UpdateProspectStatusStrategy> logger)
        {
            _prospectStatusService = prospectStatusService;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid prospectId, string configJson, string payloadJson)
        {
            _logger.LogInformation("Executing UpdateProspectStatusStrategy for ProspectId: {ProspectId}", prospectId);

            try
            {
                // 1. Parsear configuración
                var config = JsonNode.Parse(configJson);
                var targetStatus = config?["target_status"]?.ToString();
                var reason = config?["reason"]?.ToString();
                var changedBy = config?["changed_by"]?.ToString() ?? "RULE_ENGINE";

                if (string.IsNullOrEmpty(targetStatus))
                {
                    _logger.LogError("target_status not specified in configuration");
                    return;
                }

                // 2. Actualizar estado con validación
                var success = await _prospectStatusService.UpdateStatusAsync(
                    prospectId,
                    targetStatus,
                    reason,
                    changedBy);

                if (success)
                {
                    _logger.LogInformation(
                        "Status updated successfully for ProspectId {ProspectId} to {TargetStatus}",
                        prospectId, targetStatus);
                }
                else
                {
                    _logger.LogWarning(
                        "Status update failed for ProspectId {ProspectId}",
                        prospectId);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, 
                    "Invalid status transition for ProspectId: {ProspectId}. Message: {Message}",
                    prospectId, ex.Message);
                
                // No lanzamos la excepción para no romper el flujo
                // El log ya contiene la información del error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Unexpected error updating status for ProspectId: {ProspectId}",
                    prospectId);
                throw;
            }
        }
    }
}
