using System;
using System.Collections.Generic;

namespace Onboarding.Core.Models
{
    /// <summary>
    /// DTO para representar errores de validación estructurados.
    /// Aplica ISP: Interface Segregation Principle - Contrato específico para errores.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Campo que generó el error
        /// </summary>
        public string FieldKey { get; set; } = string.Empty;

        /// <summary>
        /// Código de error (REQUIRED, INVALID_TYPE, INVALID_FORMAT, etc.)
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje descriptivo del error
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Metadata adicional (opcional)
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// DTO para resultado de validación completo
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Indica si la validación fue exitosa
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Lista de errores encontrados
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new();

        /// <summary>
        /// Mensaje general (opcional)
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Timestamp de la validación
        /// </summary>
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }
}
