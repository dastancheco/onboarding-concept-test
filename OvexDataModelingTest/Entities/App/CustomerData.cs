using System.ComponentModel.DataAnnotations;

namespace OvexDataModelingTest.Entities.App
{
    // El "Golden Record"
    public class CustomerData
    {
        [Key]
        public Guid UserId { get; set; } // PK y FK a User
        public string Data { get; set; } // JSON: { "rfc": "...", "name": "..." }
        public DateTime UpdatedAt { get; set; }
    }
}
