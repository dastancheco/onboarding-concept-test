using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OvexDataModelingTest.Entities.Config
{
    public class FieldDefinition
    {
        [Key]
        public int FieldId { get; set; }

        [Required, MaxLength(50)]
        public string FieldKey { get; set; } // "monthly_income"

        [Required, MaxLength(20)]
        public string DataType { get; set; } // "TEXT", "NUMBER", "JSON"

        [Required, MaxLength(20)]
        public string Scope { get; set; }    // "USER", "APPLICATION"

        public string? Config { get; set; }  // JSON {"is_required": true}
    }
}
