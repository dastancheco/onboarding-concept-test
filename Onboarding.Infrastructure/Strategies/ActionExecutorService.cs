using Microsoft.Extensions.DependencyInjection;
using Onboarding.Core.Interfaces;
using Onboarding.Infrastructure.Strategies.External;
using Onboarding.Infrastructure.Strategies.Internal;
using OvexDataModelingTest.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Onboarding.Infrastructure.Strategies
{
    public class ActionExecutorService : IActionExecutor
    {
        private readonly OnboardingDbContext _db;
        private readonly IServiceProvider _serviceProvider;

        public ActionExecutorService(OnboardingDbContext db, IServiceProvider serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
        }

        public async Task ExecuteActionAsync(string actionKey, Guid prospectId, string payloadJson)
        {
            // 1. Buscar la definición de la estrategia en BD
            var strategyDef = await _db.InstanceActionStrategies.FindAsync(actionKey);

            if (strategyDef == null)
            {
                Console.WriteLine($"[ERROR] No se encontró configuración para la Acción: {actionKey}");
                return;
            }

            IConcreteStrategy strategyToExecute = null;

            // 2. Decidir qué ejecutar (Factory Pattern)
            if (strategyDef.ImplementationType == "EXTERNAL_API")
            {
                // Usamos la estrategia genérica para APIs
                strategyToExecute = _serviceProvider.GetRequiredService<ExternalApiStrategy>();
            }
            else if (strategyDef.ImplementationType == "INTERNAL_CODE")
            {
                // Leemos el nombre de la clase del JSON de configuración
                var details = JsonNode.Parse(strategyDef.ImplementationDetails);
                var className = details?["class_name"]?.ToString();

                strategyToExecute = className switch
                {
                    "InitialCreationStrategy" => _serviceProvider.GetRequiredService<InitialCreationStrategy>(),
                    "RedirectStrategy" => _serviceProvider.GetRequiredService<RedirectStrategy>(),
                    "ProprietaryParametric" => null, // TODO: Implementar clase
                    _ => throw new NotImplementedException($"Clase C# no implementada: {className}")
                };
            }

            // 3. Ejecutar
            if (strategyToExecute != null)
            {
                await strategyToExecute.ExecuteAsync(prospectId, strategyDef.ImplementationDetails, payloadJson);
            }
        }
    }
}
