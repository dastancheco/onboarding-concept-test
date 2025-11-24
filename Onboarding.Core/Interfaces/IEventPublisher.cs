using System;
using System.Threading.Tasks;

namespace Onboarding.Core.Interfaces
{
    /// <summary>
    /// Servicio para publicar eventos salientes.
    /// Permite notificar a otros sistemas sobre acciones completadas.
    /// Aplica DIP: Abstracción para diferentes implementaciones (in-memory, Pub/Sub, etc.)
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publica un evento con un payload específico.
        /// </summary>
        /// <param name="eventType">Tipo de evento (ej: "CreditCheckComplete", "ProspectApproved")</param>
        /// <param name="payload">Objeto con los datos del evento</param>
        /// <param name="topicName">Nombre del topic (opcional, usa default si es null)</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task PublishAsync(string eventType, object payload, string? topicName = null);

        /// <summary>
        /// Publica un evento con correlationId para trazabilidad.
        /// </summary>
        /// <param name="eventType">Tipo de evento</param>
        /// <param name="payload">Datos del evento</param>
        /// <param name="correlationId">ID para correlacionar eventos relacionados</param>
        /// <param name="topicName">Nombre del topic (opcional)</param>
        /// <returns>Task que representa la operación asíncrona</returns>
        Task PublishAsync(string eventType, object payload, string correlationId, string? topicName = null);
    }
}
