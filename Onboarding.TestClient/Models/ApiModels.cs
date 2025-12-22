using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using Onboarding.TestClient.Converters;

namespace Onboarding.TestClient.Models;

public class StartOnboardingRequest
{
    public string EventType { get; set; } = string.Empty;
    public Guid ProspectId { get; set; }
    public Guid UserId { get; set; }
    public int WorkflowId { get; set; }
    public object? Payload { get; set; }
}

public class ProspectResponse
{
    public Guid ProspectId { get; set; }
    public Guid UserId { get; set; }
    public int WorkflowId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? CurrentStepId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [JsonConverter(typeof(JsonStringToDictionaryConverter))]
    public Dictionary<string, object>? Data { get; set; }
}

public class ProspectDataResponse
{
    public Guid ProspectId { get; set; }

    [JsonConverter(typeof(JsonStringToDictionaryConverter))]
    public Dictionary<string, object>? Data { get; set; }

    public DateTime RetrievedAt { get; set; }
}

public class StatusHistoryResponse
{
    public Guid ProspectId { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public int HistoryCount { get; set; }
    public List<StatusHistoryItem> History { get; set; } = new();
}

public class StatusHistoryItem
{
    public Guid HistoryId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Metadata { get; set; }
}

public class AvailableTransitionsResponse
{
    public Guid ProspectId { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public bool IsTerminalState { get; set; }
    public List<string> AvailableTransitions { get; set; } = new();
}

public class UpdateStatusRequest
{
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? ChangedBy { get; set; }
    public string? Metadata { get; set; }
}

public class CurrentStepResponse
{
    public Guid ProspectId { get; set; }
    public int? StepId { get; set; }
    public string? StepName { get; set; }
    public int? StepOrder { get; set; }
    public int? PhaseId { get; set; }
    public string? PhaseName { get; set; }
    public int? PhaseOrder { get; set; }
    public string? Message { get; set; }
}

public class WorkflowStepsResponse
{
    public Guid ProspectId { get; set; }
    public int WorkflowId { get; set; }
    public int? CurrentStepId { get; set; }
    public List<PhaseInfo> Workflow { get; set; } = new();
}

public class PhaseInfo
{
    public int PhaseId { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public int PhaseOrder { get; set; }
    public List<StepInfo> Steps { get; set; } = new();
}

public class StepInfo
{
    public int StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public bool IsCurrent { get; set; }
}

public class SubmitStepDataRequest
{
    public string DataJson { get; set; } = string.Empty;
}

public class SubmitStepDataResponse
{
    public string Message { get; set; } = string.Empty;
    public Guid ProspectId { get; set; }
    public int StepId { get; set; }
    public string ValidationResult { get; set; } = string.Empty;
}

public class AdvanceStepResponse
{
    public string Message { get; set; } = string.Empty;
    public Guid ProspectId { get; set; }
    public int? PreviousStepId { get; set; }
    public int? CurrentStepId { get; set; }
    public string? StepName { get; set; }
    public bool Completed { get; set; }
}

// Workflow Determination
public class WorkflowDeterminationResult
{
    [JsonPropertyName("workflowId")]
    public int WorkflowId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("subTypeKey")]
    public string? SubTypeKey { get; set; }
}

// Workflow Models
public class WorkflowSummary
{
    [JsonPropertyName("workflowId")]
    public int WorkflowId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("subTypeKey")]
    public string? SubTypeKey { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }
}

public class WorkflowConfiguration
{
    [JsonPropertyName("workflowId")]
    public int WorkflowId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("subTypeKey")]
    public string? SubTypeKey { get; set; }

    [JsonPropertyName("phases")]
    public List<PhaseConfiguration> Phases { get; set; } = new();
}

public class PhaseConfiguration
{
    [JsonPropertyName("phaseId")]
    public int PhaseId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("steps")]
    public List<StepConfiguration> Steps { get; set; } = new();
}

public class StepConfiguration
{
    [JsonPropertyName("stepId")]
    public int StepId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("fields")]
    public List<FieldConfiguration> Fields { get; set; } = new();
}

public class FieldConfiguration
{
    [JsonPropertyName("fieldId")]
    public int FieldId { get; set; }

    [JsonPropertyName("fieldKey")]
    public string FieldKey { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("baseConfig")]
    public JsonElement BaseConfig { get; set; }

    [JsonPropertyName("overrideConfig")]
    public JsonElement? OverrideConfig { get; set; }

    // Propiedades helper para acceder a la configuración
    public string Label => GetConfigValue("label") ?? FieldKey;
    public bool IsRequired => GetConfigValue("is_required") == "true" || GetConfigValue("required") == "true";
    public string? Placeholder => GetConfigValue("placeholder");
    public string? HelpText => GetConfigValue("help_text");

    // Validaciones de longitud (strings)
    public int? MinLength => int.TryParse(GetConfigValue("min_length"), out var val) ? val : null;
    public int? MaxLength => int.TryParse(GetConfigValue("max_length"), out var val) ? val : null;

    // Validaciones de rango (números)
    public int? MinValue => int.TryParse(GetConfigValue("min"), out var val) ? val : null;
    public int? MaxValue => int.TryParse(GetConfigValue("max"), out var val) ? val : null;

    // Validaciones de fecha
    public DateTime? MinDate => DateTime.TryParse(GetConfigValue("min_date"), out var val) ? val : (DateTime?)null;
    public DateTime? MaxDate => DateTime.TryParse(GetConfigValue("max_date"), out var val) ? val : (DateTime?)null;

    // Validaciones de patrón
    public string? Pattern => GetConfigValue("pattern");
    public string? Validator => GetConfigValue("validator"); // ej: "EMAIL", "PHONE", "RFC"

    // Otras configuraciones
    public List<string>? AllowedValues => GetConfigArrayValue("allowed_values");

    private string? GetConfigValue(string key)
    {
        // Primero buscar en override, luego en base
        if (OverrideConfig.HasValue && OverrideConfig.Value.ValueKind == JsonValueKind.Object)
        {
            if (OverrideConfig.Value.TryGetProperty(key, out var overrideVal))
            {
                return ConvertJsonElementToString(overrideVal);
            }
        }

        if (BaseConfig.ValueKind == JsonValueKind.Object)
        {
            if (BaseConfig.TryGetProperty(key, out var baseVal))
            {
                return ConvertJsonElementToString(baseVal);
            }
        }

        return null;
    }

    private string? ConvertJsonElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.GetRawText(),
            JsonValueKind.Object => element.GetRawText(),
            _ => element.GetRawText()
        };
    }

    private List<string>? GetConfigArrayValue(string key)
    {
        JsonElement? config = null;

        if (OverrideConfig.HasValue && OverrideConfig.Value.ValueKind == JsonValueKind.Object)
        {
            if (OverrideConfig.Value.TryGetProperty(key, out var overrideVal))
            {
                config = overrideVal;
            }
        }

        if (config == null && BaseConfig.ValueKind == JsonValueKind.Object)
        {
            if (BaseConfig.TryGetProperty(key, out var baseVal))
            {
                config = baseVal;
            }
        }

        if (config.HasValue && config.Value.ValueKind == JsonValueKind.Array)
        {
            var result = new List<string>();
            foreach (var item in config.Value.EnumerateArray())
            {
                var str = ConvertJsonElementToString(item);
                if (str != null) result.Add(str);
            }
            return result;
        }

        return null;
    }

    /// <summary>
    /// Método genérico para obtener valores de configuración como tipo específico.
    /// Útil para validaciones personalizadas que no tienen property helper.
    /// </summary>
    /// <example>
    /// var minAge = field.GetConfigValueAs&lt;int&gt;("min_age");
    /// var maxDate = field.GetConfigValueAs&lt;DateTime&gt;("expiry_date");
    /// </example>
    public T? GetConfigValueAs<T>(string key) where T : struct
    {
        var value = GetConfigValue(key);
        if (string.IsNullOrEmpty(value)) return null;

        try
        {
            // Manejo especial para DateTime
            if (typeof(T) == typeof(DateTime))
            {
                if (DateTime.TryParse(value, out var dateVal))
                {
                    return (T)(object)dateVal;
                }
                return null;
            }

            // Para otros tipos (int, decimal, bool, etc.)
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Obtiene un valor booleano de configuración con valor por defecto.
    /// </summary>
    public bool GetConfigBool(string key, bool defaultValue = false)
    {
        var value = GetConfigValue(key);
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return value.ToLower() == "true" || value == "1";
    }
}

// Event Models
public class EventResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}

// Prospect Models
public class ProspectDetailsDto
{
    [JsonPropertyName("prospectId")]
    public string ProspectId { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("workflowId")]
    public int WorkflowId { get; set; }

    [JsonPropertyName("currentStepId")]
    public int? CurrentStepId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("prospectData")]
    public Dictionary<string, object>? ProspectData { get; set; }
}

public class StatusHistoryDto
{
    [JsonPropertyName("historyId")]
    public int HistoryId { get; set; }

    [JsonPropertyName("previousStatus")]
    public string? PreviousStatus { get; set; }

    [JsonPropertyName("newStatus")]
    public string NewStatus { get; set; } = string.Empty;

    [JsonPropertyName("changedBy")]
    public string ChangedBy { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }

    [JsonPropertyName("changedAt")]
    public DateTime ChangedAt { get; set; }
}

public class TransitionRequest
{
    [JsonPropertyName("newStatus")]
    public string NewStatus { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("changedBy")]
    public string ChangedBy { get; set; } = "SYSTEM";

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}

public class TransitionResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("newStatus")]
    public string? NewStatus { get; set; }
}

public class TransitionDto
{
    [JsonPropertyName("fromStatus")]
    public string FromStatus { get; set; } = string.Empty;

    [JsonPropertyName("toStatus")]
    public string ToStatus { get; set; } = string.Empty;

    [JsonPropertyName("requiresReason")]
    public bool RequiresReason { get; set; }
}

// === COMANDOS REST (Request/Response Models) ===

/// <summary>
/// Request para crear un nuevo prospecto (comando REST).
/// </summary>
public class CreateProspectRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("appId")]
    public string AppId { get; set; } = string.Empty;

    [JsonPropertyName("clientType")]
    public string ClientType { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

/// <summary>
/// Response de creación de prospecto (comando REST).
/// Contiene el prospectId y datos necesarios para el wizard.
/// </summary>
public class CreateProspectResponse
{
    [JsonPropertyName("prospectId")]
    public Guid ProspectId { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("workflowId")]
    public int WorkflowId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
