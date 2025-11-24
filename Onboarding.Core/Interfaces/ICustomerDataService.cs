using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Onboarding.Core.Interfaces
{
    /// <summary>
    /// Servicio para gestionar el "Golden Record" del cliente.
    /// Maneja datos con Scope="USER" que persisten entre múltiples onboardings.
    /// </summary>
    public interface ICustomerDataService
    {
        /// <summary>
        /// Promueve datos del prospecto al Golden Record del usuario.
        /// Solo promueve campos con Scope="USER" definidos en FieldDefinitions.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="prospectId">ID del prospecto cuyo datos se van a promover</param>
        /// <returns>Lista de campos promovidos</returns>
        Task<List<string>> PromoteToGoldenRecordAsync(Guid userId, Guid prospectId);

        /// <summary>
        /// Obtiene los datos del Golden Record del usuario.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>JSON con los datos del usuario, o null si no existe</returns>
        Task<string?> GetCustomerDataAsync(Guid userId);

        /// <summary>
        /// Verifica si el usuario tiene un campo específico en su Golden Record.
        /// Útil para decidir si saltar pasos en Re-Onboarding.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="fieldKey">Nombre del campo</param>
        /// <returns>True si el campo existe y tiene valor</returns>
        Task<bool> HasGoldenRecordFieldAsync(Guid userId, string fieldKey);

        /// <summary>
        /// Obtiene el valor de un campo específico del Golden Record.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="fieldKey">Nombre del campo</param>
        /// <returns>Valor del campo, o null si no existe</returns>
        Task<string?> GetGoldenRecordFieldAsync(Guid userId, string fieldKey);

        /// <summary>
        /// Actualiza o crea el Golden Record con nuevos datos.
        /// Solo actualiza si los datos son más recientes que los existentes.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="dataJson">JSON con los datos a actualizar</param>
        /// <returns>True si se actualizó, False si se ignoró por ser más antiguo</returns>
        Task<bool> UpdateCustomerDataAsync(Guid userId, string dataJson);
    }
}
