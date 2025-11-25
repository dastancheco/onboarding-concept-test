using Onboarding.Core.Models;
using System.Threading.Tasks;

namespace Onboarding.Core.Interfaces
{
    /// <summary>
    /// Servicio de validación de pasos con configuración dinámica.
    /// </summary>
    public interface IStepValidationService
    {
        /// <summary>
        /// Valida los datos de un paso del workflow.
        /// </summary>
        /// <param name="stepId">ID del paso a validar</param>
        /// <param name="payloadJson">JSON con los datos a validar</param>
        /// <returns>Resultado de la validación con lista de errores si aplica</returns>
        Task<ValidationResult> ValidateStepAsync(int stepId, string payloadJson);
    }
}
