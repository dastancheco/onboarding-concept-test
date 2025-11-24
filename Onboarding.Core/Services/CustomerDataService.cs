using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Onboarding.Core.Services
{
    public class CustomerDataService : ICustomerDataService
    {
        private readonly IRepository<CustomerData> _customerDataRepo;
        private readonly IRepository<ProspectData> _prospectDataRepo;
        private readonly IRepository<FieldDefinition> _fieldDefinitionsRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CustomerDataService> _logger;

        public CustomerDataService(
            IRepository<CustomerData> customerDataRepo,
            IRepository<ProspectData> prospectDataRepo,
            IRepository<FieldDefinition> fieldDefinitionsRepo,
            IUnitOfWork unitOfWork,
            ILogger<CustomerDataService> logger)
        {
            _customerDataRepo = customerDataRepo;
            _prospectDataRepo = prospectDataRepo;
            _fieldDefinitionsRepo = fieldDefinitionsRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<List<string>> PromoteToGoldenRecordAsync(Guid userId, Guid prospectId)
        {
            _logger.LogInformation("Promoting ProspectData to Golden Record. UserId: {UserId}, ProspectId: {ProspectId}", 
                userId, prospectId);

            var promotedFields = new List<string>();

            // 1. Obtener datos del prospecto
            var prospectData = await _prospectDataRepo.GetByIdAsync(prospectId);
            if (prospectData == null || string.IsNullOrWhiteSpace(prospectData.Data))
            {
                _logger.LogWarning("ProspectData not found or empty for ProspectId: {ProspectId}", prospectId);
                return promotedFields;
            }

            // 2. Obtener definiciones de campos con Scope="USER"
            var allFields = await _fieldDefinitionsRepo.GetAllAsync();
            var userScopeFields = allFields.Where(f => f.Scope == "USER").ToList();

            if (!userScopeFields.Any())
            {
                _logger.LogWarning("No fields with Scope='USER' found in FieldDefinitions");
                return promotedFields;
            }

            _logger.LogDebug("Found {Count} fields with Scope='USER'", userScopeFields.Count);

            // 3. Parsear datos del prospecto
            var prospectJson = JsonNode.Parse(prospectData.Data);
            if (prospectJson == null)
            {
                _logger.LogError("Failed to parse ProspectData JSON for ProspectId: {ProspectId}", prospectId);
                return promotedFields;
            }

            // 4. Obtener o crear CustomerData
            var customerData = await _customerDataRepo.GetByIdAsync(userId);
            JsonNode customerJson;

            if (customerData == null)
            {
                _logger.LogInformation("Creating new CustomerData for UserId: {UserId}", userId);
                
                customerData = new CustomerData
                {
                    UserId = userId,
                    Data = "{}",
                    UpdatedAt = DateTime.UtcNow
                };
                
                customerJson = new JsonObject();
                await _customerDataRepo.AddAsync(customerData);
            }
            else
            {
                customerJson = JsonNode.Parse(customerData.Data ?? "{}") ?? new JsonObject();
            }

            // 5. Copiar campos con Scope="USER" del prospecto al Golden Record
            foreach (var fieldDef in userScopeFields)
            {
                var fieldValue = prospectJson[fieldDef.FieldKey];
                
                if (fieldValue != null && !string.IsNullOrWhiteSpace(fieldValue.ToString()))
                {
                    // Solo promover si el campo tiene valor
                    customerJson.AsObject()[fieldDef.FieldKey] = fieldValue.DeepClone();
                    promotedFields.Add(fieldDef.FieldKey);
                    
                    _logger.LogDebug("Promoted field '{FieldKey}' to Golden Record", fieldDef.FieldKey);
                }
            }

            // 6. Guardar cambios
            if (promotedFields.Any())
            {
                customerData.Data = customerJson.ToJsonString();
                customerData.UpdatedAt = DateTime.UtcNow;

                await _customerDataRepo.UpdateAsync(customerData);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully promoted {Count} fields to Golden Record for UserId: {UserId}", 
                    promotedFields.Count, userId);
            }
            else
            {
                _logger.LogInformation("No fields to promote for UserId: {UserId}", userId);
            }

            return promotedFields;
        }

        public async Task<string?> GetCustomerDataAsync(Guid userId)
        {
            var customerData = await _customerDataRepo.GetByIdAsync(userId);
            return customerData?.Data;
        }

        public async Task<bool> HasGoldenRecordFieldAsync(Guid userId, string fieldKey)
        {
            var dataJson = await GetCustomerDataAsync(userId);
            
            if (string.IsNullOrWhiteSpace(dataJson))
                return false;

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

        public async Task<string?> GetGoldenRecordFieldAsync(Guid userId, string fieldKey)
        {
            var dataJson = await GetCustomerDataAsync(userId);
            
            if (string.IsNullOrWhiteSpace(dataJson))
                return null;

            try
            {
                var jsonNode = JsonNode.Parse(dataJson);
                return jsonNode?[fieldKey]?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting field '{FieldKey}' from Golden Record for UserId: {UserId}", 
                    fieldKey, userId);
                return null;
            }
        }

        public async Task<bool> UpdateCustomerDataAsync(Guid userId, string dataJson)
        {
            _logger.LogInformation("Updating CustomerData for UserId: {UserId}", userId);

            var customerData = await _customerDataRepo.GetByIdAsync(userId);

            if (customerData == null)
            {
                // Crear nuevo registro
                customerData = new CustomerData
                {
                    UserId = userId,
                    Data = dataJson,
                    UpdatedAt = DateTime.UtcNow
                };

                await _customerDataRepo.AddAsync(customerData);
            }
            else
            {
                // Actualizar existente (merge de JSONs)
                var existingJson = JsonNode.Parse(customerData.Data ?? "{}") ?? new JsonObject();
                var newJson = JsonNode.Parse(dataJson) ?? new JsonObject();

                foreach (var property in newJson.AsObject())
                {
                    existingJson.AsObject()[property.Key] = property.Value?.DeepClone();
                }

                customerData.Data = existingJson.ToJsonString();
                customerData.UpdatedAt = DateTime.UtcNow;

                await _customerDataRepo.UpdateAsync(customerData);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("CustomerData updated successfully for UserId: {UserId}", userId);
            return true;
        }
    }
}
