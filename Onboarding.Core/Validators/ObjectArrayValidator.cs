using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Onboarding.Core.Validators
{
    /// <summary>
    /// Validador para campos de tipo OBJECT_ARRAY (arrays de objetos anidados)
    /// Ejemplo: accionistas = [{ nombre: "Juan", participacion: 50 }, ...]
    /// </summary>
    public class ObjectArrayValidator : IFieldValidator
    {
        private readonly ILogger<ObjectArrayValidator> _logger;
        private readonly IServiceProvider _serviceProvider; // CAMBIO: Inyectar ServiceProvider en lugar de IValidationPipeline

        public string ValidatorType => "OBJECT_ARRAY";

        public ObjectArrayValidator(
            ILogger<ObjectArrayValidator> logger,
            IServiceProvider serviceProvider) // CAMBIO: ServiceProvider
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;

            try
            {
                var jsonNode = JsonNode.Parse(value);
                return jsonNode?.AsArray() != null;
            }
            catch
            {
                return false;
            }
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            return $"El campo '{fieldKey}' debe ser un array válido de objetos";
        }

        /// <summary>
        /// Validación completa con soporte para esquemas anidados
        /// </summary>
        public IEnumerable<ValidationError> ValidateWithSchema(
            string fieldKey,
            string? value,
            Dictionary<string, object> config)
        {
            var errors = new List<ValidationError>();

            // 1. Parsear el valor como JSON array
            JsonArray? array;
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    // Verificar si es requerido
                    if (GetConfigValue<bool>(config, "is_required", false))
                    {
                        errors.Add(new ValidationError
                        {
                            FieldKey = fieldKey,
                            ErrorCode = "REQUIRED",
                            Message = $"El campo '{fieldKey}' es requerido"
                        });
                    }
                    return errors;
                }

                var jsonNode = JsonNode.Parse(value);
                array = jsonNode?.AsArray();

                if (array == null)
                {
                    errors.Add(new ValidationError
                    {
                        FieldKey = fieldKey,
                        ErrorCode = "INVALID_FORMAT",
                        Message = $"El campo '{fieldKey}' debe ser un array de objetos"
                    });
                    return errors;
                }
            }
            catch (JsonException)
            {
                errors.Add(new ValidationError
                {
                    FieldKey = fieldKey,
                    ErrorCode = "INVALID_JSON",
                    Message = $"El campo '{fieldKey}' contiene JSON inválido"
                });
                return errors;
            }

            // 2. Validar cantidad de items
            var minItems = GetConfigValue<int>(config, "min_items", 0);
            var maxItems = GetConfigValue<int>(config, "max_items", int.MaxValue);

            if (array.Count < minItems)
            {
                errors.Add(new ValidationError
                {
                    FieldKey = fieldKey,
                    ErrorCode = "MIN_ITEMS",
                    Message = $"El campo '{fieldKey}' debe tener al menos {minItems} elemento(s)"
                });
            }

            if (array.Count > maxItems)
            {
                errors.Add(new ValidationError
                {
                    FieldKey = fieldKey,
                    ErrorCode = "MAX_ITEMS",
                    Message = $"El campo '{fieldKey}' no puede tener más de {maxItems} elemento(s)"
                });
            }

            // 3. Validar cada objeto del array según el nested_schema
            if (config.TryGetValue("nested_schema", out var schemaObj))
            {
                var schemaJson = schemaObj?.ToString();
                if (!string.IsNullOrWhiteSpace(schemaJson))
                {
                    try
                    {
                        var schema = JsonSerializer.Deserialize<NestedSchema>(schemaJson);
                        if (schema?.Properties != null)
                        {
                            for (int i = 0; i < array.Count; i++)
                            {
                                var item = array[i];
                                var itemErrors = ValidateObjectItem(fieldKey, i, item, schema.Properties);
                                errors.AddRange(itemErrors);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse nested_schema for field {FieldKey}", fieldKey);
                    }
                }
            }

            return errors;
        }

        private IEnumerable<ValidationError> ValidateObjectItem(
            string parentFieldKey,
            int itemIndex,
            JsonNode? item,
            List<NestedProperty> properties)
        {
            var errors = new List<ValidationError>();

            if (item == null)
            {
                errors.Add(new ValidationError
                {
                    FieldKey = $"{parentFieldKey}[{itemIndex}]",
                    ErrorCode = "NULL_ITEM",
                    Message = $"El elemento {itemIndex} no puede ser nulo"
                });
                return errors;
            }

            var itemObj = item.AsObject();

            foreach (var property in properties)
            {
                var propertyKey = $"{parentFieldKey}[{itemIndex}].{property.Key}";
                var propertyValue = itemObj.TryGetPropertyValue(property.Key, out var val)
                    ? val?.ToString()
                    : null;

                // Construir configuración para validar la propiedad
                var propertyConfig = new Dictionary<string, object>();
                if (property.Required) propertyConfig["is_required"] = true;
                if (property.MinLength.HasValue) propertyConfig["min_length"] = property.MinLength.Value;
                if (property.MaxLength.HasValue) propertyConfig["max_length"] = property.MaxLength.Value;
                if (property.MinValue.HasValue) propertyConfig["min_value"] = property.MinValue.Value;
                if (property.MaxValue.HasValue) propertyConfig["max_value"] = property.MaxValue.Value;
                if (!string.IsNullOrEmpty(property.Pattern)) propertyConfig["pattern"] = property.Pattern;

                // CAMBIO: Resolver IValidationPipeline de manera lazy para evitar dependencia circular
                using var scope = _serviceProvider.CreateScope();
                var validationPipeline = scope.ServiceProvider.GetRequiredService<IValidationPipeline>();

                // Usar el pipeline de validación para validar la propiedad
                var propertyErrors = validationPipeline.ValidateField(
                    propertyKey,
                    propertyValue,
                    property.Type,
                    propertyConfig);

                errors.AddRange(propertyErrors);
            }

            return errors;
        }

        private T GetConfigValue<T>(Dictionary<string, object> config, string key, T defaultValue)
        {
            if (config.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is JsonElement jsonElement)
                    {
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText()) ?? defaultValue;
                    }
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Esquema de un objeto anidado
    /// </summary>
    public class NestedSchema
    {
        public List<NestedProperty> Properties { get; set; } = new();
    }

    /// <summary>
    /// Definición de una propiedad dentro de un objeto anidado
    /// </summary>
    public class NestedProperty
    {
        public string Key { get; set; } = string.Empty;
        public string Type { get; set; } = "TEXT";
        public bool Required { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? Pattern { get; set; }
    }
}
