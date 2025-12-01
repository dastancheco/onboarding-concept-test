using System.Threading.Tasks;

namespace Onboarding.Core.Events
{
    /// <summary>
    /// Interfaz para handlers de eventos específicos.
    /// Cada tipo de evento debe tener su propia implementación.
    /// Aplica Strategy Pattern: Algoritmos intercambiables.
    /// Aplica SRP: Cada handler maneja un solo tipo de evento.
    /// </summary>
    public interface IEventHandler
    {
        /// <summary>
        /// Tipo de evento que este handler puede procesar.
        /// Ejemplo: "UserRegistered", "StepDataSubmitted", "CreditCheckComplete"
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// Procesa el evento con el contexto proporcionado.
        /// </summary>
        /// <param name="context">Contexto del evento con toda la información necesaria</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task HandleAsync(EventContext context);

        /// <summary>
        /// Indica si este handler puede procesar el evento dado.
        /// Por defecto compara el EventType, pero puede ser sobrescrito para lógica más compleja.
        /// </summary>
        /// <param name="eventType">Tipo de evento a evaluar</param>
        /// <returns>True si puede manejar el evento</returns>
        bool CanHandle(string eventType)
        {
            return EventType.Equals(eventType, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
