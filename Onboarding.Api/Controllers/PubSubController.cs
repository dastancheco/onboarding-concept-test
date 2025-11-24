using Microsoft.AspNetCore.Mvc;
using Onboarding.Api.DTOs;
using Onboarding.Core.Interfaces;

namespace Onboarding.Api.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class PubSubController(IOrchestratorService orchestrator) : ControllerBase
    {
        private readonly IOrchestratorService _orchestrator = orchestrator;

        [HttpPost("push")]
        public async Task<IActionResult> ReceivePushEvent([FromBody] PubSubEventDto eventDto)
        {
            Console.WriteLine($"\n[API] HTTP POST recibido. Evento: {eventDto.EventType}");

            if (string.IsNullOrEmpty(eventDto.PayloadJson))
            {
                return BadRequest("El PayloadJson es requerido.");
            }

            try
            {
                // Delegamos al Core para que decida qué hacer (Ruteo, Reglas, Acciones)
                await _orchestrator.ProcessEventAsync(eventDto.EventType, eventDto.PayloadJson);

                // Retornamos 200 OK para confirmar recepción (ACK)
                return Ok(new { status = "Event Processed" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API ERROR] {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
