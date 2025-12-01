namespace Onboarding.Core.Validation
{
    /// <summary>
    /// Orquestador de pipelines de validación
    /// </summary>
    public interface IValidationOrchestrator
    {
        /// <summary>
        /// Ejecuta todos los pipelines configurados para un contexto específico
        /// </summary>
        Task<ValidationPipelineResult> ExecuteAsync(
            string triggerContext,
            ValidationContext context);

        /// <summary>
        /// Ejecuta un pipeline específico por su key
        /// </summary>
        Task<ValidationPipelineResult> ExecutePipelineAsync(
            string pipelineKey,
            ValidationContext context);
    }
}
