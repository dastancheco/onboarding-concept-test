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
using Onboarding.Infrastructure.Events;
using OvexDataModelingTest.Data;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

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

            // 3. Servicios del Core - ARQUITECTURA PRINCIPAL
            services.AddScoped<IOrchestratorService, OrchestratorService>();
            services.AddScoped<IRuleEngine, RuleEngine>();
            services.AddScoped<IStepValidationService, StepValidationService>();

            // 4. PERSISTENCIA DE DATOS
            services.AddScoped<IProspectDataService, ProspectDataService>();
            services.AddScoped<ICustomerDataService, CustomerDataService>();
            services.AddScoped<IUserManagementService, UserManagementService>();
            
            // 5. OPCIÓN C: GESTIÓN DE ESTADOS Y RUTEO
            services.AddScoped<IProspectStatusService, ProspectStatusService>();
            services.AddScoped<IWorkflowRoutingService, WorkflowRoutingService>();

            // 6. EVENT PUBLISHER
            // Usar InMemoryEventPublisher para desarrollo/testing
            // En producción, reemplazar con GooglePubSubPublisher
            services.AddScoped<IEventPublisher, InMemoryEventPublisher>();

            // 7. HTTP CLIENT CON POLLY (Resiliencia)
            services.AddHttpClient("ExternalAPIs")
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy())
                .SetHandlerLifetime(TimeSpan.FromMinutes(5)); // Recrear handler cada 5 minutos

            // 8. Estrategias
            services.AddScoped<IActionExecutor, ActionExecutorService>();
            services.AddScoped<ExternalApiStrategy>(); // Simulado (legacy)
            services.AddScoped<RealExternalApiStrategy>(); // NUEVO: Real con HttpClient
            services.AddScoped<InitialCreationStrategy>();
            services.AddScoped<RedirectStrategy>();
            services.AddScoped<PromoteToGoldenRecordStrategy>();
            services.AddScoped<UpdateProspectStatusStrategy>();

            // 9. Registro de estrategias de operadores
            services.AddSingleton<IOperatorStrategy, EqualsStrategy>();
            services.AddSingleton<IOperatorStrategy, NotEqualsStrategy>();
            services.AddSingleton<IOperatorStrategy, GreaterThanStrategy>();
            services.AddSingleton<IOperatorStrategy, GreaterOrEqualStrategy>();
            services.AddSingleton<IOperatorStrategy, ContainsStrategy>();

            // 10. VALIDADORES DE CAMPOS (Strategy Pattern)
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.RequiredValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.RangeValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.LengthValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.DataTypeValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.AllowedValuesValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.RfcValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.CurpValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.EmailValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.PhoneValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.PostalCodeValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.UrlValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.DateValidator>();
            services.AddSingleton<IFieldValidator, Onboarding.Core.Validators.RegexValidator>();

            // Validation pipeline
            services.AddScoped<IValidationPipeline, ValidationPipeline>();

            return services;
        }

        /// <summary>
        /// Política de retry con backoff exponencial.
        /// Reintenta 3 veces con delays de 2s, 4s, 8s.
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError() // 5xx y 408 Request Timeout
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // 429
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        // Logging de retry (se puede inyectar ILogger si es necesario)
                        Console.WriteLine(
                            $"[POLLY RETRY] Attempt {retryAttempt} after {timespan.TotalSeconds}s. " +
                            $"Reason: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                    });
        }

        /// <summary>
        /// Circuit breaker: Abre el circuito después de 5 fallos consecutivos.
        /// Permanece abierto por 30 segundos antes de intentar nuevamente.
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, duration) =>
                    {
                        Console.WriteLine(
                            $"[POLLY CIRCUIT BREAKER] Circuit opened for {duration.TotalSeconds}s. " +
                            $"Reason: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                    },
                    onReset: () =>
                    {
                        Console.WriteLine("[POLLY CIRCUIT BREAKER] Circuit closed. Resuming normal operations.");
                    },
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("[POLLY CIRCUIT BREAKER] Circuit half-open. Testing if service recovered.");
                    });
        }
    }
}
