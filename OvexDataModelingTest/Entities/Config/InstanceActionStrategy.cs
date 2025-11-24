using System.ComponentModel.DataAnnotations;

namespace OvexDataModelingTest.Entities.Config
{
    // 7. Estrategias (La Ejecución)
    public class InstanceActionStrategy
    {
        [Key, MaxLength(50)]
        public string ActionKey { get; set; } // "CALL_SAT_API"

        [Required, MaxLength(20)]
        public string ImplementationType { get; set; } // "EXTERNAL_API", "INTERNAL_CODE"

        [Required]
        public string ImplementationDetails { get; set; } // JSON Config
    }
}
