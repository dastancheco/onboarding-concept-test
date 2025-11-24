using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System.ComponentModel.DataAnnotations;

namespace Onboarding.Core.Services
{
    public class OrchestratorService : IOrchestratorService
    {
        private readonly IRepository<WorkflowRoutingRule> _routingRepo;
        private readonly IRepository<Rule> _rulesRepo;
        private readonly IRepository<Prospect> _prospectRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActionExecutor _actionExecutor;
        private readonly RuleEngine _ruleEngine;
        private readonly StepValidationService _validator;

        public OrchestratorService(
            IRepository<WorkflowRoutingRule> routingRepo,
            IRepository<Rule> rulesRepo,
            IRepository<Prospect> prospectRepo,
            IUnitOfWork unitOfWork,
            IActionExecutor actionExecutor,
            RuleEngine ruleEngine,
            StepValidationService validator)
        {
            _routingRepo = routingRepo;
            _rulesRepo = rulesRepo;
            _prospectRepo = prospectRepo;
            _unitOfWork = unitOfWork;
            _actionExecutor = actionExecutor;
            _ruleEngine = ruleEngine;
            _validator = validator;
        }

        public async Task ProcessEventAsync(string eventType, string payloadJson)
        {
            Console.WriteLine($"\n[CORE] Procesando Evento: {eventType}");

            // CASO 1: REGISTRO (No hay prospecto aún, usamos ruteo)
            if (eventType == "UserRegistered")
            {
                await HandleUserRegistration(payloadJson);
                return; // Terminamos aquí
            }

            // CASO 2: SEGUIMIENTO (Ya existe prospecto, buscamos su Workflow ID)
            int workflowId = 0;
            Guid prospectId = Guid.Empty;

            try
            {
                // 1. Extraer prospect_id del JSON
                var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(payloadJson);
                var prospectIdStr = jsonNode?["prospect_id"]?.ToString();

                if (string.IsNullOrEmpty(prospectIdStr))
                {
                    Console.WriteLine("[CORE ERROR] El evento requiere 'prospect_id' en el payload para identificar el flujo.");
                    return;
                }

                prospectId = Guid.Parse(prospectIdStr);

                // 2. Buscar el Prospecto en la BD para saber en qué Workflow está
                var prospect = await _prospectRepo.GetByIdAsync(prospectId);

                if (prospect == null)
                {
                    Console.WriteLine($"[CORE ERROR] No se encontró el prospecto {prospectId}");
                    return;
                }

                workflowId = prospect.WorkflowId;
                Console.WriteLine($"[CORE] Contexto Recuperado: Prospecto {prospectId} pertenece al Workflow {workflowId}");
                if (eventType == "StepDataSubmitted")
                {
                    // 1. Verificar si el prospecto tiene un paso activo
                    if (prospect.CurrentStepId == null)
                    {
                        Console.WriteLine("[CORE ERROR] El prospecto no tiene un paso activo asignado.");
                        return;
                    }

                    // 2. Validar los datos contra la definición del paso
                    var validationResult = await _validator.ValidateStepAsync(prospect.CurrentStepId.Value, payloadJson);

                    if (!validationResult.IsValid)
                    {
                        Console.WriteLine("[CORE] Validación Fallida:");
                        foreach (var error in validationResult.Errors) Console.WriteLine($"   - {error}");

                        // Aquí podrías emitir un evento "StepValidationFailed" para avisar al frontend
                        return; // DETENEMOS EL PROCESO. No se evalúan reglas.
                    }

                    Console.WriteLine("[CORE] Validación Estructural Exitosa.");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CORE ERROR] Falló al recuperar contexto: {ex.Message}");
                return;
            }
           
            // 3. Evaluar Reglas con el ID correcto
            await EvaluateWorkflowRules(eventType, payloadJson, workflowId, prospectId);
        }

        private async Task HandleUserRegistration(string payloadJson)
        {
            // PASO A: Consultar Reglas de Ruteo
            var routingRules = (await _routingRepo.GetAllAsync()).OrderBy(r => r.Priority);
            int selectedWorkflowId = 0;

            foreach (var rule in routingRules)
            {
                if (_ruleEngine.Evaluate(rule.ConditionExpression, payloadJson))
                {
                    selectedWorkflowId = rule.TargetWorkflowId;
                    Console.WriteLine($"[CORE] Ruteo Exitoso: Asignado Workflow ID {selectedWorkflowId} (Regla {rule.RoutingRuleId})");
                    break;
                }
            }

            if (selectedWorkflowId == 0)
            {
                Console.WriteLine("[CORE ERROR] No se encontró ruta para este usuario.");
                return;
            }

            // PASO B: Crear la Instancia (Prospect)
            var newProspect = new Prospect
            {
                ProspectId = Guid.NewGuid(),
                UserId = Guid.NewGuid(), // En prod vendría del payload
                WorkflowId = selectedWorkflowId,
                Status = "STARTED",
                CreatedAt = DateTime.UtcNow
            };

            await _prospectRepo.AddAsync(newProspect);
            await _unitOfWork.SaveChangesAsync();

            Console.WriteLine($"[CORE] Prospecto Creado: {newProspect.ProspectId}");

            // PASO C: Evaluar Reglas de INICIO para este Workflow recién asignado
            // (Por ejemplo: Disparar 'CALL_LEGACY_API' inmediatamente)
            await EvaluateWorkflowRules("UserRegistered", payloadJson, selectedWorkflowId, newProspect.ProspectId);
        }

        private async Task EvaluateWorkflowRules(string eventType, string payloadJson, int? workflowIdOverride = null, Guid? prospectIdOverride = null)
        {
            // Si no tenemos el ID del workflow (caso normal), tendríamos que buscarlo en BD usando el ProspectId del JSON.
            // Para simplificar este test, usamos el override si venimos de HandleUserRegistration.
            int currentWorkflowId = workflowIdOverride ?? 0;
            Guid currentProspectId = prospectIdOverride ?? Guid.Empty;

            // Buscar reglas que coincidan con el Workflow y el Evento
            var rules = await _rulesRepo.FindAsync(r => r.WorkflowId == currentWorkflowId && r.TriggerEvent == eventType);

            if (!rules.Any())
            {
                Console.WriteLine($"[CORE] No hay reglas configuradas para {eventType} en Workflow {currentWorkflowId}.");
                return;
            }

            foreach (var rule in rules)
            {
                // Evaluar la condición de la regla (Ej. "data.income > 10000" o "true")
                if (_ruleEngine.Evaluate(rule.ConditionExpression, payloadJson))
                {
                    Console.WriteLine($"[CORE] Regla {rule.RuleId} Cumplida. Disparando Acción: {rule.ActionKeyOnTrue}");

                    // EJECUTAR LA ESTRATEGIA
                    // Delegamos a la capa de Infraestructura
                    await _actionExecutor.ExecuteActionAsync(rule.ActionKeyOnTrue, currentProspectId, payloadJson);
                }
            }
        }
    }
}
