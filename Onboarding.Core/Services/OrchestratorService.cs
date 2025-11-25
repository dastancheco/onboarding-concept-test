using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace Onboarding.Core.Services
{
    /// <summary>
    /// Orquestador principal del sistema.
    /// Aplica DIP: Dependency Inversion Principle - Depende de abstracciones (interfaces).
    /// Aplica SRP: Delega responsabilidades específicas a servicios especializados.
    /// </summary>
    public class OrchestratorService : IOrchestratorService
    {
        private readonly IRepository<Rule> _rulesRepo;
        private readonly IRepository<Prospect> _prospectRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActionExecutor _actionExecutor;
        private readonly IRuleEngine _ruleEngine;
        private readonly IStepValidationService _validator;
        private readonly IProspectDataService _prospectDataService;
        private readonly ICustomerDataService _customerDataService;
        private readonly IUserManagementService _userManagementService;
        private readonly IWorkflowRoutingService _workflowRoutingService;
        private readonly ILogger<OrchestratorService> _logger;

        public OrchestratorService(
            IRepository<Rule> rulesRepo,
            IRepository<Prospect> prospectRepo,
            IUnitOfWork unitOfWork,
            IActionExecutor actionExecutor,
            IRuleEngine ruleEngine,
            IStepValidationService validator,
            IProspectDataService prospectDataService,
            ICustomerDataService customerDataService,
            IUserManagementService userManagementService,
            IWorkflowRoutingService workflowRoutingService,
            ILogger<OrchestratorService> logger)
        {
            _rulesRepo = rulesRepo;
            _prospectRepo = prospectRepo;
            _unitOfWork = unitOfWork;
            _actionExecutor = actionExecutor;
            _ruleEngine = ruleEngine;
            _validator = validator;
            _prospectDataService = prospectDataService;
            _customerDataService = customerDataService;
            _userManagementService = userManagementService;
            _workflowRoutingService = workflowRoutingService;
            _logger = logger;
        }

        public async Task ProcessEventAsync(string eventType, string payloadJson)
        {
            _logger.LogInformation("Processing event: {EventType}", eventType);

            // CASO 1: REGISTRO (No hay prospecto aún, usamos ruteo)
            if (eventType == "UserRegistered")
            {
                await HandleUserRegistration(payloadJson);
                return;
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
                    _logger.LogError("Event requires 'prospect_id' in payload to identify the flow");
                    return;
                }

                prospectId = Guid.Parse(prospectIdStr);

                // 2. Buscar el Prospecto en la BD para saber en qué Workflow está
                var prospect = await _prospectRepo.GetByIdAsync(prospectId);

                if (prospect == null)
                {
                    _logger.LogError("Prospect not found: {ProspectId}", prospectId);
                    return;
                }

                workflowId = prospect.WorkflowId;
                _logger.LogInformation("Context recovered: Prospect {ProspectId} belongs to Workflow {WorkflowId}", 
                    prospectId, workflowId);
                
                if (eventType == "StepDataSubmitted")
                {
                    await HandleStepDataSubmission(prospect, payloadJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover context");
                throw; // Rethrow para manejo superior;
            }
           
            // 3. Evaluar Reglas con el ID correcto
            await EvaluateWorkflowRules(eventType, payloadJson, workflowId, prospectId);
        }

        /// <summary>
        /// Maneja el registro de un nuevo usuario.
        /// Aplica SRP: Método con responsabilidad única y clara.
        /// </summary>
        private async Task HandleUserRegistration(string payloadJson)
        {
            _logger.LogInformation("Handling user registration");

            // 1. Gestión real de usuarios (delega a UserManagementService)
            string? email = _userManagementService.ExtractEmailFromPayload(payloadJson);
            
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogError("Cannot create user: email not found in payload");
                return;
            }

            User user = await _userManagementService.GetOrCreateUserAsync(email, payloadJson);
            _logger.LogInformation("User resolved: {UserId} ({Email})", user.UserId, user.Email);

            // 2. Determinar workflow (delega a WorkflowRoutingService)
            int selectedWorkflowId = await _workflowRoutingService.DetermineWorkflowAsync(payloadJson);

            if (selectedWorkflowId == 0)
            {
                _logger.LogError("No routing rule matched for this user");
                return;
            }

            // 3. Crear la Instancia (Prospect)
            var newProspect = new Prospect
            {
                ProspectId = Guid.NewGuid(),
                UserId = user.UserId,
                WorkflowId = selectedWorkflowId,
                Status = "STARTED",
                CreatedAt = DateTime.UtcNow
            };

            await _prospectRepo.AddAsync(newProspect);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Prospect created: {ProspectId}", newProspect.ProspectId);

            // 4. Inicializar ProspectData (delega a ProspectDataService)
            await _prospectDataService.UpdateProspectDataAsync(newProspect.ProspectId, payloadJson);
            _logger.LogInformation("Initial ProspectData created for {ProspectId}", newProspect.ProspectId);

            // 5. Evaluar Reglas de INICIO
            await EvaluateWorkflowRules("UserRegistered", payloadJson, selectedWorkflowId, newProspect.ProspectId);
        }

        /// <summary>
        /// Maneja la sumisión de datos de un paso.
        /// Aplica SRP: Lógica de validación y persistencia encapsulada.
        /// </summary>
        private async Task HandleStepDataSubmission(Prospect prospect, string payloadJson)
        {
            // 1. Verificar si el prospecto tiene un paso activo
            if (prospect.CurrentStepId == null)
            {
                _logger.LogError("Prospect has no active step assigned");
                throw new InvalidOperationException("El prospecto no tiene un paso activo asignado");
            }

            // 2. Validar los datos (delega a StepValidationService)
            var validationResult = await _validator.ValidateStepAsync(prospect.CurrentStepId.Value, payloadJson);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for Prospect {ProspectId}. Errors: {ErrorCount}", 
                    prospect.ProspectId, validationResult.Errors.Count);
                
                foreach (var error in validationResult.Errors) 
                {
                    _logger.LogWarning("  - [{ErrorCode}] {FieldKey}: {Message}", 
                        error.ErrorCode, error.FieldKey, error.Message);
                }

                throw new InvalidOperationException($"Validación fallida: {validationResult.Message}");
            }

            _logger.LogInformation("Structural validation successful");

            // 3. Persistir datos (delega a ProspectDataService)
            var updatedData = await _prospectDataService.UpdateProspectDataAsync(prospect.ProspectId, payloadJson);
            _logger.LogInformation("ProspectData updated successfully. Total data: {DataLength} chars", 
                updatedData.Length);
        }

        /// <summary>
        /// Evalúa y ejecuta reglas de negocio para un workflow específico.
        /// Aplica OCP: Extensible agregando nuevas reglas en BD sin modificar código.
        /// </summary>
        private async Task EvaluateWorkflowRules(string eventType, string payloadJson, int? workflowIdOverride = null, Guid? prospectIdOverride = null)
        {
            int currentWorkflowId = workflowIdOverride ?? 0;
            Guid currentProspectId = prospectIdOverride ?? Guid.Empty;

            // Buscar reglas que coincidan con el Workflow y el Evento
            var rules = await _rulesRepo.FindAsync(r => r.WorkflowId == currentWorkflowId && r.TriggerEvent == eventType);

            if (!rules.Any())
            {
                _logger.LogInformation("No rules configured for {EventType} in Workflow {WorkflowId}", 
                    eventType, currentWorkflowId);
                return;
            }

            foreach (var rule in rules)
            {
                // Evaluar la condición de la regla
                if (_ruleEngine.Evaluate(rule.ConditionExpression, payloadJson))
                {
                    _logger.LogInformation("Rule {RuleId} matched. Triggering action: {ActionKey}", 
                        rule.RuleId, rule.ActionKeyOnTrue);

                    // Ejecutar la estrategia (delega a ActionExecutor)
                    await _actionExecutor.ExecuteActionAsync(rule.ActionKeyOnTrue, currentProspectId, payloadJson);
                }
            }
        }
    }
}
