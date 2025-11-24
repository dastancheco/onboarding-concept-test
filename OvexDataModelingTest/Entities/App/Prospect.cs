using System.ComponentModel.DataAnnotations;

namespace OvexDataModelingTest.Entities.App
{
    public class Prospect
    {
        [Key]
        public Guid ProspectId { get; set; }
        public Guid UserId { get; set; }
        public int WorkflowId { get; set; }
        public string Status { get; set; } // "IN_PROGRESS", "APPROVED"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CurrentStepId { get; set; }
    }
}
