using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OvexDataModelingTest.Entities.Config
{
    /// <summary>
    /// Define un campo configurable que puede ser usado en steps.
    /// Soporta campos simples y complejos (arrays de objetos anidados).
    /// </summary>
    public class FieldDefinition
    {
        [Key]
        public int FieldId { get; set; }

        [Required, MaxLength(50)]
        public string FieldKey { get; set; } // "monthly_income", "accionistas"

        [Required, MaxLength(20)]
        public string DataType { get; set; } // "TEXT", "NUMBER", "OBJECT_ARRAY", etc.

        [Required, MaxLength(20)]
        public string Scope { get; set; }    // "USER", "APPLICATION"

        /// <summary>
        /// Configuración básica del campo en formato JSON
        /// Ejemplo: {"is_required": true, "min_length": 3}
        /// </summary>
        public string? Config { get; set; }

        /// <summary>
        /// Esquema anidado para tipos complejos (OBJECT_ARRAY)
        /// Define la estructura de los objetos dentro del array.
        /// Ejemplo para accionistas:
        /// {
        ///   "properties": [
        ///     {"key": "nombre", "type": "TEXT", "required": true},
        ///     {"key": "participacion", "type": "NUMBER", "required": true, "min_value": 0, "max_value": 100}
        ///   ],
        ///   "min_items": 1,
        ///   "max_items": 10
        /// }
        /// </summary>
        public string? NestedSchema { get; set; }
    }
}
