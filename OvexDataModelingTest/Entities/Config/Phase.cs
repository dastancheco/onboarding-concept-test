using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OvexDataModelingTest.Entities.Config
{
    public class Phase
    {
        [Key]
        public int PhaseId { get; set; }
        public int WorkflowId { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }

        public List<Step> Steps { get; set; } = new();
    }
}
