using Onboarding.Core.Interfaces;
using Onboarding.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Onboarding.Core.Services
{
    /// <summary>
    /// Pipeline de validación que ejecuta múltiples validadores en secuencia.
    /// Aplica Chain of Responsibility Pattern.
    /// </summary>
    public class ValidationPipeline
    {
        private readonly Dictionary<string, IFieldValidator> _validators;

        public ValidationPipeline(IEnumerable<IFieldValidator> validators)
        {
            _validators = validators.ToDictionary(v => v.ValidatorType.ToUpperInvariant());
        }

        /// <summary>
        /// Ejecuta una cadena de validaciones para un campo.
        /// </summary>
        public List<ValidationError> ValidateField(
            string fieldKey,
            string? value,
            string dataType,
            Dictionary<string, object> config)
        {
            var errors = new List<ValidationError>();

            // 1. VALIDACIÓN: REQUIRED (si está configurado)
            if (GetBooleanValue(config, "is_required", false))
            {
                var requiredError = ValidateWithValidator("REQUIRED", fieldKey, value, null);
                if (requiredError != null)
                {
                    errors.Add(requiredError);
                    return errors; // Si falta, no seguir validando
                }
            }

            // Si el valor está vacío y no es requerido, saltar validaciones
            if (string.IsNullOrWhiteSpace(value))
                return errors;

            // 2. VALIDACIÓN: DATA_TYPE
            var dataTypeError = ValidateWithValidator("DATA_TYPE", fieldKey, value, dataType);
            if (dataTypeError != null)
            {
                errors.Add(dataTypeError);
                return errors; // Si el tipo es incorrecto, no seguir
            }

            // 3. VALIDACIÓN: RANGE (para números)
            if (dataType.ToUpperInvariant() is "NUMBER" or "DECIMAL" or "INTEGER")
            {
                if (config.ContainsKey("min") || config.ContainsKey("max"))
                {
                    var rangeError = ValidateWithValidator("RANGE", fieldKey, value, config);
                    if (rangeError != null)
                        errors.Add(rangeError);
                }
            }

            // 4. VALIDACIÓN: LENGTH (para texto)
            if (dataType.ToUpperInvariant() is "TEXT" or "STRING")
            {
                if (config.ContainsKey("min_length") || config.ContainsKey("max_length"))
                {
                    var lengthError = ValidateWithValidator("LENGTH", fieldKey, value, config);
                    if (lengthError != null)
                        errors.Add(lengthError);
                }
            }

            // 5. VALIDACIÓN: ALLOWED_VALUES (enum)
            if (config.TryGetValue("allowed_values", out var allowedObj))
            {
                var allowedValues = ExtractAllowedValues(allowedObj);
                if (allowedValues.Any())
                {
                    var enumError = ValidateWithValidator("ALLOWED_VALUES", fieldKey, value, allowedValues);
                    if (enumError != null)
                        errors.Add(enumError);
                }
            }

            // 6. VALIDACIÓN: VALIDADOR ESPECÍFICO (RFC, CURP, EMAIL, etc.)
            if (config.TryGetValue("validator", out var validatorObj))
            {
                var validatorType = ExtractString(validatorObj);
                if (!string.IsNullOrEmpty(validatorType))
                {
                    // Para REGEX, pasar el patrón
                    object? validatorConfig = null;
                    if (validatorType.ToUpperInvariant() == "REGEX" && config.TryGetValue("pattern", out var patternObj))
                    {
                        validatorConfig = ExtractString(patternObj);
                    }

                    var customError = ValidateWithValidator(validatorType, fieldKey, value, validatorConfig);
                    if (customError != null)
                        errors.Add(customError);
                }
            }

            return errors;
        }

        /// <summary>
        /// Ejecuta un validador específico.
        /// </summary>
        private ValidationError? ValidateWithValidator(
            string validatorType,
            string fieldKey,
            string? value,
            object? config)
        {
            if (!_validators.TryGetValue(validatorType.ToUpperInvariant(), out var validator))
                return null; // Validador no encontrado, ignorar

            if (validator.IsValid(value, config))
                return null; // Validación exitosa

            // Validación falló, crear error
            return new ValidationError
            {
                FieldKey = fieldKey,
                ErrorCode = DetermineErrorCode(validatorType),
                Message = validator.GetErrorMessage(fieldKey, config)
            };
        }

        /// <summary>
        /// Mapea el tipo de validador a un código de error.
        /// </summary>
        private string DetermineErrorCode(string validatorType)
        {
            return validatorType.ToUpperInvariant() switch
            {
                "REQUIRED" => "REQUIRED",
                "DATA_TYPE" => "INVALID_TYPE",
                "RANGE" => "OUT_OF_RANGE",
                "LENGTH" => "INVALID_LENGTH",
                "ALLOWED_VALUES" => "INVALID_VALUE",
                _ => "INVALID_FORMAT" // RFC, CURP, EMAIL, etc.
            };
        }

        /// <summary>
        /// Extrae valores permitidos de la configuración.
        /// </summary>
        private List<string> ExtractAllowedValues(object obj)
        {
            var result = new List<string>();

            if (obj is JsonElement elem && elem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in elem.EnumerateArray())
                {
                    var value = item.GetString();
                    if (!string.IsNullOrEmpty(value))
                        result.Add(value);
                }
            }
            else if (obj is List<string> list)
            {
                result.AddRange(list);
            }

            return result;
        }

        /// <summary>
        /// Extrae un string de un objeto (JsonElement o string directo).
        /// </summary>
        private string? ExtractString(object obj)
        {
            if (obj is JsonElement elem && elem.ValueKind == JsonValueKind.String)
                return elem.GetString();

            if (obj is string str)
                return str;

            return obj?.ToString();
        }

        /// <summary>
        /// Extrae un valor booleano de la configuración.
        /// </summary>
        private bool GetBooleanValue(Dictionary<string, object> config, string key, bool defaultValue)
        {
            if (!config.TryGetValue(key, out var value))
                return defaultValue;

            if (value is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.True) return true;
                if (elem.ValueKind == JsonValueKind.False) return false;
            }

            if (value is bool boolValue)
                return boolValue;

            return defaultValue;
        }
    }
}
