using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;

using Onboarding.Core.Interfaces;

namespace Onboarding.Infrastructure.Strategies.Internal
{
    public class RedirectStrategy : IConcreteStrategy
    {
        public Task ExecuteAsync(Guid prospectId, string configJson, string payloadJson)
        {
            var config = JsonNode.Parse(configJson);
            var targetUrl = config?["url"]?.ToString();

            Console.WriteLine($"   -> [INFRA] ↩️ REDIRECT: Generando instrucción de redirección a {targetUrl}");
            return Task.CompletedTask;
        }
    }
}
