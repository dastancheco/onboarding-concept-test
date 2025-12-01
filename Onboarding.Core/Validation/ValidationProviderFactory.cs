using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.Config;
using System.Text.Json;

namespace Onboarding.Core.Validation
{
    /// <summary>
    /// Factory para resolver proveedores de validación dinámicamente desde BD
    /// </summary>
    public class ValidationProviderFactory : IValidationProviderFactory
    {
        private readonly IRepository<ValidationProvider> _providerRepo;
        private readonly IEnumerable<IValidationProvider> _validationProviders;
        private readonly ILogger<ValidationProviderFactory> _logger;

        public ValidationProviderFactory(
            IRepository<ValidationProvider> providerRepo,
            IEnumerable<IValidationProvider> validationProviders,
            ILogger<ValidationProviderFactory> logger)
        {
            _providerRepo = providerRepo;
            _validationProviders = validationProviders;
            _logger = logger;
        }

        public async Task<IValidationProvider?> GetProviderAsync(int providerId)
        {
            // 1. Obtener configuración desde BD
            var providerConfig = await _providerRepo.GetByIdAsync(providerId);

            if (providerConfig == null || !providerConfig.IsActive)
            {
                _logger.LogWarning("Provider not found or inactive: {ProviderId}", providerId);
                return null;
            }

            return ResolveProviderInstance(providerConfig);
        }

        public async Task<IValidationProvider?> GetProviderByKeyAsync(string providerKey)
        {
            // 1. Obtener configuración desde BD
            var allProviders = await _providerRepo.GetAllAsync();
            var providerConfig = allProviders.FirstOrDefault(p => 
                p.ProviderKey == providerKey && p.IsActive);

            if (providerConfig == null)
            {
                _logger.LogWarning("Provider not found by key: {ProviderKey}", providerKey);
                return null;
            }

            return ResolveProviderInstance(providerConfig);
        }

        private IValidationProvider? ResolveProviderInstance(ValidationProvider providerConfig)
        {
            var provider = _validationProviders.FirstOrDefault(p => 
                p.ProviderKey == providerConfig.ProviderKey);

            if (provider == null)
            {
                _logger.LogError("No matching provider found for key: {ProviderKey}", 
                    providerConfig.ProviderKey);
                return null;
            }

            provider.Configure(providerConfig.ConfigJson);
            return provider;
        }
    }
}
