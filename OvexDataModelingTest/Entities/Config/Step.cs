using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OvexDataModelingTest.Entities.Config
{
    public class Step
    {
        [Key]
        public int StepId { get; set; }
        public int PhaseId { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
    }
}
