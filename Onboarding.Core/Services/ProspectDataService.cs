using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.App;
using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Onboarding.Core.Services
{
    public class ProspectDataService : IProspectDataService
    {
        private readonly IRepository<ProspectData> _prospectDataRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProspectDataService> _logger;

        public ProspectDataService(
            IRepository<ProspectData> prospectDataRepo,
            IUnitOfWork unitOfWork,
            ILogger<ProspectDataService> logger)
        {
            _prospectDataRepo = prospectDataRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<string> UpdateProspectDataAsync(Guid prospectId, string newDataJson)
        {
            _logger.LogInformation("Updating ProspectData for ProspectId: {ProspectId}", prospectId);

            // 1. Obtener datos existentes (si existen)
            var existingData = await _prospectDataRepo.GetByIdAsync(prospectId);

            if (existingData == null)
            {
                // Primera vez: Crear registro nuevo
                _logger.LogInformation("Creating new ProspectData record for ProspectId: {ProspectId}", prospectId);
                
                existingData = new ProspectData
                {
                    ProspectId = prospectId,
                    Data = newDataJson
                };

                await _prospectDataRepo.AddAsync(existingData);
            }
            else
            {
                // Merge: Combinar JSON existente con nuevo
                _logger.LogDebug("Merging existing data with new data for ProspectId: {ProspectId}", prospectId);
                
                var mergedJson = MergeJsonData(existingData.Data, newDataJson);
                existingData.Data = mergedJson;

                await _prospectDataRepo.UpdateAsync(existingData);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("ProspectData updated successfully for ProspectId: {ProspectId}", prospectId);
            return existingData.Data;
        }

        public async Task<string> GetProspectDataAsync(Guid prospectId)
        {
            var data = await _prospectDataRepo.GetByIdAsync(prospectId);
            return data?.Data ?? "{}";
        }

        public async Task<bool> HasFieldAsync(Guid prospectId, string fieldKey)
        {
            var dataJson = await GetProspectDataAsync(prospectId);
            
            try
            {
                var jsonNode = JsonNode.Parse(dataJson);
                var value = jsonNode?[fieldKey]?.ToString();
                return !string.IsNullOrWhiteSpace(value);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Combina dos JSONs, dando prioridad a los valores nuevos.
        /// Si un campo existe en ambos, el nuevo sobrescribe al viejo.
        /// </summary>
        private string MergeJsonData(string existingJson, string newJson)
        {
            try
            {
                var existingNode = JsonNode.Parse(existingJson ?? "{}") ?? new JsonObject();
                var newNode = JsonNode.Parse(newJson) ?? new JsonObject();

                // Iterar sobre todos los campos del JSON nuevo
                foreach (var property in newNode.AsObject())
                {
                    // Sobrescribir o agregar el campo
                    existingNode.AsObject()[property.Key] = property.Value?.DeepClone();
                }

                return existingNode.ToJsonString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging JSON data. Returning new data only.");
                return newJson;
            }
        }
    }
}
