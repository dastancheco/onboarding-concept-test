using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.App;
using OvexDataModelingTest.Entities.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onboarding.Api.Controllers
{
    /// <summary>
    /// Controlador para el Dashboard de Prospectos
    /// Provee endpoints para visualización, estadísticas y reportes
    /// </summary>
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IRepository<Prospect> _prospectRepo;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Workflow> _workflowRepo;
        private readonly IRepository<Phase> _phaseRepo;
        private readonly IRepository<Step> _stepRepo;
        private readonly IProspectDataService _prospectDataService;
        private readonly IProspectStatusService _prospectStatusService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IRepository<Prospect> prospectRepo,
            IRepository<User> userRepo,
            IRepository<Workflow> workflowRepo,
            IRepository<Phase> phaseRepo,
            IRepository<Step> stepRepo,
            IProspectDataService prospectDataService,
            IProspectStatusService prospectStatusService,
            ILogger<DashboardController> logger)
        {
            _prospectRepo = prospectRepo;
            _userRepo = userRepo;
            _workflowRepo = workflowRepo;
            _phaseRepo = phaseRepo;
            _stepRepo = stepRepo;
            _prospectDataService = prospectDataService;
            _prospectStatusService = prospectStatusService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene lista de prospectos con filtros y paginación
        /// </summary>
        /// <param name="workflowId">Filtrar por workflow (opcional)</param>
        /// <param name="status">Filtrar por estado (opcional)</param>
        /// <param name="page">Página actual (default: 1)</param>
        /// <param name="pageSize">Tamaño de página (default: 20)</param>
        [HttpGet("prospects")]
        public async Task<IActionResult> GetProspects(
            [FromQuery] int? workflowId = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            _logger.LogInformation(
                "Getting prospects - WorkflowId: {WorkflowId}, Status: {Status}, Page: {Page}",
                workflowId, status, page);

            try
            {
                // Obtener todos los prospectos
                var allProspects = await _prospectRepo.GetAllAsync();

                // Aplicar filtros
                var filteredProspects = allProspects.AsQueryable();

                if (workflowId.HasValue)
                {
                    filteredProspects = filteredProspects.Where(p => p.WorkflowId == workflowId.Value);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    filteredProspects = filteredProspects.Where(p => p.Status == status);
                }

                // Ordenar por fecha de creación (más recientes primero)
                var orderedProspects = filteredProspects.OrderByDescending(p => p.CreatedAt);

                // Contar total
                var totalCount = orderedProspects.Count();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Paginar
                var paginatedProspects = orderedProspects
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Obtener workflows, users y steps para enriquecer la respuesta
                var workflows = await _workflowRepo.GetAllAsync();
                var users = await _userRepo.GetAllAsync();
                var steps = await _stepRepo.GetAllAsync();

                // Construir respuesta enriquecida
                var enrichedProspects = paginatedProspects.Select(p =>
                {
                    var workflow = workflows.FirstOrDefault(w => w.WorkflowId == p.WorkflowId);
                    var user = users.FirstOrDefault(u => u.UserId == p.UserId);
                    var currentStep = p.CurrentStepId.HasValue
                        ? steps.FirstOrDefault(s => s.StepId == p.CurrentStepId.Value)
                        : null;

                    return new
                    {
                        prospectId = p.ProspectId,
                        userId = p.UserId,
                        userEmail = user?.Email ?? "Unknown",
                        workflowId = p.WorkflowId,
                        workflowName = workflow?.Name ?? "Unknown",
                        workflowType = workflow?.WorkflowType ?? "Unknown",
                        status = p.Status,
                        currentStepId = p.CurrentStepId,
                        currentStepName = currentStep?.Name ?? (p.CurrentStepId.HasValue ? "Unknown" : "Completed"),
                        createdAt = p.CreatedAt,
                        updatedAt = p.UpdatedAt,
                        daysActive = (DateTime.UtcNow - p.CreatedAt).Days
                    };
                }).ToList();

                return Ok(new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages,
                    prospects = enrichedProspects
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prospects list");
                return StatusCode(500, new { error = "Error al obtener lista de prospectos" });
            }
        }

        /// <summary>
        /// Obtiene detalle completo de un prospecto con progreso del workflow
        /// </summary>
        [HttpGet("prospects/{prospectId}")]
        public async Task<IActionResult> GetProspectDetail(Guid prospectId)
        {
            _logger.LogInformation("Getting prospect detail for ProspectId: {ProspectId}", prospectId);

            try
            {
                var prospect = await _prospectRepo.GetByIdAsync(prospectId);
                if (prospect == null)
                {
                    return NotFound(new { error = "Prospecto no encontrado" });
                }

                // Obtener entidades relacionadas
                var user = await _userRepo.GetByIdAsync(prospect.UserId);
                var workflow = await _workflowRepo.GetByIdAsync(prospect.WorkflowId);
                var prospectData = await _prospectDataService.GetProspectDataAsync(prospectId);
                var statusHistory = await _prospectStatusService.GetStatusHistoryAsync(prospectId);

                // Obtener fases y steps del workflow
                var phases = (await _phaseRepo.GetAllAsync())
                    .Where(p => p.WorkflowId == prospect.WorkflowId)
                    .OrderBy(p => p.Order)
                    .ToList();

                var allSteps = await _stepRepo.GetAllAsync();

                // Calcular progreso del workflow
                var workflowSteps = allSteps
                    .Where(s => phases.Select(ph => ph.PhaseId).Contains(s.PhaseId))
                    .OrderBy(s => s.Order)
                    .ToList();

                var totalSteps = workflowSteps.Count;
                var completedSteps = 0;

                if (prospect.CurrentStepId.HasValue)
                {
                    var currentStep = workflowSteps.FirstOrDefault(s => s.StepId == prospect.CurrentStepId.Value);
                    if (currentStep != null)
                    {
                        completedSteps = workflowSteps.TakeWhile(s => s.StepId != currentStep.StepId).Count();
                    }
                }
                else
                {
                    // Si no hay CurrentStepId, asumimos que está completado
                    completedSteps = totalSteps;
                }

                var progressPercentage = totalSteps > 0 ? (completedSteps * 100) / totalSteps : 0;

                // Construir detalle de fases con progreso
                var phasesDetail = phases.Select(phase =>
                {
                    var phaseSteps = allSteps
                        .Where(s => s.PhaseId == phase.PhaseId)
                        .OrderBy(s => s.Order)
                        .Select(s => new
                        {
                            stepId = s.StepId,
                            stepName = s.Name,
                            stepOrder = s.Order,
                            isCurrent = s.StepId == prospect.CurrentStepId,
                            isCompleted = prospect.CurrentStepId.HasValue
                                ? s.Order < workflowSteps.FirstOrDefault(ws => ws.StepId == prospect.CurrentStepId.Value)?.Order
                                : true
                        })
                        .ToList();

                    var phaseCompleted = phaseSteps.All(s => s.isCompleted);

                    return new
                    {
                        phaseId = phase.PhaseId,
                        phaseName = phase.Name,
                        phaseOrder = phase.Order,
                        isCompleted = phaseCompleted,
                        steps = phaseSteps
                    };
                }).ToList();

                return Ok(new
                {
                    prospectId = prospect.ProspectId,
                    user = new
                    {
                        userId = user?.UserId,
                        email = user?.Email,
                        createdAt = user?.CreatedAt
                    },
                    workflow = new
                    {
                        workflowId = workflow?.WorkflowId,
                        name = workflow?.Name,
                        type = workflow?.WorkflowType,
                        subTypeKey = workflow?.SubTypeKey
                    },
                    status = prospect.Status,
                    progress = new
                    {
                        currentStepId = prospect.CurrentStepId,
                        totalSteps,
                        completedSteps,
                        progressPercentage,
                        isCompleted = prospect.CurrentStepId == null
                    },
                    phases = phasesDetail,
                    data = prospectData,
                    timeline = statusHistory.Select(h => new
                    {
                        historyId = h.HistoryId,
                        oldStatus = h.OldStatus,
                        newStatus = h.NewStatus,
                        reason = h.Reason,
                        changedBy = h.ChangedBy,
                        changedAt = h.ChangedAt,
                        metadata = h.Metadata
                    }),
                    createdAt = prospect.CreatedAt,
                    updatedAt = prospect.UpdatedAt,
                    daysActive = (DateTime.UtcNow - prospect.CreatedAt).Days
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prospect detail for ProspectId: {ProspectId}", prospectId);
                return StatusCode(500, new { error = "Error al obtener detalle del prospecto" });
            }
        }

        /// <summary>
        /// Obtiene estadísticas generales del dashboard
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            _logger.LogInformation("Getting dashboard statistics");

            try
            {
                var allProspects = await _prospectRepo.GetAllAsync();
                var workflows = await _workflowRepo.GetAllAsync();

                // Estadísticas generales
                var totalProspects = allProspects.Count;
                var activeProspects = allProspects.Count(p => p.Status == "IN_PROGRESS" || p.Status == "STARTED");
                var completedProspects = allProspects.Count(p => p.CurrentStepId == null);
                var rejectedProspects = allProspects.Count(p => p.Status == "REJECTED");

                // Prospectos por workflow
                var prospectsByWorkflow = workflows.Select(w => new
                {
                    workflowId = w.WorkflowId,
                    workflowName = w.Name,
                    workflowType = w.WorkflowType,
                    count = allProspects.Count(p => p.WorkflowId == w.WorkflowId),
                    active = allProspects.Count(p => p.WorkflowId == w.WorkflowId && 
                                                     (p.Status == "IN_PROGRESS" || p.Status == "STARTED")),
                    completed = allProspects.Count(p => p.WorkflowId == w.WorkflowId && p.CurrentStepId == null)
                }).ToList();

                // Prospectos por estado
                var prospectsByStatus = allProspects
                    .GroupBy(p => p.Status)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count(),
                        percentage = totalProspects > 0 ? (g.Count() * 100.0 / totalProspects) : 0
                    })
                    .OrderByDescending(x => x.count)
                    .ToList();

                // Prospectos recientes (últimos 7 días)
                var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                var recentProspects = allProspects.Count(p => p.CreatedAt >= sevenDaysAgo);

                // Tiempo promedio de completitud (solo prospectos completados)
                var completedWithDates = allProspects
                    .Where(p => p.CurrentStepId == null && p.UpdatedAt.HasValue)
                    .ToList();

                var averageCompletionDays = completedWithDates.Any()
                    ? completedWithDates.Average(p => (p.UpdatedAt!.Value - p.CreatedAt).TotalDays)
                    : 0;

                return Ok(new
                {
                    summary = new
                    {
                        totalProspects,
                        activeProspects,
                        completedProspects,
                        rejectedProspects,
                        recentProspects,
                        averageCompletionDays = Math.Round(averageCompletionDays, 1)
                    },
                    byWorkflow = prospectsByWorkflow,
                    byStatus = prospectsByStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics");
                return StatusCode(500, new { error = "Error al obtener estadísticas" });
            }
        }

        /// <summary>
        /// Obtiene timeline de actividad reciente
        /// </summary>
        [HttpGet("timeline")]
        public async Task<IActionResult> GetTimeline([FromQuery] int days = 7, [FromQuery] int limit = 50)
        {
            _logger.LogInformation("Getting timeline for last {Days} days", days);

            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var allProspects = await _prospectRepo.GetAllAsync();
                var users = await _userRepo.GetAllAsync();
                var workflows = await _workflowRepo.GetAllAsync();

                // Obtener prospectos recientes
                var recentProspects = allProspects
                    .Where(p => p.CreatedAt >= cutoffDate || 
                               (p.UpdatedAt.HasValue && p.UpdatedAt.Value >= cutoffDate))
                    .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                    .Take(limit)
                    .ToList();

                // Construir timeline
                var timeline = recentProspects.Select(p =>
                {
                    var user = users.FirstOrDefault(u => u.UserId == p.UserId);
                    var workflow = workflows.FirstOrDefault(w => w.WorkflowId == p.WorkflowId);

                    var eventType = "updated";
                    var eventDate = p.UpdatedAt ?? p.CreatedAt;

                    if (p.CreatedAt >= cutoffDate && (!p.UpdatedAt.HasValue || p.CreatedAt >= p.UpdatedAt.Value.AddMinutes(-1)))
                    {
                        eventType = "created";
                        eventDate = p.CreatedAt;
                    }
                    else if (p.CurrentStepId == null)
                    {
                        eventType = "completed";
                    }

                    return new
                    {
                        prospectId = p.ProspectId,
                        eventType,
                        eventDate,
                        userEmail = user?.Email ?? "Unknown",
                        workflowName = workflow?.Name ?? "Unknown",
                        status = p.Status,
                        currentStepId = p.CurrentStepId
                    };
                }).ToList();

                return Ok(new
                {
                    days,
                    limit,
                    count = timeline.Count,
                    timeline
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting timeline");
                return StatusCode(500, new { error = "Error al obtener timeline" });
            }
        }

        /// <summary>
        /// Busca prospectos por email o prospectId
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchProspects([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Query parameter is required" });
            }

            _logger.LogInformation("Searching prospects with query: {Query}", query);

            try
            {
                var allProspects = await _prospectRepo.GetAllAsync();
                var users = await _userRepo.GetAllAsync();
                var workflows = await _workflowRepo.GetAllAsync();

                // Buscar por ProspectId (GUID)
                var results = new List<object>();

                if (Guid.TryParse(query, out var prospectGuid))
                {
                    var prospectById = allProspects.FirstOrDefault(p => p.ProspectId == prospectGuid);
                    if (prospectById != null)
                    {
                        var user = users.FirstOrDefault(u => u.UserId == prospectById.UserId);
                        var workflow = workflows.FirstOrDefault(w => w.WorkflowId == prospectById.WorkflowId);

                        results.Add(new
                        {
                            prospectId = prospectById.ProspectId,
                            userEmail = user?.Email ?? "Unknown",
                            workflowName = workflow?.Name ?? "Unknown",
                            status = prospectById.Status,
                            createdAt = prospectById.CreatedAt,
                            matchType = "ProspectId"
                        });
                    }
                }

                // Buscar por email (parcial)
                var prospectsByEmail = allProspects
                    .Where(p =>
                    {
                        var user = users.FirstOrDefault(u => u.UserId == p.UserId);
                        return user != null && user.Email.Contains(query, StringComparison.OrdinalIgnoreCase);
                    })
                    .ToList();

                results.AddRange(prospectsByEmail.Select(p =>
                {
                    var user = users.FirstOrDefault(u => u.UserId == p.UserId);
                    var workflow = workflows.FirstOrDefault(w => w.WorkflowId == p.WorkflowId);

                    return new
                    {
                        prospectId = p.ProspectId,
                        userEmail = user?.Email ?? "Unknown",
                        workflowName = workflow?.Name ?? "Unknown",
                        status = p.Status,
                        createdAt = p.CreatedAt,
                        matchType = "Email"
                    };
                }));

                return Ok(new
                {
                    query,
                    count = results.Count,
                    results = results.Take(20) // Limitar a 20 resultados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching prospects");
                return StatusCode(500, new { error = "Error al buscar prospectos" });
            }
        }
    }
}
