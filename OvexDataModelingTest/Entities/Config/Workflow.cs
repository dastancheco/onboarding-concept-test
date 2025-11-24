using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Numerics;
using System.Text;

namespace OvexDataModelingTest.Entities.Config
{
    public class Workflow
    {
        [Key]
        public int WorkflowId { get; set; }

        [Required, MaxLength(20)]
        public string WorkflowType { get; set; } // "PROSPECT", "PRODUCT"

        [Required, MaxLength(50)]
        public string SubTypeKey { get; set; }   // "OVEX_PF_CLIENT", "OVEX_FLOTILLA"

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public int? ProductId { get; set; } // Nullable
        public bool IsActive { get; set; } = true;

        // Relaciones
        public List<Phase> Phases { get; set; } = new();
        public List<Rule> Rules { get; set; } = new();
    }
}
