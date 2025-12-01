using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Validation;
using System.Text.Json;

namespace Onboarding.Infrastructure.Validation.Providers
{
    /// <summary>
    /// Provider para validaciones basadas en el RuleEngine existente
    /// </summary>
    public class RuleEngineValidationProvider : IValidationProvider
    {
        private readonly IRuleEngine _ruleEngine;
        private readonly ILogger<RuleEngineValidationProvider> _logger;

        private RuleEngineConfig _config = new();

        public string ProviderKey => "RULE_ENGINE_VALIDATOR";
        public string ProviderType => "RULE_ENGINE";

        public RuleEngineValidationProvider(
            IRuleEngine ruleEngine,
            ILogger<RuleEngineValidationProvider> logger)
        {
            _ruleEngine = ruleEngine;
            _logger = logger;
        }

        public void Configure(string? configJson)
        {
            if (!string.IsNullOrWhiteSpace(configJson))
            {
                try
                {
                    _config = JsonSerializer.Deserialize<RuleEngineConfig>(configJson)
                        ?? new RuleEngineConfig();
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

            if (string.IsNullOrWhiteSpace(_config.ConditionExpression))
            {
                result.IsValid = false;
                result.Severity = ValidationSeverity.Error;
                result.Message = "ConditionExpression is required";
                return result;
            }

            _logger.LogInformation(
                "Evaluating rule: {Expression}",
                _config.ConditionExpression);

            try
            {
                var contextJson = JsonSerializer.Serialize(context.InputData);
                var ruleResult = _ruleEngine.Evaluate(_config.ConditionExpression, contextJson);

                if (_config.ExpectedResult)
                {
                    // Esperamos que la condición sea verdadera
                    result.IsValid = ruleResult;
                    result.Message = ruleResult
                        ? _config.SuccessMessage ?? "Rule validation passed"
                        : _config.FailureMessage ?? "Rule validation failed";
                }
                else
                {
                    // Esperamos que la condición sea falsa (negación)
                    result.IsValid = !ruleResult;
                    result.Message = !ruleResult
                        ? _config.SuccessMessage ?? "Rule validation passed (negated)"
                        : _config.FailureMessage ?? "Rule validation failed (negated)";
                }

                result.Severity = result.IsValid
                    ? ValidationSeverity.Info
                    : ValidationSeverity.Error;

                _logger.LogInformation(
                    "Rule evaluation result: {IsValid} - {Message}",
                    result.IsValid, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating rule");
                result.IsValid = false;
                result.Severity = ValidationSeverity.Critical;
                result.Message = $"Error evaluating rule: {ex.Message}";
            }

            return await Task.FromResult(result);
        }

        private class RuleEngineConfig
        {
            public string ConditionExpression { get; set; } = string.Empty;
            public bool ExpectedResult { get; set; } = true;
            public string? SuccessMessage { get; set; }
            public string? FailureMessage { get; set; }
        }
    }
}
