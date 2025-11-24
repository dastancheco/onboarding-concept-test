using System.ComponentModel.DataAnnotations;

namespace OvexDataModelingTest.Entities.Config
{
    public class Step_Field
    {
        [Key]
        public int Id { get; set; } // PK simple para EF Core
        public int StepId { get; set; }
        public int FieldId { get; set; }
        public string? ConfigOverride { get; set; }
    }
}
