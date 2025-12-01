using System.ComponentModel.DataAnnotations;

namespace OvexDataModelingTest.Entities.Config
{
    /// <summary>
    /// Define un pipeline de validación ejecutable en contextos específicos
    /// </summary>
    public class ValidationPipeline
    {
        [Key]
        public int PipelineId { get; set; }

        [Required, MaxLength(50)]
        public string PipelineKey { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Contexto de ejecución: PRE_USER_REGISTRATION, PRE_STEP_SUBMISSION, POST_EVENT, ON_DEMAND
        /// </summary>
        [Required, MaxLength(50)]
        public string TriggerContext { get; set; } = string.Empty;

        /// <summary>
        /// Workflow específico (NULL = todos)
        /// </summary>
        public int? WorkflowId { get; set; }

        /// <summary>
        /// Step específico (NULL = todos)
        /// </summary>
        public int? StepId { get; set; }

        /// <summary>
        /// Detener pipeline al primer fallo
        /// </summary>
        public bool StopOnFirstFailure { get; set; } = true;

        /// <summary>
        /// Requiere que todos los steps pasen
        /// </summary>
        public bool RequireAllPass { get; set; } = true;

        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Orden de ejecución (menor = mayor prioridad)
        /// </summary>
        public int Priority { get; set; } = 100;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
