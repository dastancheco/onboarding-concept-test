using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Data;
using OvexDataModelingTest.Entities.Config;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onboarding.Api.Controllers
{
    /// <summary>
    /// Controlador para consultar configuración de workflows, fases, steps y campos.
    /// Permite al frontend construir interfaces dinámicas basadas en configuración.
    /// </summary>
    [ApiController]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        private readonly IRepository<Workflow> _workflowRepo;
        private readonly IRepository<Phase> _phaseRepo;
        private readonly IRepository<Step> _stepRepo;
        private readonly IRepository<Step_Field> _stepFieldRepo;
        private readonly IRepository<FieldDefinition> _fieldDefRepo;
        private readonly IWorkflowRoutingService _routingService;
        private readonly OnboardingDbContext _dbContext;
        private readonly ILogger<ConfigController> _logger;

        public ConfigController(
            IRepository<Workflow> workflowRepo,
            IRepository<Phase> phaseRepo,
            IRepository<Step> stepRepo,
            IRepository<Step_Field> stepFieldRepo,
            IRepository<FieldDefinition> fieldDefRepo,
            IWorkflowRoutingService routingService,
            OnboardingDbContext dbContext,
            ILogger<ConfigController> logger)
        {
            _workflowRepo = workflowRepo;
            _phaseRepo = phaseRepo;
            _stepRepo = stepRepo;
            _stepFieldRepo = stepFieldRepo;
            _fieldDefRepo = fieldDefRepo;
            _routingService = routingService;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los workflows disponibles.
        /// </summary>
        [HttpGet("workflows")]
        public async Task<IActionResult> GetWorkflows()
        {
            _logger.LogInformation("Getting all workflows");

            var workflows = await _workflowRepo.GetAllAsync();

            var result = workflows
                .Where(w => w.IsActive)
                .Select(w => new
                {
                    workflowId = w.WorkflowId,
                    name = w.Name,
                    type = w.WorkflowType,
                    subTypeKey = w.SubTypeKey,
                    isActive = w.IsActive
                })
                .ToList();

            return Ok(result);
        }

        /// <summary>
        /// Determina el workflow apropiado basándose en datos de usuario.
        /// No requiere conocer el workflow ID de antemano.
        /// </summary>
        [HttpPost("workflows/determine")]
        public async Task<IActionResult> DetermineWorkflow([FromBody] JsonElement payload)
        {
            _logger.LogInformation("Determining workflow based on user data");

            try
            {
                // Convertir JsonElement a string JSON
                var payloadJson = JsonSerializer.Serialize(payload);

                // Usar el WorkflowRoutingService para determinar el workflow
                var workflowId = await _routingService.DetermineWorkflowAsync(payloadJson);

                if (workflowId == 0)
                {
                    return BadRequest(new
                    {
                        error = "No se pudo determinar un workflow válido",
                        message = "Los datos proporcionados no coinciden con ninguna regla de ruteo configurada"
                    });
                }

                // Obtener información básica del workflow
                var workflow = await _workflowRepo.GetByIdAsync(workflowId);
                if (workflow == null)
                {
                    return NotFound(new { error = "Workflow determinado no existe" });
                }

                _logger.LogInformation("Workflow determined: {WorkflowId} ({Name})", workflowId, workflow.Name);

                return Ok(new
                {
                    workflowId = workflow.WorkflowId,
                    name = workflow.Name,
                    type = workflow.WorkflowType,
                    subTypeKey = workflow.SubTypeKey
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining workflow");
                return StatusCode(500, new
                {
                    error = "Error al determinar workflow",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene la configuración completa de un workflow específico.
        /// Incluye fases, steps y campos con sus validaciones.
        /// </summary>
        [HttpGet("workflows/{workflowId}")]
        public async Task<IActionResult> GetWorkflowConfiguration(int workflowId)
        {
            _logger.LogInformation("Getting workflow configuration for WorkflowId: {WorkflowId}", workflowId);

            var workflow = await _workflowRepo.GetByIdAsync(workflowId);
            if (workflow == null)
            {
                return NotFound(new { error = "Workflow no encontrado" });
            }

            // Obtener fases del workflow
            var phases = (await _phaseRepo.GetAllAsync())
                .Where(p => p.WorkflowId == workflowId)
                .OrderBy(p => p.Order)
                .ToList();

            // Obtener todos los steps
            var allSteps = await _stepRepo.GetAllAsync();

            // Obtener todos los step_fields
            var allStepFields = await _stepFieldRepo.GetAllAsync();

            // Obtener todas las definiciones de campos
            var allFieldDefs = await _fieldDefRepo.GetAllAsync();

            var configuration = new
            {
                workflowId = workflow.WorkflowId,
                name = workflow.Name,
                type = workflow.WorkflowType,
                subTypeKey = workflow.SubTypeKey,
                phases = phases.Select(phase => new
                {
                    phaseId = phase.PhaseId,
                    name = phase.Name,
                    order = phase.Order,
                    steps = allSteps
                        .Where(s => s.PhaseId == phase.PhaseId)
                        .OrderBy(s => s.Order)
                        .Select(step => new
                        {
                            stepId = step.StepId,
                            name = step.Name,
                            order = step.Order,
                            fields = allStepFields
                                .Where(sf => sf.StepId == step.StepId)
                                .Select(sf =>
                                {
                                    var fieldDef = allFieldDefs.FirstOrDefault(fd => fd.FieldId == sf.FieldId);
                                    if (fieldDef == null) return null;

                                    // Parsear config base y override
                                    var baseConfig = string.IsNullOrEmpty(fieldDef.Config)
                                        ? new { }
                                        : JsonSerializer.Deserialize<object>(fieldDef.Config);

                                    var overrideConfig = string.IsNullOrEmpty(sf.ConfigOverride)
                                        ? null
                                        : JsonSerializer.Deserialize<object>(sf.ConfigOverride);

                                    return new
                                    {
                                        fieldId = fieldDef.FieldId,
                                        fieldKey = fieldDef.FieldKey,
                                        dataType = fieldDef.DataType,
                                        scope = fieldDef.Scope,
                                        baseConfig = baseConfig,
                                        overrideConfig = overrideConfig
                                    };
                                })
                                .Where(f => f != null)
                                .ToList()
                        })
                        .ToList()
                })
                .ToList()
            };

            return Ok(configuration);
        }

        /// <summary>
        /// Obtiene los detalles de un campo específico.
        /// </summary>
        [HttpGet("fields/{fieldId}")]
        public async Task<IActionResult> GetFieldDefinition(int fieldId)
        {
            _logger.LogInformation("Getting field definition for FieldId: {FieldId}", fieldId);

            var field = await _fieldDefRepo.GetByIdAsync(fieldId);
            if (field == null)
            {
                return NotFound(new { error = "Campo no encontrado" });
            }

            var config = string.IsNullOrEmpty(field.Config)
                ? new { }
                : JsonSerializer.Deserialize<object>(field.Config);

            return Ok(new
            {
                fieldId = field.FieldId,
                fieldKey = field.FieldKey,
                dataType = field.DataType,
                scope = field.Scope,
                config = config
            });
        }
    }
}
