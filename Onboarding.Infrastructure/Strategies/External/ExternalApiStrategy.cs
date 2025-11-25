using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Onboarding.Core.Interfaces;

namespace Onboarding.Infrastructure.Strategies.External
{
    public class ExternalApiStrategy : IConcreteStrategy
    {
        // Simula una llamada HTTP genérica (Syntage, Buró, Ovex Legacy)
        public async Task ExecuteAsync(Guid prospectId, string configJson, string payloadJson)
        {
            var config = JsonNode.Parse(configJson);
            var url = config?["url"]?.ToString();
            var method = config?["method"]?.ToString() ?? "POST";

            Console.WriteLine($"   -> [INFRA] 🌐 EXTERNAL_API Call iniciada...");
            Console.WriteLine($"      URL: {url} ({method})");

            // Simulación de Latencia de red
            await Task.Delay(100);

            Console.WriteLine($"      [SUCCESS] 200 OK recibido de {url}");

            // NOTA: En un caso real, aquí parsearíamos la respuesta y actualizaríamos App.ProspectData
            // usando el repositorio de ProspectData.
        }
    }
}