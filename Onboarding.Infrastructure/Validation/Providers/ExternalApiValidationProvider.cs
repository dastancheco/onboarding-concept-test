using Microsoft.Extensions.Logging;
using Onboarding.Core.Validation;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Onboarding.Infrastructure.Validation.Providers
{
    /// <summary>
    /// Provider genérico para validaciones con APIs externas
    /// </summary>
    public class ExternalApiValidationProvider : IValidationProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ExternalApiValidationProvider> _logger;

        private ExternalApiConfig _config = new();

        public string ProviderKey => "EXTERNAL_API_VALIDATOR";
        public string ProviderType => "EXTERNAL_API";

        public ExternalApiValidationProvider(
            IHttpClientFactory httpClientFactory,
            ILogger<ExternalApiValidationProvider> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public void Configure(string? configJson)
        {
            if (!string.IsNullOrWhiteSpace(configJson))
            {
                try
                {
                    _config = JsonSerializer.Deserialize<ExternalApiConfig>(configJson)
                        ?? new ExternalApiConfig();
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

            if (string.IsNullOrWhiteSpace(_config.Url))
            {
                result.IsValid = false;
                result.Severity = ValidationSeverity.Error;
                result.Message = "API URL is required";
                return result;
            }

            _logger.LogInformation("Calling external API: {Url}", _config.Url);

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);

                // Configurar headers
                if (!string.IsNullOrWhiteSpace(_config.AuthHeader))
                {
                    var authParts = _config.AuthHeader.Split(' ', 2);
                    if (authParts.Length == 2)
                    {
                        httpClient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue(authParts[0], authParts[1]);
                    }
                }

                // Preparar body con template
                var requestBody = InterpolateTemplate(_config.BodyTemplate, context);

                HttpResponseMessage response;

                if (_config.Method.ToUpper() == "POST")
                {
                    var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                    response = await httpClient.PostAsync(_config.Url, content);
                }
                else if (_config.Method.ToUpper() == "GET")
                {
                    response = await httpClient.GetAsync(_config.Url);
                }
                else
                {
                    result.IsValid = false;
                    result.Severity = ValidationSeverity.Error;
                    result.Message = $"Unsupported HTTP method: {_config.Method}";
                    return result;
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    result.IsValid = true;
                    result.Severity = ValidationSeverity.Info;
                    result.Message = "API validation passed";
                    result.OutputDataJson = responseBody;

                    // Intentar parsear respuesta
                    try
                    {
                        var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);
                        if (responseData != null)
                        {
                            foreach (var kvp in responseData)
                            {
                                result.Data[$"api_{kvp.Key}"] = kvp.Value;
                            }
                        }
                    }
                    catch
                    {
                        // Si no se puede parsear, guardar como string
                        result.Data["api_response"] = responseBody;
                    }

                    _logger.LogInformation("API validation successful: {Url}", _config.Url);
                }
                else
                {
                    result.IsValid = false;
                    result.Severity = ValidationSeverity.Error;
                    result.Message = $"API validation failed: {response.StatusCode}";
                    result.OutputDataJson = responseBody;

                    _logger.LogWarning(
                        "API validation failed: {Url} - Status: {StatusCode}",
                        _config.Url, response.StatusCode);
                }
            }
            catch (TaskCanceledException)
            {
                result.IsValid = false;
                result.Severity = ValidationSeverity.Critical;
                result.Message = $"API timeout after {_config.TimeoutSeconds} seconds";

                _logger.LogError("API timeout: {Url}", _config.Url);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Severity = ValidationSeverity.Critical;
                result.Message = $"Error calling API: {ex.Message}";

                _logger.LogError(ex, "Error calling external API: {Url}", _config.Url);
            }

            return result;
        }

        private string InterpolateTemplate(string template, ValidationContext context)
        {
            if (string.IsNullOrWhiteSpace(template))
                return "{}";

            var result = template;

            // Reemplazar {{input.key}} con valores del contexto
            foreach (var kvp in context.InputData)
            {
                result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value?.ToString() ?? "");
            }

            return result;
        }

        private class ExternalApiConfig
        {
            public string Url { get; set; } = string.Empty;
            public string Method { get; set; } = "POST";
            public int TimeoutSeconds { get; set; } = 10;
            public string? AuthHeader { get; set; }
            public string BodyTemplate { get; set; } = "{}";
        }
    }
}
