using Onboarding.TestClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onboarding.TestClient.Services;

public class OnboardingApiClient : IOnboardingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OnboardingApiClient> _logger;

    public OnboardingApiClient(HttpClient httpClient, ILogger<OnboardingApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // === COMANDOS REST (Sincronos) ===

    /// <summary>
    /// Crea un nuevo prospecto (comando REST sincrónico).
    /// Retorna el prospectId inmediatamente.
    /// </summary>
    public async Task<CreateProspectResponse?> CreateProspectAsync(CreateProspectRequest request)
    {
        try
        {
            _logger.LogInformation("Creating prospect for email: {Email}", request.Email);
            
            var response = await _httpClient.PostAsJsonAsync("api/prospects", request);
            
            // Log detallado del response
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Response Status: {Status}, Content: {Content}", 
                response.StatusCode, responseContent);
            
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<CreateProspectResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (result != null)
                    {
                        _logger.LogInformation("Prospect created successfully: {ProspectId}", result.ProspectId);
                        return result;
                    }
                    
                    _logger.LogError("Deserialization returned null. JSON: {Json}", responseContent);
                    return null;
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON deserialization failed. Content: {Content}", responseContent);
                    return null;
                }
            }
            
            _logger.LogWarning("Failed to create prospect. Status: {Status}, Error: {Error}", 
                response.StatusCode, responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prospect");
            return null;
        }
    }

    public async Task<SubmitStepDataResponse?> SubmitStepDataAsync(Guid prospectId, SubmitStepDataRequest request)
    {
        try
        {
            _logger.LogInformation("Submitting step data for prospect: {ProspectId}", prospectId);
            
            var response = await _httpClient.PostAsJsonAsync($"api/prospects/{prospectId}/steps/current/submit", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SubmitStepDataResponse>();
                _logger.LogInformation("Step data submitted successfully");
                return result;
            }
            
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to submit step data. Status: {Status}, Error: {Error}", 
                response.StatusCode, error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting step data for {ProspectId}", prospectId);
            return null;
        }
    }

    public async Task<AdvanceStepResponse?> AdvanceToNextStepAsync(Guid prospectId)
    {
        try
        {
            _logger.LogInformation("Advancing to next step for prospect: {ProspectId}", prospectId);
            
            var response = await _httpClient.PostAsync($"api/prospects/{prospectId}/steps/advance", null);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AdvanceStepResponse>();
                _logger.LogInformation("Advanced to step: {StepId}", result?.CurrentStepId);
                return result;
            }
            
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to advance step. Status: {Status}, Error: {Error}", 
                response.StatusCode, error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error advancing step for {ProspectId}", prospectId);
            return null;
        }
    }

    // === CONSULTAS (Queries) ===

    public async Task<ProspectResponse?> GetProspectAsync(Guid prospectId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProspectResponse>($"api/prospects/{prospectId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prospect {ProspectId}", prospectId);
            return null;
        }
    }

    public async Task<ProspectDataResponse?> GetProspectDataAsync(Guid prospectId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProspectDataResponse>($"api/prospects/{prospectId}/data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prospect data for {ProspectId}", prospectId);
            return null;
        }
    }

    public async Task<CurrentStepResponse?> GetCurrentStepAsync(Guid prospectId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CurrentStepResponse>($"api/prospects/{prospectId}/steps/current");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current step for {ProspectId}", prospectId);
            return null;
        }
    }

    public async Task<WorkflowStepsResponse?> GetAllStepsAsync(Guid prospectId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<WorkflowStepsResponse>($"api/prospects/{prospectId}/steps/all");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all steps for {ProspectId}", prospectId);
            return null;
        }
    }

    // === STATUS MANAGEMENT ===

    public async Task<StatusHistoryResponse?> GetStatusHistoryAsync(Guid prospectId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StatusHistoryResponse>($"api/prospects/{prospectId}/status-history");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status history for {ProspectId}", prospectId);
            return null;
        }
    }

    public async Task<AvailableTransitionsResponse?> GetAvailableTransitionsAsync(Guid prospectId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AvailableTransitionsResponse>($"api/prospects/{prospectId}/available-transitions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available transitions for {ProspectId}", prospectId);
            return null;
        }
    }

    public async Task<bool> UpdateStatusAsync(Guid prospectId, UpdateStatusRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/prospects/{prospectId}/status", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for {ProspectId}", prospectId);
            return false;
        }
    }

    // === WORKFLOWS ===

    public async Task<List<WorkflowSummary>> GetWorkflowsAsync()
    {
        var response = await _httpClient.GetAsync("/api/config/workflows");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<WorkflowSummary>>() ?? new List<WorkflowSummary>();
    }

    public async Task<WorkflowDeterminationResult?> DetermineWorkflowAsync(string payloadJson)
    {
        var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/api/config/workflows/determine", content);
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<WorkflowDeterminationResult>();
        }
        
        return null;
    }

    public async Task<WorkflowConfiguration?> GetWorkflowConfigurationAsync(int workflowId)
    {
        var response = await _httpClient.GetAsync($"/api/config/workflows/{workflowId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkflowConfiguration>();
    }

    // === DASHBOARD ===

    public async Task<DashboardProspectListResponse?> GetDashboardProspectsAsync(int? workflowId = null, string? status = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var queryParams = new List<string>();
            if (workflowId.HasValue) queryParams.Add($"workflowId={workflowId}");
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
            queryParams.Add($"page={page}");
            queryParams.Add($"pageSize={pageSize}");

            var query = string.Join("&", queryParams);
            var url = $"api/dashboard/prospects?{query}";

            _logger.LogInformation("Getting dashboard prospects: {Url}", url);
            return await _httpClient.GetFromJsonAsync<DashboardProspectListResponse>(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard prospects");
            return null;
        }
    }

    public async Task<DashboardStatisticsResponse?> GetDashboardStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Getting dashboard statistics");
            return await _httpClient.GetFromJsonAsync<DashboardStatisticsResponse>("api/dashboard/statistics");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard statistics");
            return null;
        }
    }

    public async Task<DashboardTimelineResponse?> GetDashboardTimelineAsync(int days = 7, int limit = 50)
    {
        try
        {
            var url = $"api/dashboard/timeline?days={days}&limit={limit}";
            _logger.LogInformation("Getting dashboard timeline: {Url}", url);
            return await _httpClient.GetFromJsonAsync<DashboardTimelineResponse>(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard timeline");
            return null;
        }
    }

    public async Task<DashboardSearchResponse?> SearchProspectsAsync(string query)
    {
        try
        {
            var url = $"api/dashboard/search?query={Uri.EscapeDataString(query)}";
            _logger.LogInformation("Searching prospects: {Query}", query);
            return await _httpClient.GetFromJsonAsync<DashboardSearchResponse>(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching prospects");
            return null;
        }
    }

    // === EVENTOS (Asíncronos - Fire and Forget) ===

    public async Task<EventResponse> SendEventAsync(string eventType, string payloadJson)
    {
        var payload = new
        {
            EventType = eventType,
            PayloadJson = payloadJson
        };

        var response = await _httpClient.PostAsJsonAsync("/api/events/push", payload);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<EventResponse>();
            return result ?? new EventResponse { Status = "Unknown", CorrelationId = "" };
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return new EventResponse
            {
                Status = "Error",
                Error = errorContent,
                CorrelationId = ""
            };
        }
    }

    // === LEGACY/COMPATIBILITY ===

    public async Task<bool> StartOnboardingAsync(StartOnboardingRequest request)
    {
        try
        {
            var apiRequest = new
            {
                EventType = request.EventType,
                PayloadJson = JsonSerializer.Serialize(request.Payload)
            };
            var response = await _httpClient.PostAsJsonAsync("api/events/push", apiRequest);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Onboarding started successfully: {Content}", content);
                return true;
            }
            
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to start onboarding. Status: {Status}, Error: {Error}", 
                response.StatusCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling StartOnboarding API");
            return false;
        }
    }

    public async Task<ProspectDetailsDto?> GetProspectDetailsAsync(string prospectId)
    {
        var response = await _httpClient.GetAsync($"/api/prospects/{prospectId}");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ProspectDetailsDto>();
        }
        
        return null;
    }

    public async Task<List<StatusHistoryDto>> GetProspectHistoryAsync(string prospectId)
    {
        var response = await _httpClient.GetAsync($"/api/prospects/{prospectId}/history");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<StatusHistoryDto>>() 
                ?? new List<StatusHistoryDto>();
        }
        
        return new List<StatusHistoryDto>();
    }

    public async Task<TransitionResponse> TransitionProspectStatusAsync(string prospectId, TransitionRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/prospects/{prospectId}/transition", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<TransitionResponse>();
            return result ?? new TransitionResponse { Success = false, Message = "Unknown response" };
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return new TransitionResponse
            {
                Success = false,
                Message = errorContent
            };
        }
    }

    public async Task<List<TransitionDto>> GetAvailableTransitionsAsync(string prospectId)
    {
        var response = await _httpClient.GetAsync($"/api/prospects/{prospectId}/transitions");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<TransitionDto>>() 
                ?? new List<TransitionDto>();
        }
        
        return new List<TransitionDto>();
    }
}
