using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onboarding.Core.Domain;
using Onboarding.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Onboarding.Api.Controllers
{
    /// <summary>
    /// Controlador para consultar información de prospectos.
    /// Aplica OCP: Open/Closed Principle - Extensible sin modificar PubSubController.
    /// </summary>
    [ApiController]
    [Route("api/prospects")]
    public class ProspectController : ControllerBase
    {
        private readonly IProspectDataService _prospectDataService;
        private readonly IProspectStatusService _prospectStatusService;
        private readonly IRepository<OvexDataModelingTest.Entities.App.Prospect> _prospectRepo;
        private readonly ILogger<ProspectController> _logger;

        public ProspectController(
            IProspectDataService prospectDataService,
            IProspectStatusService prospectStatusService,
            IRepository<OvexDataModelingTest.Entities.App.Prospect> prospectRepo,
            ILogger<ProspectController> logger)
        {
            _prospectDataService = prospectDataService;
            _prospectStatusService = prospectStatusService;
            _prospectRepo = prospectRepo;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene los datos completos de un prospecto
        /// </summary>
        [HttpGet("{prospectId}")]
        public async Task<IActionResult> GetProspect(Guid prospectId)
        {
            _logger.LogInformation("Getting prospect data for ProspectId: {ProspectId}", prospectId);

            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                _logger.LogWarning("Prospect not found: {ProspectId}", prospectId);
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            var data = await _prospectDataService.GetProspectDataAsync(prospectId);

            return Ok(new
            {
                prospectId = prospect.ProspectId,
                userId = prospect.UserId,
                workflowId = prospect.WorkflowId,
                status = prospect.Status,
                currentStepId = prospect.CurrentStepId,
                createdAt = prospect.CreatedAt,
                updatedAt = prospect.UpdatedAt,
                data = data
            });
        }

        /// <summary>
        /// Obtiene solo los datos JSON de un prospecto
        /// </summary>
        [HttpGet("{prospectId}/data")]
        public async Task<IActionResult> GetProspectData(Guid prospectId)
        {
            _logger.LogInformation("Getting prospect data for ProspectId: {ProspectId}", prospectId);

            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            var data = await _prospectDataService.GetProspectDataAsync(prospectId);

            return Ok(new
            {
                prospectId = prospectId,
                data = data,
                retrievedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Obtiene el historial de cambios de estado de un prospecto
        /// </summary>
        [HttpGet("{prospectId}/status-history")]
        public async Task<IActionResult> GetStatusHistory(Guid prospectId)
        {
            _logger.LogInformation("Getting status history for ProspectId: {ProspectId}", prospectId);

            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            var history = await _prospectStatusService.GetStatusHistoryAsync(prospectId);

            return Ok(new
            {
                prospectId = prospectId,
                currentStatus = prospect.Status,
                historyCount = history.Count,
                history = history.Select(h => new
                {
                    historyId = h.HistoryId,
                    oldStatus = h.OldStatus,
                    newStatus = h.NewStatus,
                    reason = h.Reason,
                    changedBy = h.ChangedBy,
                    changedAt = h.ChangedAt,
                    metadata = h.Metadata
                })
            });
        }

        /// <summary>
        /// Obtiene las transiciones de estado disponibles para un prospecto
        /// </summary>
        [HttpGet("{prospectId}/available-transitions")]
        public async Task<IActionResult> GetAvailableTransitions(Guid prospectId)
        {
            _logger.LogInformation("Getting available transitions for ProspectId: {ProspectId}", prospectId);

            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                return NotFound(new { error = "Prospecto no encontrado" });
            }

            var currentStatus = await _prospectStatusService.GetCurrentStatusAsync(prospectId);
            var availableTransitions = await _prospectStatusService.GetAvailableTransitionsAsync(prospectId);
            var isTerminal = ProspectStateMachine.IsTerminalState(currentStatus ?? "");

            return Ok(new
            {
                prospectId = prospectId,
                currentStatus = currentStatus,
                isTerminalState = isTerminal,
                availableTransitions = availableTransitions
            });
        }

        /// <summary>
        /// Actualiza manualmente el estado de un prospecto
        /// </summary>
        [HttpPut("{prospectId}/status")]
        public async Task<IActionResult> UpdateStatus(
            Guid prospectId,
            [FromBody] UpdateStatusRequest request)
        {
            _logger.LogInformation(
                "Updating status for ProspectId: {ProspectId} to {NewStatus}", 
                prospectId, request.NewStatus);

            try
            {
                var success = await _prospectStatusService.UpdateStatusAsync(
                    prospectId,
                    request.NewStatus,
                    request.Reason,
                    request.ChangedBy ?? "API_USER",
                    request.Metadata);

                if (success)
                {
                    return Ok(new
                    {
                        message = "Estado actualizado exitosamente",
                        prospectId = prospectId,
                        newStatus = request.NewStatus
                    });
                }

                return BadRequest(new { error = "No se pudo actualizar el estado" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid status transition for ProspectId: {ProspectId}", prospectId);
                return BadRequest(new
                {
                    error = "Transición de estado inválida",
                    detail = ex.Message
                });
            }
        }
    }

    /// <summary>
    /// DTO para actualizar el estado de un prospecto
    /// </summary>
    public class UpdateStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string? ChangedBy { get; set; }
        public string? Metadata { get; set; }
    }
}
