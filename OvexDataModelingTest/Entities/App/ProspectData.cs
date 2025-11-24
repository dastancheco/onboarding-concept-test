using System.ComponentModel.DataAnnotations;

namespace OvexDataModelingTest.Entities.App
{
    public class ProspectData
    {
        [Key]
        public Guid ProspectId { get; set; } // PK y FK a Prospect
        public string Data { get; set; } // JSON con TODO lo capturado en el flujo
    }
}
