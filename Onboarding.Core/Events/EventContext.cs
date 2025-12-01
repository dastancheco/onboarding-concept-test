using System;

namespace Onboarding.Core.Events
{
    /// <summary>
    /// Contexto que se pasa a los handlers de eventos.
    /// Encapsula toda la información necesaria para procesar un evento.
    /// </summary>
    public class EventContext
    {
        /// <summary>
        /// Tipo de evento (ej: "UserRegistered", "StepDataSubmitted")
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Payload del evento en formato JSON
        /// </summary>
        public string PayloadJson { get; set; } = string.Empty;

        /// <summary>
        /// ID del prospecto (puede ser null en eventos de registro)
        /// </summary>
        public Guid? ProspectId { get; set; }

        /// <summary>
        /// ID del workflow (puede ser null en eventos de registro)
        /// </summary>
        public int? WorkflowId { get; set; }

        /// <summary>
        /// ID de correlación para trazabilidad
        /// </summary>
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Timestamp de cuando se recibió el evento
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Metadata adicional (headers HTTP, etc.)
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
