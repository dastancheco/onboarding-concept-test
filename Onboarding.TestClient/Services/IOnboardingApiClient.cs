using Onboarding.TestClient.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onboarding.TestClient.Services;

public interface IOnboardingApiClient
{
    // === COMANDOS REST (Sincronos) ===
    
    /// <summary>
    /// Crea un nuevo prospecto de forma sincrónica (comando REST).
    /// Retorna el prospectId inmediatamente para que el wizard pueda continuar.
    /// </summary>
    Task<CreateProspectResponse?> CreateProspectAsync(CreateProspectRequest request);
    
    /// <summary>
    /// Envía datos de un step específico (comando REST).
    /// </summary>
    Task<SubmitStepDataResponse?> SubmitStepDataAsync(Guid prospectId, SubmitStepDataRequest request);
    
    /// <summary>
    /// Avanza al siguiente step (comando REST).
    /// </summary>
    Task<AdvanceStepResponse?> AdvanceToNextStepAsync(Guid prospectId);
    
    // === CONSULTAS (Queries) ===
    
    Task<ProspectResponse?> GetProspectAsync(Guid prospectId);
    Task<ProspectDataResponse?> GetProspectDataAsync(Guid prospectId);
    Task<CurrentStepResponse?> GetCurrentStepAsync(Guid prospectId);
    Task<WorkflowStepsResponse?> GetAllStepsAsync(Guid prospectId);
    
    // === STATUS MANAGEMENT ===
    
    Task<StatusHistoryResponse?> GetStatusHistoryAsync(Guid prospectId);
    Task<AvailableTransitionsResponse?> GetAvailableTransitionsAsync(Guid prospectId);
    Task<bool> UpdateStatusAsync(Guid prospectId, UpdateStatusRequest request);

    // === WORKFLOWS ===
    
    Task<List<WorkflowSummary>> GetWorkflowsAsync();
    Task<WorkflowDeterminationResult?> DetermineWorkflowAsync(string payloadJson);
    Task<WorkflowConfiguration?> GetWorkflowConfigurationAsync(int workflowId);

    // === DASHBOARD ===
    
    Task<DashboardProspectListResponse?> GetDashboardProspectsAsync(int? workflowId = null, string? status = null, int page = 1, int pageSize = 20);
    Task<DashboardStatisticsResponse?> GetDashboardStatisticsAsync();
    Task<DashboardTimelineResponse?> GetDashboardTimelineAsync(int days = 7, int limit = 50);
    Task<DashboardSearchResponse?> SearchProspectsAsync(string query);

    // === EVENTOS (Asíncronos - Fire and Forget) ===
    
    /// <summary>
    /// Envía un evento al orquestador para ejecutar reglas de negocio.
    /// Este es asíncrono y NO espera respuesta con datos de negocio.
    /// </summary>
    Task<EventResponse> SendEventAsync(string eventType, string payloadJson);

    // === LEGACY/COMPATIBILITY ===
    
    Task<bool> StartOnboardingAsync(StartOnboardingRequest request);
    Task<ProspectDetailsDto?> GetProspectDetailsAsync(string prospectId);
    Task<List<StatusHistoryDto>> GetProspectHistoryAsync(string prospectId);
    Task<TransitionResponse> TransitionProspectStatusAsync(string prospectId, TransitionRequest request);
    Task<List<TransitionDto>> GetAvailableTransitionsAsync(string prospectId);
}
