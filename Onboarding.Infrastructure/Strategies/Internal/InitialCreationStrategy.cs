using System;
using System.Collections.Generic;
using System.Text;

using Onboarding.Core.Interfaces;

namespace Onboarding.Infrastructure.Strategies.Internal
{
    public class InitialCreationStrategy : IConcreteStrategy
    {
        public Task ExecuteAsync(Guid prospectId, string configJson, string payloadJson)
        {
            Console.WriteLine($"   -> [INFRA] INTERNAL_CODE: Ejecutando 'InitialCreationStrategy'...");
            Console.WriteLine($"      Creando registros base para ProspectId: {prospectId}");

            // Aquí iría lógica compleja C# (ej. generar passwords, asignar roles, etc.)

            return Task.CompletedTask;
        }
    }
}
