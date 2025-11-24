using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Onboarding.Core.Validators
{
    /// <summary>
    /// Validador de campo requerido.
    /// Lee "is_required" de la configuración.
    /// </summary>
    public class RequiredValidator : IFieldValidator
    {
        public string ValidatorType => "REQUIRED";

        public bool IsValid(string? value, object? config = null)
        {
            // Si el valor está presente, es válido
            return !string.IsNullOrWhiteSpace(value);
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            return $"El campo '{fieldKey}' es obligatorio.";
        }
    }

    /// <summary>
    /// Validador de rangos numéricos.
    /// Lee "min" y "max" de la configuración.
    /// </summary>
    public class RangeValidator : IFieldValidator
    {
        public string ValidatorType => "RANGE";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true; // El REQUIRED validator maneja esto

            if (!double.TryParse(value, out var numValue))
                return false; // DataTypeValidator debería capturar esto primero

            if (config is not Dictionary<string, object> configDict)
                return true;

            // Validar mínimo
            if (configDict.TryGetValue("min", out var minObj))
            {
                var min = GetDoubleValue(minObj);
                if (min.HasValue && numValue < min.Value)
                    return false;
            }

            // Validar máximo
            if (configDict.TryGetValue("max", out var maxObj))
            {
                var max = GetDoubleValue(maxObj);
                if (max.HasValue && numValue > max.Value)
                    return false;
            }

            return true;
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            if (config is not Dictionary<string, object> configDict)
                return $"El campo '{fieldKey}' está fuera de rango.";

            var min = configDict.TryGetValue("min", out var minObj) ? GetDoubleValue(minObj) : null;
            var max = configDict.TryGetValue("max", out var maxObj) ? GetDoubleValue(maxObj) : null;

            if (min.HasValue && max.HasValue)
                return $"El campo '{fieldKey}' debe estar entre {min.Value} y {max.Value}.";
            
            if (min.HasValue)
                return $"El campo '{fieldKey}' debe ser mayor o igual a {min.Value}.";
            
            if (max.HasValue)
                return $"El campo '{fieldKey}' debe ser menor o igual a {max.Value}.";

            return $"El campo '{fieldKey}' está fuera de rango.";
        }

        private double? GetDoubleValue(object obj)
        {
            if (obj is JsonElement elem && elem.ValueKind == JsonValueKind.Number)
                return elem.GetDouble();
            
            if (obj is double d)
                return d;
            
            if (obj is int i)
                return i;

            return null;
        }
    }

    /// <summary>
    /// Validador de longitud de texto.
    /// Lee "min_length" y "max_length" de la configuración.
    /// </summary>
    public class LengthValidator : IFieldValidator
    {
        public string ValidatorType => "LENGTH";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true; // REQUIRED validator maneja esto

            if (config is not Dictionary<string, object> configDict)
                return true;

            // Validar longitud mínima
            if (configDict.TryGetValue("min_length", out var minObj))
            {
                var minLen = GetIntValue(minObj);
                if (minLen.HasValue && value.Length < minLen.Value)
                    return false;
            }

            // Validar longitud máxima
            if (configDict.TryGetValue("max_length", out var maxObj))
            {
                var maxLen = GetIntValue(maxObj);
                if (maxLen.HasValue && value.Length > maxLen.Value)
                    return false;
            }

            return true;
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            if (config is not Dictionary<string, object> configDict)
                return $"El campo '{fieldKey}' tiene una longitud inválida.";

            var minLen = configDict.TryGetValue("min_length", out var minObj) ? GetIntValue(minObj) : null;
            var maxLen = configDict.TryGetValue("max_length", out var maxObj) ? GetIntValue(maxObj) : null;

            if (minLen.HasValue && maxLen.HasValue)
                return $"El campo '{fieldKey}' debe tener entre {minLen.Value} y {maxLen.Value} caracteres.";
            
            if (minLen.HasValue)
                return $"El campo '{fieldKey}' debe tener al menos {minLen.Value} caracteres.";
            
            if (maxLen.HasValue)
                return $"El campo '{fieldKey}' no debe exceder {maxLen.Value} caracteres.";

            return $"El campo '{fieldKey}' tiene una longitud inválida.";
        }

        private int? GetIntValue(object obj)
        {
            if (obj is JsonElement elem && elem.ValueKind == JsonValueKind.Number)
                return elem.GetInt32();
            
            if (obj is int i)
                return i;

            return null;
        }
    }

    /// <summary>
    /// Validador de tipo de dato.
    /// Valida que el valor coincida con el DataType esperado.
    /// </summary>
    public class DataTypeValidator : IFieldValidator
    {
        private readonly ILogger<DataTypeValidator> _logger;

        public DataTypeValidator(ILogger<DataTypeValidator> logger)
        {
            _logger = logger;
        }

        public string ValidatorType => "DATA_TYPE";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true; // REQUIRED validator maneja esto

            // Config debe ser el DataType (STRING)
            if (config is not string dataType)
                return true;

            return dataType.ToUpperInvariant() switch
            {
                "NUMBER" or "DECIMAL" => double.TryParse(value, out _),
                "INTEGER" => int.TryParse(value, out _),
                "BOOLEAN" => bool.TryParse(value, out _),
                "DATE" => DateTime.TryParse(value, out _),
                "TEXT" or "STRING" => true,
                _ => true // Tipo desconocido, no validar
            };
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            if (config is string dataType)
            {
                return dataType.ToUpperInvariant() switch
                {
                    "NUMBER" or "DECIMAL" => $"El campo '{fieldKey}' debe ser numérico.",
                    "INTEGER" => $"El campo '{fieldKey}' debe ser un número entero.",
                    "BOOLEAN" => $"El campo '{fieldKey}' debe ser true o false.",
                    "DATE" => $"El campo '{fieldKey}' debe ser una fecha válida.",
                    _ => $"El campo '{fieldKey}' tiene un tipo de dato inválido."
                };
            }

            return $"El campo '{fieldKey}' tiene un tipo de dato inválido.";
        }
    }

    /// <summary>
    /// Validador de valores permitidos (enum).
    /// Lee "allowed_values" de la configuración.
    /// </summary>
    public class AllowedValuesValidator : IFieldValidator
    {
        public string ValidatorType => "ALLOWED_VALUES";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return true; // REQUIRED validator maneja esto

            if (config is not List<string> allowedValues || !allowedValues.Any())
                return true; // Sin restricción

            return allowedValues.Contains(value);
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            if (config is List<string> allowedValues && allowedValues.Any())
            {
                return $"El campo '{fieldKey}' debe ser uno de: {string.Join(", ", allowedValues)}.";
            }

            return $"El campo '{fieldKey}' tiene un valor no permitido.";
        }
    }
}
