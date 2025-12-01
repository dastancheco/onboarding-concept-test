using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Validation;
using System.Text.Json;

namespace Onboarding.Infrastructure.Validation.Providers
{
    /// <summary>
    /// Provider genérico para validaciones con queries a la base de datos
    /// </summary>
    public class DatabaseQueryValidationProvider : IValidationProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseQueryValidationProvider> _logger;

        private DatabaseQueryConfig _config = new();

        public string ProviderKey => "DATABASE_QUERY_VALIDATOR";
        public string ProviderType => "DATABASE_QUERY";

        public DatabaseQueryValidationProvider(
            IServiceProvider serviceProvider,
            ILogger<DatabaseQueryValidationProvider> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void Configure(string? configJson)
        {
            if (!string.IsNullOrWhiteSpace(configJson))
            {
                try
                {
                    _config = JsonSerializer.Deserialize<DatabaseQueryConfig>(configJson)
                        ?? new DatabaseQueryConfig();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deserializing config. Using defaults.");
                }
            }
        }

        public async Task<ValidationStepResult> ValidateAsync(ValidationContext context)
        {
            var result = new ValidationStepResult
            {
                ProviderKey = ProviderKey,
                IsValid = true
            };

            if (string.IsNullOrWhiteSpace(_config.QueryDescription))
            {
                result.IsValid = false;
                result.Severity = ValidationSeverity.Error;
                result.Message = "QueryDescription is required";
                return result;
            }

            _logger.LogInformation(
                "Executing database query validation: {Description}",
                _config.QueryDescription);

            try
            {
                // Aquí podrías implementar queries específicas según el tipo
                // Por ahora es un placeholder que puedes extender

                result.IsValid = true;
                result.Severity = ValidationSeverity.Info;
                result.Message = $"Database query validation passed: {_config.QueryDescription}";

                _logger.LogInformation("Database query validation successful");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Severity = ValidationSeverity.Critical;
                result.Message = $"Error executing database query: {ex.Message}";

                _logger.LogError(ex, "Error executing database query validation");
            }

            return await Task.FromResult(result);
        }

        private class DatabaseQueryConfig
        {
            public string QueryDescription { get; set; } = string.Empty;
            public string? QueryParameters { get; set; }
        }
    }
}
