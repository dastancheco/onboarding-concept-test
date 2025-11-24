namespace Onboarding.Api.DTOs
{
    public class PubSubEventDto
    {
        // El nombre del evento: "UserRegistered", "StepDataSubmitted"
        public string EventType { get; set; }

        // El JSON con los datos: { "app_id": "OVEX", "client_type": "CLIENT", ... }
        public string PayloadJson { get; set; }
    }
}
