using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;

namespace Onboarding.Core.Services
{
    public class StepValidationService
    {
        private readonly IRepository<Step_Field> _stepFieldsRepo;
        private readonly IRepository<FieldDefinition> _fieldsRepo;

        public StepValidationService(
            IRepository<Step_Field> stepFieldsRepo,
            IRepository<FieldDefinition> fieldsRepo)
        {
            _stepFieldsRepo = stepFieldsRepo;
            _fieldsRepo = fieldsRepo;
        }

        public async Task<(bool IsValid, List<string> Errors)> ValidateStepAsync(int stepId, string payloadJson)
        {
            var errors = new List<string>();
            var jsonNode = JsonNode.Parse(payloadJson);

            // 1. Obtener qué campos se requieren en este paso
            var stepFieldLinks = await _stepFieldsRepo.FindAsync(sf => sf.StepId == stepId);
            var fieldIds = stepFieldLinks.Select(sf => sf.FieldId).ToList();

            var allFields = await _fieldsRepo.GetAllAsync(); 
            var requiredFields = allFields.Where(f => fieldIds.Contains(f.FieldId)).ToList();

            // 2. Validar cada campo
            foreach (var fieldDef in requiredFields)
            {
                var value = jsonNode?[fieldDef.FieldKey]?.ToString();

                // Validación A: Requerido
                // (Asumimos que todos los definidos en el paso son requeridos por defecto para este ejemplo)
                if (string.IsNullOrWhiteSpace(value))
                {
                    errors.Add($"El campo '{fieldDef.FieldKey}' es obligatorio en este paso.");
                    continue;
                }

                // Validación B: Tipo de Dato (Ejemplo simple)
                if (fieldDef.DataType == "NUMBER" && !double.TryParse(value, out _))
                {
                    errors.Add($"El campo '{fieldDef.FieldKey}' debe ser numérico.");
                }
            }

            return (errors.Count == 0, errors);
        }
    }
}
