using System;
using System.Threading.Tasks;

namespace Onboarding.Core.Interfaces
{
    /// <summary>
    /// Servicio para gestionar la actualización incremental de datos del prospecto.
    /// </summary>
    public interface IProspectDataService
    {
        /// <summary>
        /// Actualiza los datos del prospecto haciendo merge con el JSON existente.
        /// Si el prospecto no tiene datos previos, crea el registro.
        /// </summary>
        /// <param name="prospectId">ID del prospecto</param>
        /// <param name="newDataJson">JSON con los nuevos datos a mergear</param>
        /// <returns>El JSON completo actualizado</returns>
        Task<string> UpdateProspectDataAsync(Guid prospectId, string newDataJson);

        /// <summary>
        /// Obtiene todos los datos actuales del prospecto.
        /// </summary>
        /// <param name="prospectId">ID del prospecto</param>
        /// <returns>JSON con todos los datos, o string vacío si no existe</returns>
        Task<string> GetProspectDataAsync(Guid prospectId);

        /// <summary>
        /// Verifica si el prospecto tiene un campo específico ya capturado.
        /// Útil para el flujo de Re-Onboarding.
        /// </summary>
        /// <param name="prospectId">ID del prospecto</param>
        /// <param name="fieldKey">Nombre del campo a buscar</param>
        /// <returns>True si el campo existe y tiene valor</returns>
        Task<bool> HasFieldAsync(Guid prospectId, string fieldKey);
    }
}
