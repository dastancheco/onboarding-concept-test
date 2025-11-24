using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OvexDataModelingTest.Entities.App;

namespace Onboarding.Core.Interfaces
{
    /// <summary>
    /// Servicio responsable exclusivamente de la gestión de estados de prospectos.
    /// Aplica SRP: Single Responsibility Principle - Solo maneja transiciones de estado.
    /// </summary>
    public interface IProspectStatusService
    {
        /// <summary>
        /// Actualiza el estado de un prospecto con validación de transiciones.
        /// Registra el cambio en el historial de auditoría.
        /// </summary>
        /// <param name="prospectId">ID del prospecto</param>
        /// <param name="newStatus">Nuevo estado</param>
        /// <param name="reason">Razón del cambio</param>
        /// <param name="changedBy">Usuario o sistema que realizó el cambio</param>
        /// <param name="metadata">Metadata adicional (opcional)</param>
        /// <returns>True si el cambio fue exitoso</returns>
        Task<bool> UpdateStatusAsync(
            Guid prospectId, 
            string newStatus, 
            string? reason = null, 
            string? changedBy = "SYSTEM",
            string? metadata = null);

        /// <summary>
        /// Obtiene el historial completo de cambios de estado de un prospecto.
        /// </summary>
        /// <param name="prospectId">ID del prospecto</param>
        /// <returns>Lista ordenada de cambios de estado</returns>
        Task<List<ProspectStatusHistory>> GetStatusHistoryAsync(Guid prospectId);

        /// <summary>
        /// Obtiene el estado actual de un prospecto.
        /// </summary>
        /// <param name="prospectId">ID del prospecto</param>
        /// <returns>Estado actual o null si no existe</returns>
        Task<string?> GetCurrentStatusAsync(Guid prospectId);

        /// <summary>
        /// Verifica si una transición de estado es válida para un prospecto específico.
        /// </summary>
        /// <param name="prospectId">ID del prospecto</param>
        /// <param name="newStatus">Estado al que se quiere transicionar</param>
        /// <returns>True si la transición es válida</returns>
        Task<bool> CanTransitionToAsync(Guid prospectId, string newStatus);

        /// <summary>
        /// Obtiene todos los estados válidos a los que puede transicionar un prospecto.
        /// </summary>
        /// <param name="prospectId">ID del prospecto</param>
        /// <returns>Lista de estados válidos</returns>
        Task<List<string>> GetAvailableTransitionsAsync(Guid prospectId);
    }
}
