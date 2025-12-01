using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OvexDataModelingTest.Entities.Config
{
    public class WorkflowRoutingRule
    {
        [Key]
        public int RoutingRuleId { get; set; }

        public int Priority { get; set; } // 10, 20...

        [Required]
        public string ConditionExpression { get; set; } // "input.client_type == 'CLIENT'"

        public int TargetWorkflowId { get; set; }
    }
}
