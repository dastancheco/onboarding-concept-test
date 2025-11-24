using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Onboarding.Infrastructure.Strategies.Internal
{
    /// <summary>
    /// Estrategia que promueve datos del prospecto al Golden Record del usuario.
    /// Se ejecuta típicamente cuando un prospecto es APROBADO.
    /// </summary>
    public class PromoteToGoldenRecordStrategy : IConcreteStrategy
    {
        private readonly ICustomerDataService _customerDataService;
        private readonly IRepository<OvexDataModelingTest.Entities.App.Prospect> _prospectRepo;
        private readonly ILogger<PromoteToGoldenRecordStrategy> _logger;

        public PromoteToGoldenRecordStrategy(
            ICustomerDataService customerDataService,
            IRepository<OvexDataModelingTest.Entities.App.Prospect> prospectRepo,
            ILogger<PromoteToGoldenRecordStrategy> logger)
        {
            _customerDataService = customerDataService;
            _prospectRepo = prospectRepo;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid prospectId, string configJson, string payloadJson)
        {
            _logger.LogInformation("Executing PromoteToGoldenRecordStrategy for ProspectId: {ProspectId}", prospectId);

            try
            {
                // 1. Obtener el prospecto para conocer el UserId
                var prospect = await _prospectRepo.GetByIdAsync(prospectId);
                
                if (prospect == null)
                {
                    _logger.LogError("Prospect not found: {ProspectId}", prospectId);
                    return;
                }

                // 2. Promover datos al Golden Record
                var promotedFields = await _customerDataService.PromoteToGoldenRecordAsync(
                    prospect.UserId, 
                    prospectId);

                if (promotedFields.Any())
                {
                    _logger.LogInformation(
                        "Successfully promoted {Count} fields to Golden Record for User {UserId}: {Fields}",
                        promotedFields.Count,
                        prospect.UserId,
                        string.Join(", ", promotedFields));
                }
                else
                {
                    _logger.LogWarning("No fields were promoted for ProspectId: {ProspectId}", prospectId);
                }

                // 3. Opcional: Parsear config para acciones adicionales
                var config = JsonNode.Parse(configJson);
                var notifyUser = config?["notify_user"]?.GetValue<bool>() ?? false;

                if (notifyUser)
                {
                    _logger.LogInformation("User notification enabled (not implemented yet)");
                    // TODO: Emitir evento "GoldenRecordUpdated" para notificar al usuario
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting data to Golden Record for ProspectId: {ProspectId}", prospectId);
                throw;
            }
        }
    }
}
