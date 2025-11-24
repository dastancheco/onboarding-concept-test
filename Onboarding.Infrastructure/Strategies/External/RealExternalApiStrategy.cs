using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Onboarding.Infrastructure.Strategies.External
{
    /// <summary>
    /// Estrategia para ejecutar llamadas HTTP reales a APIs externas.
    /// Reemplaza ExternalApiStrategy (simulado) con implementación real.
    /// </summary>
    public class RealExternalApiStrategy : IConcreteStrategy
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IProspectDataService _prospectDataService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<RealExternalApiStrategy> _logger;

        public RealExternalApiStrategy(
            IHttpClientFactory httpClientFactory,
            IProspectDataService prospectDataService,
            IEventPublisher eventPublisher,
            ILogger<RealExternalApiStrategy> logger)
        {
            _httpClientFactory = httpClientFactory;
            _prospectDataService = prospectDataService;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task ExecuteAsync(Guid prospectId, string configJson, string payloadJson)
        {
            _logger.LogInformation("Executing RealExternalApiStrategy for ProspectId: {ProspectId}", prospectId);

            try
            {
                // 1. Parsear configuración
                var config = JsonNode.Parse(configJson);
                var url = config?["url"]?.ToString();
                var method = config?["method"]?.ToString()?.ToUpperInvariant() ?? "POST";
                var authHeader = config?["auth_header"]?.ToString();
                var timeoutSeconds = config?["timeout_seconds"]?.GetValue<int>() ?? 30;
                var completionEvent = config?["completion_event"]?.ToString();

                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("URL not specified in configuration");
                    return;
                }

                _logger.LogInformation("Calling external API: {Method} {Url}", method, url);

                // 2. Crear HttpClient con nombre (para aplicar políticas Polly)
                var httpClient = _httpClientFactory.CreateClient("ExternalAPIs");
                httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                // 3. Preparar request
                var request = new HttpRequestMessage(new HttpMethod(method), url);

                // Agregar headers de autenticación
                if (!string.IsNullOrEmpty(authHeader))
                {
                    if (authHeader.StartsWith("Bearer "))
                    {
                        httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authHeader.Substring(7));
                    }
                    else
                    {
                        request.Headers.Add("Authorization", authHeader);
                    }
                }

                // 4. Preparar body (merge de config + payload)
                if (method is "POST" or "PUT" or "PATCH")
                {
                    var bodyData = MergePayloadWithConfig(config, payloadJson);
                    request.Content = new StringContent(bodyData, Encoding.UTF8, "application/json");
                    
                    _logger.LogDebug("Request body: {Body}", bodyData);
                }

                // 5. Ejecutar llamada HTTP
                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogInformation(
                        "External API call successful. Url: {Url}, Status: {StatusCode}",
                        url, response.StatusCode);

                    _logger.LogDebug("Response body: {ResponseBody}", responseBody);

                    // 6. Parsear y persistir respuesta en ProspectData
                    await UpdateProspectDataWithResponse(prospectId, responseBody, config);

                    // 7. Emitir evento de completitud (si está configurado)
                    if (!string.IsNullOrEmpty(completionEvent))
                    {
                        await EmitCompletionEvent(completionEvent, prospectId, responseBody, payloadJson);
                    }
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogError(
                        "External API call failed. Url: {Url}, Status: {StatusCode}, Error: {Error}",
                        url, response.StatusCode, errorBody);

                    // Emitir evento de fallo
                    await _eventPublisher.PublishAsync("ExternalApiFailure", new
                    {
                        prospect_id = prospectId,
                        url = url,
                        status_code = (int)response.StatusCode,
                        error = errorBody
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request exception for ProspectId: {ProspectId}", prospectId);
                
                await _eventPublisher.PublishAsync("ExternalApiFailure", new
                {
                    prospect_id = prospectId,
                    error = ex.Message,
                    error_type = "HttpRequestException"
                });
                
                throw; // Propagar para que Polly maneje retry
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timeout for ProspectId: {ProspectId}", prospectId);
                
                await _eventPublisher.PublishAsync("ExternalApiFailure", new
                {
                    prospect_id = prospectId,
                    error = "Request timeout",
                    error_type = "Timeout"
                });
                
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling external API for ProspectId: {ProspectId}", prospectId);
                throw;
            }
        }

        /// <summary>
        /// Combina el payload del evento con datos adicionales de la configuración.
        /// </summary>
        private string MergePayloadWithConfig(JsonNode config, string payloadJson)
        {
            var payload = JsonNode.Parse(payloadJson) ?? new JsonObject();
            
            // Si hay campos adicionales en config, agregarlos al payload
            if (config?["additional_fields"] is JsonNode additionalFields)
            {
                foreach (var kvp in additionalFields.AsObject())
                {
                    payload.AsObject()[kvp.Key] = kvp.Value?.DeepClone();
                }
            }

            return payload.ToJsonString();
        }

        /// <summary>
        /// Actualiza ProspectData con los resultados de la API.
        /// </summary>
        private async Task UpdateProspectDataWithResponse(Guid prospectId, string responseJson, JsonNode config)
        {
            try
            {
                // Si no hay mapeo configurado, no actualizar
                if (config?["response_mapping"] is not JsonNode mappingNode)
                {
                    _logger.LogDebug("No response_mapping configured, skipping ProspectData update");
                    return;
                }

                var responseNode = JsonNode.Parse(responseJson);
                if (responseNode == null)
                {
                    _logger.LogWarning("Failed to parse response JSON");
                    return;
                }

                // Construir JSON con campos mapeados
                var mappedData = new JsonObject();

                foreach (var mapping in mappingNode.AsObject())
                {
                    var targetField = mapping.Key; // Campo en ProspectData
                    var jsonPath = mapping.Value?.ToString(); // Path en respuesta

                    if (string.IsNullOrEmpty(jsonPath))
                        continue;

                    // Extraer valor usando JSONPath simple (ej: "$.data.score")
                    var value = EvaluateJsonPath(responseNode, jsonPath);
                    if (value != null)
                    {
                        mappedData[targetField] = value.DeepClone();
                    }
                }

                if (mappedData.AsObject().Count > 0)
                {
                    // Actualizar ProspectData con los campos mapeados
                    var mappedJson = mappedData.ToJsonString();
                    await _prospectDataService.UpdateProspectDataAsync(prospectId, mappedJson);

                    _logger.LogInformation(
                        "ProspectData updated with {Count} mapped fields for ProspectId: {ProspectId}",
                        mappedData.AsObject().Count, prospectId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ProspectData with response for ProspectId: {ProspectId}", prospectId);
                // No lanzar excepción, solo loggear
            }
        }

        /// <summary>
        /// Evalúa un JSONPath simple (ej: "$.data.score" o "data.score").
        /// </summary>
        private JsonNode? EvaluateJsonPath(JsonNode root, string path)
        {
            // Remover "$." al inicio si existe
            path = path.TrimStart('$', '.');

            var parts = path.Split('.');
            var current = root;

            foreach (var part in parts)
            {
                if (current == null)
                    return null;

                current = current[part];
            }

            return current;
        }

        /// <summary>
        /// Emite un evento de completitud para encadenar reglas.
        /// </summary>
        private async Task EmitCompletionEvent(string eventType, Guid prospectId, string responseJson, string originalPayload)
        {
            try
            {
                // Combinar datos originales con respuesta
                var originalNode = JsonNode.Parse(originalPayload) ?? new JsonObject();
                var responseNode = JsonNode.Parse(responseJson) ?? new JsonObject();

                // Merge simple: agregar campos de response que no existan en original
                foreach (var kvp in responseNode.AsObject())
                {
                    if (!originalNode.AsObject().ContainsKey(kvp.Key))
                    {
                        originalNode.AsObject()[kvp.Key] = kvp.Value?.DeepClone();
                    }
                }

                // Asegurar que prospect_id esté presente
                originalNode.AsObject()["prospect_id"] = prospectId.ToString();

                await _eventPublisher.PublishAsync(eventType, originalNode.ToJsonString());

                _logger.LogInformation(
                    "Completion event '{EventType}' published for ProspectId: {ProspectId}",
                    eventType, prospectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error emitting completion event '{EventType}' for ProspectId: {ProspectId}",
                    eventType, prospectId);
                // No lanzar excepción
            }
        }
    }
}
