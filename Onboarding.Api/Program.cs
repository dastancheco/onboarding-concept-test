using Onboarding.Infrastructure.Extensions; // Para AddInfrastructureServices
using OvexDataModelingTest.Data; // Para OvexDbContext y Seeder

var builder = WebApplication.CreateBuilder(args);

// 1. AGREGAR SERVICIOS
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// INYECCIÓN DE DEPENDENCIAS DE NUESTRA ARQUITECTURA
// Esto registra Core, Infraestructura, Repositorios, Estrategias y la BD en Memoria
builder.Services.AddInfrastructureServices();

var app = builder.Build();

// 2. CONFIGURAR PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// 3. SEMBRAR DATOS DE PRUEBA (OVEX LEGACY Y FLOTILLA)
// Esto simula que nuestra BD ya tiene la configuración cargada al iniciar
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OnboardingDbContext>();

    // Llamamos al Seeder que creamos en OvexDataModelingTest/Infrastructure
    // Esto inserta los Workflows 10 y 20, las Reglas y las Estrategias.
    OvexDataSeeder.Seed(dbContext);
}

Console.WriteLine("SISTEMA ONBOARDING INICIADO.");

app.Run();