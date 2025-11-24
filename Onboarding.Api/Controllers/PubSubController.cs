using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Onboarding.Api.DTOs;
using Onboarding.Core.Interfaces;

namespace Onboarding.Api.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class PubSubController : ControllerBase
    {
        private readonly IOrchestratorService _orchestrator;
        private readonly ILogger<PubSubController> _logger;

        public PubSubController(
            IOrchestratorService orchestrator,
            ILogger<PubSubController> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        [HttpPost("push")]
        public async Task<IActionResult> ReceivePushEvent([FromBody] PubSubEventDto eventDto)
        {
            // Generar Correlation ID para trazabilidad
            var correlationId = HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? Guid.NewGuid().ToString();

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["EventType"] = eventDto.EventType
            }))
            {
                _logger.LogInformation("HTTP POST recibido. Evento: {EventType}", eventDto.EventType);

                if (string.IsNullOrEmpty(eventDto.PayloadJson))
                {
                    _logger.LogWarning("PayloadJson es requerido pero está vacío");
                    return BadRequest(new { error = "El PayloadJson es requerido." });
                }

                try
                {
                    // Delegamos al Core para que decida qué hacer (Ruteo, Reglas, Acciones)
                    await _orchestrator.ProcessEventAsync(eventDto.EventType, eventDto.PayloadJson);

                    _logger.LogInformation("Evento procesado con éxito");

                    // Retornamos 200 OK para confirmar recepción (ACK)
                    return Ok(new
                    {
                        status = "Evento Procesado",
                        correlationId = correlationId
                    });
                }
                catch (ArgumentException argEx)
                {
                    _logger.LogWarning(argEx, "Error de validación al procesar el evento");
                    return BadRequest(new
                    {
                        error = "Error de validación",
                        detail = argEx.Message,
                        correlationId = correlationId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inesperado al procesar el evento");
                    return StatusCode(500, new
                    {
                        error = "Error interno del servidor",
                        detail = "Ha ocurrido un error inesperado. Por favor, contacte al soporte.",
                        correlationId = correlationId
                    });
                }
            }
        }
    }
}
