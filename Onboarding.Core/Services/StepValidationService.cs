using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Models;
using OvexDataModelingTest.Entities.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Onboarding.Core.Services
{
    /// <summary>
    /// Servicio de validación de pasos con configuración dinámica.
    /// Orquesta la validación delegando TODA la lógica a validadores.
    /// Aplica SRP: Solo coordina, no valida directamente.
    /// </summary>
    public class StepValidationService : IStepValidationService
    {
        private readonly IRepository<Step_Field> _stepFieldsRepo;
        private readonly IRepository<FieldDefinition> _fieldsRepo;
        private readonly IValidationPipeline _validationPipeline;
        private readonly ILogger<StepValidationService> _logger;

        public StepValidationService(
            IRepository<Step_Field> stepFieldsRepo,
            IRepository<FieldDefinition> fieldsRepo,
            IValidationPipeline validationPipeline,
            ILogger<StepValidationService> logger)
        {
            _stepFieldsRepo = stepFieldsRepo;
            _fieldsRepo = fieldsRepo;
            _validationPipeline = validationPipeline;
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateStepAsync(int stepId, string payloadJson)
        {
            _logger.LogInformation("Validating step {StepId}", stepId);

            var result = new ValidationResult { IsValid = true };

            // 1. Parsear JSON del payload
            JsonNode? jsonNode;
            try
            {
                jsonNode = JsonNode.Parse(payloadJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON in payload");
                result.Errors.Add(new ValidationError
                {
                    FieldKey = "_payload",
                    ErrorCode = "INVALID_JSON",
                    Message = "El payload no es un JSON válido."
                });
                result.IsValid = false;
                result.Message = "JSON inválido";
                return result;
            }

            // 2. Obtener campos configurados para este paso
            var stepFieldLinks = await _stepFieldsRepo.FindAsync(sf => sf.StepId == stepId);

            if (!stepFieldLinks.Any())
            {
                _logger.LogWarning("No fields configured for step {StepId}", stepId);
                result.Message = "No hay campos configurados para este paso";
                return result;
            }

            // 3. Obtener definiciones de campos
            var fieldIds = stepFieldLinks.Select(sf => sf.FieldId).ToList();
            var allFields = await _fieldsRepo.GetAllAsync();
            var requiredFields = allFields.Where(f => fieldIds.Contains(f.FieldId)).ToList();

            _logger.LogDebug("Validating {Count} fields for step {StepId}", requiredFields.Count, stepId);

            // 4. Validar cada campo usando el pipeline
            foreach (var fieldDef in requiredFields)
            {
                var stepFieldLink = stepFieldLinks.First(sf => sf.FieldId == fieldDef.FieldId);
                
                // Obtener configuración efectiva (ConfigOverride tiene prioridad)
                var effectiveConfig = GetEffectiveConfig(fieldDef.Config, stepFieldLink.ConfigOverride);

                // NUEVO: Agregar nested_schema si existe
                if (!string.IsNullOrWhiteSpace(fieldDef.NestedSchema))
                {
                    effectiveConfig["nested_schema"] = fieldDef.NestedSchema;
                }

                // Extraer valor del payload
                // NUEVO: Para OBJECT_ARRAY, serializar el nodo completo
                string? value;
                if (fieldDef.DataType.ToUpper() == "OBJECT_ARRAY")
                {
                    var arrayNode = jsonNode?[fieldDef.FieldKey];
                    value = arrayNode?.ToJsonString();
                }
                else
                {
                    value = jsonNode?[fieldDef.FieldKey]?.ToString();
                }

                _logger.LogDebug("Validating field '{FieldKey}' (Type: {DataType}) with value: {Value}",
                    fieldDef.FieldKey, fieldDef.DataType, value ?? "(null)");

                // DELEGAR TODA LA VALIDACIÓN AL PIPELINE
                var fieldErrors = _validationPipeline.ValidateField(
                    fieldDef.FieldKey,
                    value,
                    fieldDef.DataType,
                    effectiveConfig);

                result.Errors.AddRange(fieldErrors);
            }

            // 5. Determinar resultado final
            result.IsValid = result.Errors.Count == 0;
            result.Message = result.IsValid
                ? "Validación exitosa"
                : $"Se encontraron {result.Errors.Count} error(es) de validación";

            _logger.LogInformation("Validation completed for step {StepId}. IsValid: {IsValid}, Errors: {ErrorCount}",
                stepId, result.IsValid, result.Errors.Count);

            return result;
        }

        /// <summary>
        /// Combina Config base con ConfigOverride.
        /// ConfigOverride tiene prioridad sobre Config.
        /// </summary>
        private Dictionary<string, object> GetEffectiveConfig(string? baseConfig, string? overrideConfig)
        {
            var config = new Dictionary<string, object>();

            // 1. Parsear config base
            if (!string.IsNullOrWhiteSpace(baseConfig))
            {
                try
                {
                    var baseDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(baseConfig);
                    if (baseDict != null)
                    {
                        foreach (var kvp in baseDict)
                        {
                            config[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse base config: {Config}", baseConfig);
                }
            }

            // 2. Sobrescribir con ConfigOverride
            if (!string.IsNullOrWhiteSpace(overrideConfig))
            {
                try
                {
                    var overrideDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(overrideConfig);
                    if (overrideDict != null)
                    {
                        foreach (var kvp in overrideDict)
                        {
                            config[kvp.Key] = kvp.Value; // Sobrescribir
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse override config: {Config}", overrideConfig);
                }
            }

            return config;
        }
    }
}
