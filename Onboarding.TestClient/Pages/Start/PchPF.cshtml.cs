using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Onboarding.TestClient.Models;
using Onboarding.TestClient.Services;
using System.Threading.Tasks;

namespace Onboarding.TestClient.Pages.Start
{
    public class PchPFModel : PageModel
    {
        private readonly IOnboardingApiClient _apiClient;
        private readonly ILogger<PchPFModel> _logger;

        public PchPFModel(IOnboardingApiClient apiClient, ILogger<PchPFModel> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        [BindProperty]
        public string Nombres { get; set; } = string.Empty;

        [BindProperty]
        public string PrimerApellido { get; set; } = string.Empty;

        [BindProperty]
        public string SegundoApellido { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Telefono { get; set; } = string.Empty;

        [BindProperty]
        public string RFC { get; set; } = string.Empty;

        [BindProperty]
        public string Nacionalidad { get; set; } = string.Empty;

        [BindProperty]
        public bool EULA { get; set; } = false;

        [BindProperty]
        public char Gender { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Mostrar formulario
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Los nombres son requeridos";
                return Page();
            }

            try
            {
                _logger.LogInformation("Creating PCH PF prospect for email: {Email}", Email);

                // Crear el prospecto mediante comando REST (sincrónico)
                var request = new CreateProspectRequest
                {
                    Email = Email,
                    AppId = "PCH",
                    ClientType = "PF",
                    Country = "MX"
                };

                var response = await _apiClient.CreateProspectAsync(request);

                if (response == null)
                {
                    ErrorMessage = "No se pudo crear el prospecto. Por favor, intenta de nuevo.";
                    _logger.LogWarning("Failed to create prospect for email: {Email}", Email);
                    return Page();
                }

                _logger.LogInformation(
                    "Prospect created: ProspectId={ProspectId}, WorkflowId={WorkflowId}",
                    response.ProspectId, response.WorkflowId);

                // Guardar en TempData
                TempData["ProspectId"] = response.ProspectId.ToString();
                TempData["WorkflowId"] = response.WorkflowId;
                TempData["OnboardingType"] = "PCH_PM";
                TempData["UserEmail"] = Email;

                // NUEVO: Guardar también en Cookie como respaldo (expira en 30 minutos)
                Response.Cookies.Append("ProspectId", response.ProspectId.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                });
                Response.Cookies.Append("WorkflowId", response.WorkflowId.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                });

                _logger.LogInformation("ProspectId stored in TempData and Cookie");

                // Redirigir al wizard de onboarding
                return RedirectToPage("/Start/Wizard");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating prospect for email: {Email}", Email);
                ErrorMessage = $"Error al iniciar el proceso: {ex.Message}";
                return Page();
            }
        }
    }
}
