using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Onboarding.Core.Events.Handlers
{
    /// <summary>
    /// Handler genérico para eventos que solo necesitan ejecutar reglas de negocio.
    /// No realiza ninguna acción específica, solo registra el evento.
    /// Útil para eventos como "CreditCheckComplete", "SatCheckComplete", etc.
    /// </summary>
    public class GenericEventHandler : IEventHandler
    {
        private readonly string _eventType;
        private readonly ILogger<GenericEventHandler> _logger;

        public string EventType => _eventType;

        public GenericEventHandler(string eventType, ILogger<GenericEventHandler> logger)
        {
            _eventType = eventType;
            _logger = logger;
        }

        public Task HandleAsync(EventContext context)
        {
            _logger.LogInformation(
                "Handling generic event: {EventType} for ProspectId: {ProspectId}. CorrelationId: {CorrelationId}",
                context.EventType, context.ProspectId, context.CorrelationId);

            // Para eventos genéricos, solo registramos y dejamos que las reglas se ejecuten
            // No hay lógica específica de procesamiento

            return Task.CompletedTask;
        }
    }
}
