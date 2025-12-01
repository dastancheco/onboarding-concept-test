using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Onboarding.Core.Events
{
    /// <summary>
    /// Factory para resolver handlers de eventos.
    /// Aplica Factory Pattern: Centraliza la creación de objetos.
    /// Aplica SRP: Solo se encarga de resolver handlers.
    /// </summary>
    public interface IEventHandlerFactory
    {
        /// <summary>
        /// Obtiene el handler apropiado para un tipo de evento.
        /// </summary>
        /// <param name="eventType">Tipo de evento</param>
        /// <returns>Handler correspondiente o null si no existe</returns>
        IEventHandler? GetHandler(string eventType);

        /// <summary>
        /// Verifica si existe un handler registrado para un tipo de evento.
        /// </summary>
        /// <param name="eventType">Tipo de evento</param>
        /// <returns>True si existe un handler</returns>
        bool HasHandler(string eventType);
    }

    /// <summary>
    /// Implementación del factory de handlers de eventos.
    /// </summary>
    public class EventHandlerFactory : IEventHandlerFactory
    {
        private readonly IEnumerable<IEventHandler> _handlers;
        private readonly ILogger<EventHandlerFactory> _logger;

        public EventHandlerFactory(
            IEnumerable<IEventHandler> handlers,
            ILogger<EventHandlerFactory> logger)
        {
            _handlers = handlers;
            _logger = logger;

            _logger.LogInformation("EventHandlerFactory initialized with {Count} handlers", handlers.Count());
            
            foreach (var handler in handlers)
            {
                _logger.LogDebug("Registered handler: {HandlerType} for event: {EventType}",
                    handler.GetType().Name, handler.EventType);
            }
        }

        public IEventHandler? GetHandler(string eventType)
        {
            var handler = _handlers.FirstOrDefault(h => h.CanHandle(eventType));

            if (handler == null)
            {
                _logger.LogWarning("No handler found for event type: {EventType}", eventType);
            }
            else
            {
                _logger.LogDebug("Resolved handler: {HandlerType} for event: {EventType}",
                    handler.GetType().Name, eventType);
            }

            return handler;
        }

        public bool HasHandler(string eventType)
        {
            return _handlers.Any(h => h.CanHandle(eventType));
        }
    }
}
