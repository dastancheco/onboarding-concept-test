using Microsoft.Extensions.Logging;
using Onboarding.Core.Domain;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Onboarding.Core.Services
{
    /// <summary>
    /// Implementación del servicio de gestión de estados de prospectos.
    /// Aplica SRP: Responsable únicamente de transiciones de estado y auditoría.
    /// </summary>
    public class ProspectStatusService : IProspectStatusService
    {
        private readonly IRepository<Prospect> _prospectRepo;
        private readonly IRepository<ProspectStatusHistory> _historyRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ProspectStatusService> _logger;

        public ProspectStatusService(
            IRepository<Prospect> prospectRepo,
            IRepository<ProspectStatusHistory> historyRepo,
            IUnitOfWork unitOfWork,
            ILogger<ProspectStatusService> logger)
        {
            _prospectRepo = prospectRepo;
            _historyRepo = historyRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<bool> UpdateStatusAsync(
            Guid prospectId, 
            string newStatus, 
            string? reason = null, 
            string? changedBy = "SYSTEM",
            string? metadata = null)
        {
            _logger.LogInformation(
                "Updating status for Prospect {ProspectId} to {NewStatus}. Reason: {Reason}", 
                prospectId, newStatus, reason ?? "N/A");

            // 1. Obtener el prospecto
            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            if (prospect == null)
            {
                _logger.LogError("Prospect not found: {ProspectId}", prospectId);
                throw new InvalidOperationException($"Prospecto {prospectId} no encontrado");
            }

            var currentStatus = prospect.Status;

            // 2. Si el estado es el mismo, no hacer nada
            if (currentStatus == newStatus)
            {
                _logger.LogInformation("Status unchanged for Prospect {ProspectId}: {Status}", prospectId, currentStatus);
                return true;
            }

            // 3. Validar la transición con el State Machine
            ProspectStateMachine.ValidateTransition(prospectId, currentStatus, newStatus);

            // 4. Registrar en el historial ANTES de actualizar
            var historyEntry = new ProspectStatusHistory
            {
                ProspectId = prospectId,
                OldStatus = currentStatus,
                NewStatus = newStatus,
                Reason = reason,
                ChangedBy = changedBy,
                ChangedAt = DateTime.UtcNow,
                Metadata = metadata
            };

            await _historyRepo.AddAsync(historyEntry);

            // 5. Actualizar el estado del prospecto
            prospect.Status = newStatus;
            prospect.UpdatedAt = DateTime.UtcNow;

            await _prospectRepo.UpdateAsync(prospect);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Status updated successfully for Prospect {ProspectId}: {OldStatus} -> {NewStatus}",
                prospectId, currentStatus, newStatus);

            return true;
        }

        public async Task<List<ProspectStatusHistory>> GetStatusHistoryAsync(Guid prospectId)
        {
            var history = await _historyRepo.FindAsync(h => h.ProspectId == prospectId);
            return history.OrderBy(h => h.ChangedAt).ToList();
        }

        public async Task<string?> GetCurrentStatusAsync(Guid prospectId)
        {
            var prospect = await _prospectRepo.GetByIdAsync(prospectId);
            return prospect?.Status;
        }

        public async Task<bool> CanTransitionToAsync(Guid prospectId, string newStatus)
        {
            var currentStatus = await GetCurrentStatusAsync(prospectId);
            
            if (currentStatus == null)
                return false;

            return ProspectStateMachine.CanTransition(currentStatus, newStatus);
        }

        public async Task<List<string>> GetAvailableTransitionsAsync(Guid prospectId)
        {
            var currentStatus = await GetCurrentStatusAsync(prospectId);
            
            if (currentStatus == null)
                return new List<string>();

            return ProspectStateMachine.GetAllowedTransitions(currentStatus);
        }
    }
}
