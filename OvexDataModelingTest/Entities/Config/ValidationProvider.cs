using System.ComponentModel.DataAnnotations;

namespace OvexDataModelingTest.Entities.Config
{
    /// <summary>
    /// Define un proveedor de validación disponible
    /// </summary>
    public class ValidationProvider
    {
        [Key]
        public int ProviderId { get; set; }

        [Required, MaxLength(50)]
        public string ProviderKey { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Tipo: INTERNAL_CODE, EXTERNAL_API, DATABASE_QUERY, RULE_ENGINE
        /// </summary>
        [Required, MaxLength(50)]
        public string ProviderType { get; set; } = string.Empty;

        /// <summary>
        /// Clase que implementa IValidationProvider (para INTERNAL_CODE)
        /// </summary>
        [MaxLength(200)]
        public string? ImplementationClass { get; set; }

        /// <summary>
        /// Configuración específica del provider en formato JSON
        /// </summary>
        public string? ConfigJson { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indica si los resultados se pueden cachear
        /// </summary>
        public bool CanCache { get; set; } = false;

        /// <summary>
        /// Duración del caché en segundos
        /// </summary>
        public int CacheDurationSeconds { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
