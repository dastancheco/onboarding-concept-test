using Microsoft.AspNetCore.Mvc;
using Onboarding.Core.Validation;
using System.Text.Json;

namespace Onboarding.Api.Controllers
{
    /// <summary>
    /// Controller para ejecutar validaciones síncronas desde el frontend
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ValidationController : ControllerBase
    {
        private readonly IValidationOrchestrator _orchestrator;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(
            IValidationOrchestrator orchestrator,
            ILogger<ValidationController> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        /// <summary>
        /// Ejecuta validaciones configuradas para un contexto específico
        /// </summary>
        /// <param name="request">Request con contexto y datos de entrada</param>
        /// <returns>Resultado de validación con detalles</returns>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExecuteValidation(
            [FromBody] ValidationRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Executing validation for context: {Context}",
                    request.TriggerContext);

                // Construir contexto de validación
                var context = new ValidationContext
                {
                    TriggerContext = request.TriggerContext,
                    InputData = request.InputData ?? new(),
                    Email = request.InputData?.GetValueOrDefault("email")?.ToString(),
                    WorkflowId = ExtractWorkflowId(request.InputData),
                    StepId = ExtractStepId(request.InputData)
                };

                // Ejecutar validaciones
                var result = await _orchestrator.ExecuteAsync(
                    request.TriggerContext, context);

                // Construir respuesta
                var response = new ValidationResponse
                {
                    Success = result.Success,
                    Message = result.Success
                        ? "All validations passed"
                        : result.FirstError?.Message ?? "Validation failed",
                    PipelineKey = result.PipelineKey,
                    TotalStepsExecuted = result.TotalStepsExecuted,
                    FailedSteps = result.FailedSteps,
                    WarningSteps = result.WarningSteps,
                    DurationMs = result.TotalDurationMs,
                    EnrichedData = context.EnrichedData,
                    StepResults = result.StepResults.Select(s => new StepResultDto
                    {
                        IsValid = s.IsValid,
                        Message = s.Message,
                        Severity = s.Severity.ToString(),
                        ProviderKey = s.ProviderKey,
                        DurationMs = s.DurationMs,
                        Data = s.Data
                    }).ToList()
                };

                if (result.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing validation");

                return StatusCode(StatusCodes.Status500InternalServerError, new ValidationResponse
                {
                    Success = false,
                    Message = $"Internal error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Ejecuta un pipeline específico por su key
        /// </summary>
        [HttpPost("pipeline/{pipelineKey}")]
        [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ExecutePipeline(
            string pipelineKey,
            [FromBody] ValidationRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Executing validation pipeline: {PipelineKey}",
                    pipelineKey);

                var context = new ValidationContext
                {
                    TriggerContext = request.TriggerContext,
                    InputData = request.InputData ?? new(),
                    Email = request.InputData?.GetValueOrDefault("email")?.ToString(),
                    WorkflowId = ExtractWorkflowId(request.InputData),
                    StepId = ExtractStepId(request.InputData)
                };

                var result = await _orchestrator.ExecutePipelineAsync(
                    pipelineKey, context);

                var response = new ValidationResponse
                {
                    Success = result.Success,
                    Message = result.Success
                        ? "Pipeline validation passed"
                        : result.FirstError?.Message ?? "Pipeline validation failed",
                    PipelineKey = result.PipelineKey,
                    TotalStepsExecuted = result.TotalStepsExecuted,
                    FailedSteps = result.FailedSteps,
                    WarningSteps = result.WarningSteps,
                    DurationMs = result.TotalDurationMs,
                    EnrichedData = context.EnrichedData,
                    StepResults = result.StepResults.Select(s => new StepResultDto
                    {
                        IsValid = s.IsValid,
                        Message = s.Message,
                        Severity = s.Severity.ToString(),
                        ProviderKey = s.ProviderKey,
                        DurationMs = s.DurationMs,
                        Data = s.Data
                    }).ToList()
                };

                if (result.Success)
                {
                    return Ok(response);
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing pipeline");

                return StatusCode(StatusCodes.Status500InternalServerError, new ValidationResponse
                {
                    Success = false,
                    Message = $"Internal error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Endpoint específico para validar email duplicado (shortcut)
        /// </summary>
        [HttpPost("check-duplicate-email")]
        [ProducesResponseType(typeof(DuplicateEmailResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckDuplicateEmail(
            [FromBody] CheckDuplicateEmailRequest request)
        {
            try
            {
                var context = new ValidationContext
                {
                    TriggerContext = "PRE_USER_REGISTRATION",
                    Email = request.Email,
                    WorkflowId = request.WorkflowId,
                    InputData = new Dictionary<string, object>
                    {
                        ["email"] = request.Email,
                        ["workflow_id"] = request.WorkflowId
                    }
                };

                var result = await _orchestrator.ExecuteAsync(
                    "PRE_USER_REGISTRATION", context);

                var response = new DuplicateEmailResponse
                {
                    CanProceed = result.Success,
                    IsDuplicate = !result.Success,
                    Message = result.Success
                        ? "Email is available"
                        : result.FirstError?.Message ?? "Email validation failed",
                    ExistingProspectId = context.EnrichedData.ContainsKey("recent_prospect_id")
                        ? context.EnrichedData["recent_prospect_id"]?.ToString()
                        : null,
                    ExistingWorkflowId = context.EnrichedData.ContainsKey("recent_workflow_id")
                        ? Convert.ToInt32(context.EnrichedData["recent_workflow_id"])
                        : null,
                    ExistingStatus = context.EnrichedData.ContainsKey("recent_status")
                        ? context.EnrichedData["recent_status"]?.ToString()
                        : null,
                    DaysSinceCreated = context.EnrichedData.ContainsKey("days_since_created")
                        ? Convert.ToInt32(context.EnrichedData["days_since_created"])
                        : null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking duplicate email");

                return StatusCode(StatusCodes.Status500InternalServerError, new DuplicateEmailResponse
                {
                    CanProceed = false,
                    IsDuplicate = false,
                    Message = $"Internal error: {ex.Message}"
                });
            }
        }

        private int? ExtractWorkflowId(Dictionary<string, object>? data)
        {
            if (data == null) return null;

            if (data.TryGetValue("workflow_id", out var value))
            {
                if (int.TryParse(value?.ToString(), out var workflowId))
                    return workflowId;
            }

            return null;
        }

        private int? ExtractStepId(Dictionary<string, object>? data)
        {
            if (data == null) return null;

            if (data.TryGetValue("step_id", out var value))
            {
                if (int.TryParse(value?.ToString(), out var stepId))
                    return stepId;
            }

            return null;
        }
    }

    // DTOs

    public record ValidationRequest(
        string TriggerContext,
        Dictionary<string, object>? InputData
    );

    public class ValidationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string PipelineKey { get; set; } = string.Empty;
        public int TotalStepsExecuted { get; set; }
        public int FailedSteps { get; set; }
        public int WarningSteps { get; set; }
        public int DurationMs { get; set; }
        public Dictionary<string, object> EnrichedData { get; set; } = new();
        public List<StepResultDto> StepResults { get; set; } = new();
    }

    public class StepResultDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string ProviderKey { get; set; } = string.Empty;
        public int DurationMs { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public record CheckDuplicateEmailRequest(
        string Email,
        int WorkflowId
    );

    public class DuplicateEmailResponse
    {
        public bool CanProceed { get; set; }
        public bool IsDuplicate { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ExistingProspectId { get; set; }
        public int? ExistingWorkflowId { get; set; }
        public string? ExistingStatus { get; set; }
        public int? DaysSinceCreated { get; set; }
    }
}
