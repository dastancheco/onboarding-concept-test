using System.ComponentModel.DataAnnotations;

namespace OvexDataModelingTest.Entities.Config
{
    /// <summary>
    /// Define un paso dentro de un pipeline de validación
    /// </summary>
    public class PipelineStep
    {
        [Key]
        public int PipelineStepId { get; set; }

        public int PipelineId { get; set; }
        public int ProviderId { get; set; }

        /// <summary>
        /// Orden de ejecución dentro del pipeline
        /// </summary>
        public int ExecutionOrder { get; set; }

        /// <summary>
        /// Condición para ejecutar este step (expresión evaluable por RuleEngine)
        /// </summary>
        public string? ExecuteIf { get; set; }

        /// <summary>
        /// Acción en caso de fallo: STOP, CONTINUE, SKIP_REMAINING, REDIRECT
        /// </summary>
        [MaxLength(50)]
        public string OnFailureAction { get; set; } = "STOP";

        /// <summary>
        /// Mensaje personalizado en caso de fallo
        /// </summary>
        public string? OnFailureMessage { get; set; }

        /// <summary>
        /// Configuración override que sobrescribe la del provider
        /// </summary>
        public string? ConfigOverrideJson { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
