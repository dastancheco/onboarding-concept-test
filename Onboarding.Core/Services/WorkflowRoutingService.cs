using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.Config;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Onboarding.Core.Services
{
    /// <summary>
    /// Implementación del servicio de ruteo de workflows.
    /// Aplica SRP: Responsable únicamente de determinar el workflow correcto.
    /// </summary>
    public class WorkflowRoutingService : IWorkflowRoutingService
    {
        private readonly IRepository<WorkflowRoutingRule> _routingRepo;
        private readonly IRuleEngine _ruleEngine;
        private readonly ILogger<WorkflowRoutingService> _logger;

        public WorkflowRoutingService(
            IRepository<WorkflowRoutingRule> routingRepo,
            IRuleEngine ruleEngine,
            ILogger<WorkflowRoutingService> logger)
        {
            _routingRepo = routingRepo;
            _ruleEngine = ruleEngine;
            _logger = logger;
        }

        public async Task<int> DetermineWorkflowAsync(string payloadJson)
        {
            _logger.LogInformation("Determining workflow for payload");

            // Ordenar por Priority DESCENDENTE: prioridades más altas se evalúan primero
            var routingRules = (await _routingRepo.GetAllAsync())
                .OrderByDescending(r => r.Priority);

            foreach (var rule in routingRules)
            {
                _logger.LogDebug(
                    "Evaluating routing rule {RuleId} (Priority: {Priority}): {Condition}",
                    rule.RoutingRuleId, rule.Priority, rule.ConditionExpression);

                if (_ruleEngine.Evaluate(rule.ConditionExpression, payloadJson))
                {
                    _logger.LogInformation(
                        "Routing rule matched: RuleId={RuleId}, TargetWorkflowId={WorkflowId}",
                        rule.RoutingRuleId, rule.TargetWorkflowId);

                    return rule.TargetWorkflowId;
                }
            }

            _logger.LogWarning("No routing rule matched for the given payload");
            return 0; // No match
        }

        public async Task<bool> HasMatchingRuleAsync(string payloadJson)
        {
            var workflowId = await DetermineWorkflowAsync(payloadJson);
            return workflowId != 0;
        }
    }
}
