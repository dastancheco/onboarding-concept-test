using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OvexDataModelingTest.Entities.App
{
    /// <summary>
    /// Registro de auditoría para cambios de estado de prospectos.
    /// Permite rastrear todo el ciclo de vida del prospecto.
    /// </summary>
    public class ProspectStatusHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [Required]
        public Guid ProspectId { get; set; }

        [Required]
        [MaxLength(50)]
        public string OldStatus { get; set; }

        [Required]
        [MaxLength(50)]
        public string NewStatus { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        /// <summary>
        /// Usuario o sistema que realizó el cambio
        /// </summary>
        [MaxLength(100)]
        public string? ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// JSON con metadata adicional del cambio
        /// </summary>
        public string? Metadata { get; set; }

        // Navigation property
        [ForeignKey(nameof(ProspectId))]
        public Prospect? Prospect { get; set; }
    }
}
