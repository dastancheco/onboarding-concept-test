using Onboarding.Core.Interfaces;
using Onboarding.Core.Events;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using Microsoft.Extensions.Logging;

namespace Onboarding.Core.Services
{
    /// <summary>
    /// Orquestador principal del sistema (REFACTORIZADO con Strategy Pattern).
    /// Aplica DIP: Dependency Inversion Principle - Depende de abstracciones (interfaces).
    /// Aplica SRP: Delega responsabilidades específicas a handlers y servicios especializados.
    /// Aplica Strategy Pattern: Usa handlers intercambiables para cada tipo de evento.
    /// </summary>
    public class OrchestratorService : IOrchestratorService
    {
        private readonly IEventHandlerFactory _handlerFactory;
        private readonly IRepository<Rule> _rulesRepo;
        private readonly IRepository<Prospect> _prospectRepo;
        private readonly IActionExecutor _actionExecutor;
        private readonly IRuleEngine _ruleEngine;
        private readonly ILogger<OrchestratorService> _logger;

        public OrchestratorService(
            IEventHandlerFactory handlerFactory,
            IRepository<Rule> rulesRepo,
            IRepository<Prospect> prospectRepo,
            IActionExecutor actionExecutor,
            IRuleEngine ruleEngine,
            ILogger<OrchestratorService> logger)
        {
            _handlerFactory = handlerFactory;
            _rulesRepo = rulesRepo;
            _prospectRepo = prospectRepo;
            _actionExecutor = actionExecutor;
            _ruleEngine = ruleEngine;
            _logger = logger;
        }

        public async Task ProcessEventAsync(string eventType, string payloadJson)
        {
            _logger.LogInformation("Processing event: {EventType}", eventType);

            // 1. Crear contexto del evento
            var context = await BuildEventContextAsync(eventType, payloadJson);

            // 2. Obtener handler específico para el evento
            var handler = _handlerFactory.GetHandler(eventType);

            if (handler != null)
            {
                try
                {
                    // 3. Ejecutar handler específico
                    await handler.HandleAsync(context);
                    _logger.LogInformation("Event handler executed successfully for {EventType}", eventType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing handler for event: {EventType}", eventType);
                    throw;
                }
            }
            else
            {
                _logger.LogWarning("No specific handler found for event: {EventType}. Skipping handler execution.", eventType);
            }

            // 4. Evaluar reglas de negocio (siempre se ejecutan si hay contexto válido)
            if (context.WorkflowId.HasValue)
            {
                await EvaluateWorkflowRules(context);
            }
            else
            {
                _logger.LogWarning("No WorkflowId in context. Skipping rule evaluation for event: {EventType}", eventType);
            }
        }

        /// <summary>
        /// Construye el contexto del evento extrayendo información del payload.
        /// </summary>
        private async Task<EventContext> BuildEventContextAsync(string eventType, string payloadJson)
        {
            var context = new EventContext
            {
                EventType = eventType,
                PayloadJson = payloadJson
            };

            // Para eventos que NO son de registro, extraer prospect_id del payload
            if (eventType != "UserRegistered")
            {
                try
                {
                    var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(payloadJson);
                    var prospectIdStr = jsonNode?["prospect_id"]?.ToString();

                    if (!string.IsNullOrEmpty(prospectIdStr) && Guid.TryParse(prospectIdStr, out var prospectId))
                    {
                        context.ProspectId = prospectId;

                        // Recuperar workflow del prospecto
                        var prospect = await _prospectRepo.GetByIdAsync(prospectId);
                        if (prospect != null)
                        {
                            context.WorkflowId = prospect.WorkflowId;
                            _logger.LogInformation(
                                "Context recovered: Prospect {ProspectId} belongs to Workflow {WorkflowId}",
                                prospectId, prospect.WorkflowId);
                        }
                        else
                        {
                            _logger.LogWarning("Prospect not found: {ProspectId}", prospectId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No valid prospect_id found in payload for event: {EventType}", eventType);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing payload to extract context for event: {EventType}", eventType);
                }
            }

            return context;
        }

        /// <summary>
        /// Evalúa y ejecuta reglas de negocio para el evento.
        /// Aplica OCP: Extensible agregando nuevas reglas en BD sin modificar código.
        /// </summary>
        private async Task EvaluateWorkflowRules(EventContext context)
        {
            if (!context.WorkflowId.HasValue)
            {
                _logger.LogWarning("Cannot evaluate rules: WorkflowId is null");
                return;
            }

            // Buscar reglas que coincidan con el Workflow y el Evento
            var rules = await _rulesRepo.FindAsync(r =>
                r.WorkflowId == context.WorkflowId.Value &&
                r.TriggerEvent == context.EventType);

            if (!rules.Any())
            {
                _logger.LogInformation(
                    "No rules configured for {EventType} in Workflow {WorkflowId}",
                    context.EventType, context.WorkflowId.Value);
                return;
            }

            _logger.LogInformation(
                "Evaluating {RuleCount} rules for event {EventType} in Workflow {WorkflowId}",
                rules.Count(), context.EventType, context.WorkflowId.Value);

            foreach (var rule in rules)
            {
                try
                {
                    // Evaluar la condición de la regla
                    if (_ruleEngine.Evaluate(rule.ConditionExpression, context.PayloadJson))
                    {
                        _logger.LogInformation(
                            "Rule {RuleId} matched. Triggering action: {ActionKey}",
                            rule.RuleId, rule.ActionKeyOnTrue);

                        // Ejecutar la estrategia (delega a ActionExecutor)
                        await _actionExecutor.ExecuteActionAsync(
                            rule.ActionKeyOnTrue,
                            context.ProspectId ?? Guid.Empty,
                            context.PayloadJson);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "Rule {RuleId} condition not met: {Condition}",
                            rule.RuleId, rule.ConditionExpression);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error evaluating or executing rule {RuleId} for event {EventType}",
                        rule.RuleId, context.EventType);
                    
                    // Decidir si continuar con otras reglas o fallar completamente
                    // Por ahora, loggeamos y continuamos
                }
            }
        }
    }
}
