namespace Onboarding.Core.Validation
{
    /// <summary>
    /// Severidad de un resultado de validación
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Contexto compartido entre validadores
    /// </summary>
    public class ValidationContext
    {
        /// <summary>
        /// Contexto de ejecución: PRE_USER_REGISTRATION, PRE_STEP_SUBMISSION, etc.
        /// </summary>
        public string TriggerContext { get; set; } = string.Empty;

        /// <summary>
        /// Datos de entrada originales
        /// </summary>
        public Dictionary<string, object> InputData { get; set; } = new();

        /// <summary>
        /// Email extraído (si aplica)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Workflow ID (si aplica)
        /// </summary>
        public int? WorkflowId { get; set; }

        /// <summary>
        /// Step ID (si aplica)
        /// </summary>
        public int? StepId { get; set; }

        /// <summary>
        /// Datos enriquecidos por validadores previos
        /// </summary>
        public Dictionary<string, object> EnrichedData { get; set; } = new();

        /// <summary>
        /// ID de correlación para tracing
        /// </summary>
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Momento de inicio de la validación
        /// </summary>
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Service provider para resolver dependencias
        /// </summary>
        public IServiceProvider? Services { get; set; }
    }

    /// <summary>
    /// Resultado de un paso individual de validación
    /// </summary>
    public class ValidationStepResult
    {
        public bool IsValid { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Info;

        /// <summary>
        /// Datos adicionales que otros steps pueden usar
        /// </summary>
        public Dictionary<string, object> Data { get; set; } = new();

        /// <summary>
        /// Key del provider que ejecutó esta validación
        /// </summary>
        public string ProviderKey { get; set; } = string.Empty;

        /// <summary>
        /// Duración de ejecución en milisegundos
        /// </summary>
        public int DurationMs { get; set; }

        /// <summary>
        /// Datos de salida serializados (para auditoría)
        /// </summary>
        public string? OutputDataJson { get; set; }
    }

    /// <summary>
    /// Resultado agregado de un pipeline completo
    /// </summary>
    public class ValidationPipelineResult
    {
        public bool Success { get; set; } = true;
        public string PipelineKey { get; set; } = string.Empty;

        /// <summary>
        /// Resultados de todos los steps ejecutados
        /// </summary>
        public List<ValidationStepResult> StepResults { get; set; } = new();

        /// <summary>
        /// Total de steps ejecutados
        /// </summary>
        public int TotalStepsExecuted { get; set; }

        /// <summary>
        /// Cantidad de steps que fallaron
        /// </summary>
        public int FailedSteps { get; set; }

        /// <summary>
        /// Cantidad de steps con warnings
        /// </summary>
        public int WarningSteps { get; set; }

        /// <summary>
        /// Primer error crítico encontrado
        /// </summary>
        public ValidationStepResult? FirstError =>
            StepResults.FirstOrDefault(s => !s.IsValid &&
                s.Severity >= ValidationSeverity.Error);

        /// <summary>
        /// Duración total en milisegundos
        /// </summary>
        public int TotalDurationMs { get; set; }

        /// <summary>
        /// ID de correlación
        /// </summary>
        public string CorrelationId { get; set; } = string.Empty;
    }
}
