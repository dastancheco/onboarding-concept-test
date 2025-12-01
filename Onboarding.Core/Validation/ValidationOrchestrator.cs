using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.Config;
using System.Text.Json;

namespace Onboarding.Core.Validation
{
    /// <summary>
    /// Orquestador que coordina la ejecución de pipelines de validación
    /// </summary>
    public class ValidationOrchestrator : IValidationOrchestrator
    {
        private readonly IRepository<ValidationPipeline> _pipelineRepo;
        private readonly IRepository<PipelineStep> _stepRepo;
        private readonly IValidationProviderFactory _providerFactory;
        private readonly IRuleEngine _ruleEngine;
        private readonly ILogger<ValidationOrchestrator> _logger;

        public ValidationOrchestrator(
            IRepository<ValidationPipeline> pipelineRepo,
            IRepository<PipelineStep> stepRepo,
            IValidationProviderFactory providerFactory,
            IRuleEngine ruleEngine,
            ILogger<ValidationOrchestrator> logger)
        {
            _pipelineRepo = pipelineRepo;
            _stepRepo = stepRepo;
            _providerFactory = providerFactory;
            _ruleEngine = ruleEngine;
            _logger = logger;
        }

        public async Task<ValidationPipelineResult> ExecuteAsync(
            string triggerContext,
            ValidationContext context)
        {
            var result = new ValidationPipelineResult
            {
                CorrelationId = context.CorrelationId
            };

            var startTime = DateTime.UtcNow;

            _logger.LogInformation(
                "Executing validation pipelines for context: {TriggerContext} (CorrelationId: {CorrelationId})",
                triggerContext, context.CorrelationId);

            // 1. Buscar pipelines activos para este contexto
            var allPipelines = await _pipelineRepo.GetAllAsync();
            var pipelines = allPipelines.Where(p =>
                p.TriggerContext == triggerContext &&
                p.IsActive &&
                (p.WorkflowId == null || p.WorkflowId == context.WorkflowId) &&
                (p.StepId == null || p.StepId == context.StepId)).ToList();

            if (!pipelines.Any())
            {
                _logger.LogInformation(
                    "No validation pipelines found for context: {Context}",
                    triggerContext);
                return result; // Success sin validaciones
            }

            _logger.LogInformation(
                "Found {Count} validation pipelines to execute",
                pipelines.Count);

            // 2. Ordenar por prioridad y ejecutar
            foreach (var pipeline in pipelines.OrderBy(p => p.Priority))
            {
                _logger.LogInformation(
                    "Executing validation pipeline: {PipelineKey} (Priority: {Priority})",
                    pipeline.PipelineKey, pipeline.Priority);

                var pipelineResult = await ExecutePipelineAsync(
                    pipeline.PipelineKey, context);

                // Combinar resultados
                result.StepResults.AddRange(pipelineResult.StepResults);
                result.TotalStepsExecuted += pipelineResult.TotalStepsExecuted;

                // Si falla y es requerido, detener
                if (!pipelineResult.Success && pipeline.RequireAllPass)
                {
                    _logger.LogWarning(
                        "Pipeline {PipelineKey} failed and RequireAllPass=true. Stopping execution.",
                        pipeline.PipelineKey);
                    result.Success = false;
                    break;
                }
            }

            result.FailedSteps = result.StepResults.Count(s => !s.IsValid);
            result.WarningSteps = result.StepResults.Count(s =>
                s.Severity == ValidationSeverity.Warning);
            result.TotalDurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Validation completed. Success: {Success}, TotalSteps: {TotalSteps}, Failed: {Failed}, Warnings: {Warnings}, Duration: {Duration}ms",
                result.Success, result.TotalStepsExecuted, result.FailedSteps, 
                result.WarningSteps, result.TotalDurationMs);

            return result;
        }

        public async Task<ValidationPipelineResult> ExecutePipelineAsync(
            string pipelineKey,
            ValidationContext context)
        {
            var result = new ValidationPipelineResult
            {
                PipelineKey = pipelineKey,
                CorrelationId = context.CorrelationId
            };

            var startTime = DateTime.UtcNow;

            // 1. Obtener pipeline
            var allPipelines = await _pipelineRepo.GetAllAsync();
            var pipeline = allPipelines.FirstOrDefault(p =>
                p.PipelineKey == pipelineKey && p.IsActive);

            if (pipeline == null)
            {
                _logger.LogWarning("Pipeline not found: {PipelineKey}", pipelineKey);
                result.Success = false;
                return result;
            }

            // 2. Obtener steps del pipeline
            var allSteps = await _stepRepo.GetAllAsync();
            var steps = allSteps.Where(s =>
                s.PipelineId == pipeline.PipelineId && s.IsActive).ToList();

            if (!steps.Any())
            {
                _logger.LogWarning(
                    "No active steps found for pipeline: {PipelineKey}",
                    pipelineKey);
                return result; // Success sin steps
            }

            _logger.LogInformation(
                "Executing {Count} steps for pipeline: {PipelineKey}",
                steps.Count, pipelineKey);

            // 3. Ejecutar steps en orden
            foreach (var step in steps.OrderBy(s => s.ExecutionOrder))
            {
                // 3.1. Evaluar condición del step
                if (!string.IsNullOrWhiteSpace(step.ExecuteIf))
                {
                    var contextJson = JsonSerializer.Serialize(context.InputData);
                    var shouldExecute = _ruleEngine.Evaluate(step.ExecuteIf, contextJson);

                    if (!shouldExecute)
                    {
                        _logger.LogDebug(
                            "Skipping step {StepId}: condition not met: {Condition}",
                            step.PipelineStepId, step.ExecuteIf);
                        continue;
                    }
                }

                // 3.2. Obtener provider
                var provider = await _providerFactory.GetProviderAsync(step.ProviderId);

                if (provider == null)
                {
                    _logger.LogError(
                        "Provider not found for step {StepId} (ProviderId: {ProviderId})",
                        step.PipelineStepId, step.ProviderId);
                    continue;
                }

                // 3.3. Configurar provider (con override si existe)
                if (!string.IsNullOrWhiteSpace(step.ConfigOverrideJson))
                {
                    provider.Configure(step.ConfigOverrideJson);
                }

                // 3.4. Ejecutar validación
                var stepStartTime = DateTime.UtcNow;

                try
                {
                    _logger.LogDebug(
                        "Executing validation step: {ProviderKey} (Order: {Order})",
                        provider.ProviderKey, step.ExecutionOrder);

                    var stepResult = await provider.ValidateAsync(context);
                    stepResult.DurationMs = (int)(DateTime.UtcNow - stepStartTime).TotalMilliseconds;

                    // Usar mensaje personalizado si está configurado y hay fallo
                    if (!stepResult.IsValid && !string.IsNullOrWhiteSpace(step.OnFailureMessage))
                    {
                        stepResult.Message = step.OnFailureMessage;
                    }

                    result.StepResults.Add(stepResult);
                    result.TotalStepsExecuted++;

                    _logger.LogDebug(
                        "Step completed. Valid: {IsValid}, Message: {Message}, Duration: {Duration}ms",
                        stepResult.IsValid, stepResult.Message, stepResult.DurationMs);

                    // 3.5. Enriquecer contexto con datos del step
                    foreach (var kvp in stepResult.Data)
                    {
                        context.EnrichedData[kvp.Key] = kvp.Value;
                    }

                    // 3.6. Manejar fallo
                    if (!stepResult.IsValid)
                    {
                        _logger.LogWarning(
                            "Validation step failed: {ProviderKey} - {Message}",
                            stepResult.ProviderKey, stepResult.Message);

                        switch (step.OnFailureAction)
                        {
                            case "STOP":
                                _logger.LogInformation("OnFailureAction=STOP. Stopping pipeline.");
                                result.Success = false;
                                result.TotalDurationMs =
                                    (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                                return result;

                            case "SKIP_REMAINING":
                                _logger.LogInformation("OnFailureAction=SKIP_REMAINING. Skipping remaining steps.");
                                result.Success = false;
                                goto exit_loop;

                            case "CONTINUE":
                                _logger.LogDebug("OnFailureAction=CONTINUE. Continuing with next step.");
                                // Continuar con el siguiente step
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error executing validation step: {ProviderKey}",
                        provider.ProviderKey);

                    var errorResult = new ValidationStepResult
                    {
                        IsValid = false,
                        Message = $"Error interno: {ex.Message}",
                        Severity = ValidationSeverity.Critical,
                        ProviderKey = provider.ProviderKey,
                        DurationMs = (int)(DateTime.UtcNow - stepStartTime).TotalMilliseconds
                    };

                    result.StepResults.Add(errorResult);
                    result.TotalStepsExecuted++;

                    if (pipeline.StopOnFirstFailure)
                    {
                        _logger.LogError("StopOnFirstFailure=true. Stopping pipeline due to error.");
                        result.Success = false;
                        break;
                    }
                }
            }

        exit_loop:
            result.FailedSteps = result.StepResults.Count(s => !s.IsValid);
            result.WarningSteps = result.StepResults.Count(s =>
                s.Severity == ValidationSeverity.Warning);
            result.TotalDurationMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            return result;
        }
    }
}
