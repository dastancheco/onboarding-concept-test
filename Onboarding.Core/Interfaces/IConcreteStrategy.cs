namespace Onboarding.Core.Interfaces
{
    /// <summary>
    /// Interfaz base para todas las estrategias de acción concretas.
    /// Aplica Strategy Pattern - Las estrategias pueden intercambiarse dinámicamente.
    /// Aplica OCP: Open/Closed Principle - Extensible agregando nuevas estrategias sin modificar código existente.
    /// </summary>
    public interface IConcreteStrategy
    {
        /// <summary>
        /// Ejecuta la estrategia con los parámetros proporcionados.
        /// </summary>
        /// <param name="prospectId">ID del prospecto afectado</param>
        /// <param name="configJson">Configuración JSON específica de la estrategia</param>
        /// <param name="payloadJson">Payload del evento que disparó la acción</param>
        /// <returns>Tarea que representa la operación asíncrona</returns>
        Task ExecuteAsync(Guid prospectId, string configJson, string payloadJson);
    }
}
