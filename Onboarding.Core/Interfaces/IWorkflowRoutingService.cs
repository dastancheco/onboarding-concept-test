using System;
using System.Threading.Tasks;

namespace Onboarding.Core.Interfaces
{
    /// <summary>
    /// Servicio responsable de enrutar usuarios a workflows.
    /// Aplica SRP: Single Responsibility Principle - Solo se encarga del ruteo.
    /// </summary>
    public interface IWorkflowRoutingService
    {
        /// <summary>
        /// Determina a qué workflow debe asignarse un usuario basándose en el payload.
        /// </summary>
        /// <param name="payloadJson">JSON con los datos del usuario</param>
        /// <returns>ID del workflow asignado, o 0 si no hay match</returns>
        Task<int> DetermineWorkflowAsync(string payloadJson);

        /// <summary>
        /// Verifica si existe una regla de ruteo para el payload dado.
        /// </summary>
        /// <param name="payloadJson">JSON con los datos del usuario</param>
        /// <returns>True si hay una regla que coincide</returns>
        Task<bool> HasMatchingRuleAsync(string payloadJson);
    }
}
