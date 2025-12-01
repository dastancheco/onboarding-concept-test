using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Onboarding.TestClient.Models;
using Onboarding.TestClient.Services;

namespace Onboarding.TestClient.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly IOnboardingApiClient _apiClient;
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(IOnboardingApiClient apiClient, ILogger<DashboardModel> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // Estadísticas
        public DashboardStatisticsResponse? Statistics { get; set; }

        // Lista de prospectos
        public DashboardProspectListResponse? ProspectList { get; set; }

        // Timeline
        public DashboardTimelineResponse? Timeline { get; set; }

        // Búsqueda
        public DashboardSearchResponse? SearchResults { get; set; }

        // Filtros
        [BindProperty(SupportsGet = true)]
        public int? WorkflowFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        public string ActiveTab { get; set; } = "overview";

        public async Task<IActionResult> OnGetAsync(string? tab = null)
        {
            ActiveTab = tab ?? "overview";

            try
            {
                // Siempre cargar estadísticas
                Statistics = await _apiClient.GetDashboardStatisticsAsync();

                switch (ActiveTab.ToLower())
                {
                    case "overview":
                        // Cargar timeline para el overview
                        Timeline = await _apiClient.GetDashboardTimelineAsync(days: 7, limit: 10);
                        break;

                    case "prospects":
                        // Cargar lista de prospectos con filtros
                        ProspectList = await _apiClient.GetDashboardProspectsAsync(
                            workflowId: WorkflowFilter,
                            status: StatusFilter,
                            page: CurrentPage,
                            pageSize: 20
                        );
                        break;

                    case "timeline":
                        // Cargar timeline completa
                        Timeline = await _apiClient.GetDashboardTimelineAsync(days: 30, limit: 50);
                        break;

                    case "search":
                        if (!string.IsNullOrWhiteSpace(SearchQuery))
                        {
                            SearchResults = await _apiClient.SearchProspectsAsync(SearchQuery);
                        }
                        break;
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "Error al cargar el dashboard. Por favor, intenta de nuevo.";
                return Page();
            }
        }

        // New handler to serve prospect details to client-side
        public async Task<IActionResult> OnGetProspectDetailAsync(string prospectId)
        {
            if (string.IsNullOrWhiteSpace(prospectId))
                return BadRequest(new { error = "prospectId is required" });

            try
            {
                var detail = await _apiClient.GetProspectDetailsAsync(prospectId);
                if (detail == null)
                    return NotFound();

                return new JsonResult(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prospect detail for {ProspectId}", prospectId);
                return StatusCode(500, new { error = "Server error" });
            }
        }

        public string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "IN_PROGRESS" => "badge bg-primary",
                "COMPLETED" => "badge bg-success",
                "REJECTED" => "badge bg-danger",
                "PENDING_REVIEW" => "badge bg-warning",
                "IN_REVIEW" => "badge bg-info",
                "STARTED" => "badge bg-secondary",
                _ => "badge bg-secondary"
            };
        }

        public string GetStatusText(string status)
        {
            return status switch
            {
                "IN_PROGRESS" => "En Progreso",
                "COMPLETED" => "Completado",
                "REJECTED" => "Rechazado",
                "PENDING_REVIEW" => "Pendiente de Revisión",
                "IN_REVIEW" => "En Revisión",
                "STARTED" => "Iniciado",
                "MORE_INFO_REQUIRED" => "Requiere Más Información",
                "FAILED" => "Fallido",
                "CANCELLED" => "Cancelado",
                _ => status
            };
        }

        public string GetEventTypeIcon(string eventType)
        {
            return eventType switch
            {
                "created" => "bi-plus-circle",
                "updated" => "bi-pencil-square",
                "completed" => "bi-check-circle",
                _ => "bi-circle"
            };
        }

        public string GetEventTypeColor(string eventType)
        {
            return eventType switch
            {
                "created" => "text-success",
                "updated" => "text-primary",
                "completed" => "text-info",
                _ => "text-secondary"
            };
        }
    }
}
