using System.ComponentModel.DataAnnotations;

namespace OvexDataModelingTest.Entities.Config
{
    public class Rule
    {
        [Key]
        public int RuleId { get; set; }
        public int WorkflowId { get; set; }

        [Required, MaxLength(100)]
        public string TriggerEvent { get; set; } // "StepDataValidated"

        [Required]
        public string ConditionExpression { get; set; } // "data.income > 10000"

        [Required, MaxLength(50)]
        public string ActionKeyOnTrue { get; set; }
    }
}
