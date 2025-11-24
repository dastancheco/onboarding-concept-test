using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Services;
using Onboarding.Infrastructure.Data;
using Onboarding.Infrastructure.Data.Repositories;
using Onboarding.Infrastructure.Strategies;
using Onboarding.Infrastructure.Strategies.External;
using Onboarding.Infrastructure.Strategies.Internal;
using Onboarding.Infrastructure.Strategies.Rules;
using OvexDataModelingTest.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Onboarding.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // 1. Configurar BD en Memoria (Aquí se conecta el proyecto de prueba)
            services.AddDbContext<OnboardingDbContext>(options =>
                options.UseInMemoryDatabase("OvexProductionSimulation"));

            // 2. Repositorios Genéricos
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 3. Servicios del Core
            services.AddScoped<IOrchestratorService, OrchestratorService>();
            services.AddScoped<RuleEngine>();
            services.AddScoped<StepValidationService>();

            // 4. Estrategias
            services.AddScoped<IActionExecutor, ActionExecutorService>();
            services.AddScoped<ExternalApiStrategy>();
            services.AddScoped<InitialCreationStrategy>();
            services.AddScoped<RedirectStrategy>();

            // Registro de estrategias de operadores
            services.AddSingleton<IOperatorStrategy, EqualsStrategy>();
            services.AddSingleton<IOperatorStrategy, NotEqualsStrategy>();
            services.AddSingleton<IOperatorStrategy, GreaterThanStrategy>();
            services.AddSingleton<IOperatorStrategy, GreaterOrEqualStrategy>();
            services.AddSingleton<IOperatorStrategy, ContainsStrategy>();

            // Estrategias adicionales pueden ser registradas aquí

            return services;
        }
    }
}
