using Onboarding.TestClient.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configurar HttpClient para comunicarse con la API de Onboarding
builder.Services.AddHttpClient<IOnboardingApiClient, OnboardingApiClient>(client =>
{
    // Cambiar el puerto según tu configuración de la API
    client.BaseAddress = new Uri("http://localhost:5014/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigureHttpClient((sp, client) =>
{
    // Configuración adicional del HttpClient
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Configurar JSON options globalmente para que sea case-insensitive
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
