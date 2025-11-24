using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onboarding.Infrastructure.Events
{
    /// <summary>
    /// Implementación in-memory de IEventPublisher para testing y desarrollo.
    /// Los eventos se loggean pero no se envían a ningún sistema externo.
    /// </summary>
    public class InMemoryEventPublisher : IEventPublisher
    {
        private readonly ILogger<InMemoryEventPublisher> _logger;

        public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync(string eventType, object payload, string? topicName = null)
        {
            return PublishAsync(eventType, payload, Guid.NewGuid().ToString(), topicName);
        }

        public Task PublishAsync(string eventType, object payload, string correlationId, string? topicName = null)
        {
            var topic = topicName ?? "default-topic";
            
            _logger.LogInformation(
                "[IN-MEMORY EVENT] Type: {EventType}, Topic: {Topic}, CorrelationId: {CorrelationId}",
                eventType, topic, correlationId);

            // Serializar payload para logging
            string payloadJson;
            if (payload is string str)
            {
                payloadJson = str;
            }
            else
            {
                try
                {
                    payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
                catch
                {
                    payloadJson = payload?.ToString() ?? "null";
                }
            }

            _logger.LogDebug("[IN-MEMORY EVENT] Payload: {Payload}", payloadJson);

            // En un entorno real, aquí se enviaría a Pub/Sub, RabbitMQ, etc.
            // Por ahora, solo loggeamos

            return Task.CompletedTask;
        }
    }
}
