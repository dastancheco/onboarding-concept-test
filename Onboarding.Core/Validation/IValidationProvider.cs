namespace Onboarding.Core.Validation
{
    /// <summary>
    /// Interfaz para proveedores de validación configurables
    /// </summary>
    public interface IValidationProvider
    {
        /// <summary>
        /// Key única del provider
        /// </summary>
        string ProviderKey { get; }

        /// <summary>
        /// Tipo de provider: INTERNAL_CODE, EXTERNAL_API, DATABASE_QUERY, RULE_ENGINE
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// Configura el provider con JSON desde BD
        /// </summary>
        void Configure(string? configJson);

        /// <summary>
        /// Ejecuta la validación
        /// </summary>
        Task<ValidationStepResult> ValidateAsync(ValidationContext context);
    }
}
