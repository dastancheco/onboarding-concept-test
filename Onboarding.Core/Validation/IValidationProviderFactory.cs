namespace Onboarding.Core.Validation
{
    /// <summary>
    /// Factory para resolver providers de validación desde BD
    /// </summary>
    public interface IValidationProviderFactory
    {
        /// <summary>
        /// Obtiene un provider por su ID
        /// </summary>
        Task<IValidationProvider?> GetProviderAsync(int providerId);

        /// <summary>
        /// Obtiene un provider por su key
        /// </summary>
        Task<IValidationProvider?> GetProviderByKeyAsync(string providerKey);
    }
}
