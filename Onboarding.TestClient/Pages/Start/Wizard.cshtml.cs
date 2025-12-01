using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Onboarding.TestClient.Models;
using Onboarding.TestClient.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Onboarding.TestClient.Pages.Start
{
    public class WizardModel : PageModel
    {
        private readonly IOnboardingApiClient _apiClient;
        private readonly ILogger<WizardModel> _logger;

        public WizardModel(IOnboardingApiClient apiClient, ILogger<WizardModel> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // Identificación del prospect
        public Guid ProspectId { get; set; }
        public int WorkflowId { get; set; }
        
        // Datos del workflow
        public WorkflowConfiguration? WorkflowConfig { get; set; }
        public string OnboardingType { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        
        // Estado del wizard
        public int CurrentStepIndex { get; set; }
        public int TotalSteps { get; set; }
        public StepConfiguration? CurrentStep { get; set; }
        public PhaseConfiguration? CurrentPhase { get; set; }
        
        // Mensajes
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Recuperar mensaje de éxito si existe (desde redirect)
                SuccessMessage = TempData["SuccessMessage"] as string;
                
                // Recuperar ProspectId de TempData (creado en páginas iniciales)
                var prospectIdStr = TempData["ProspectId"] as string;
                
                // NUEVO: Si no está en TempData, buscar en Cookie
                if (string.IsNullOrEmpty(prospectIdStr))
                {
                    prospectIdStr = Request.Cookies["ProspectId"];
                    _logger.LogInformation("ProspectId not in TempData, retrieved from Cookie: {ProspectId}", prospectIdStr);
                }
                
                if (string.IsNullOrEmpty(prospectIdStr) || !Guid.TryParse(prospectIdStr, out var prospectId))
                {
                    _logger.LogWarning("No ProspectId found in TempData or Cookie");
                    
                    // Si hay mensaje de éxito, mostrar página de completitud sin error
                    if (!string.IsNullOrEmpty(SuccessMessage))
                    {
                        return Page();
                    }
                    
                    ErrorMessage = "No se encontró el prospecto. Por favor, comienza de nuevo.";
                    return Page();
                }

                ProspectId = prospectId;
                
                // Recuperar WorkflowId
                var workflowIdObj = TempData["WorkflowId"];
                if (workflowIdObj == null && int.TryParse(Request.Cookies["WorkflowId"], out var cookieWorkflowId))
                {
                    WorkflowId = cookieWorkflowId;
                    _logger.LogInformation("WorkflowId retrieved from Cookie: {WorkflowId}", WorkflowId);
                }
                else
                {
                    WorkflowId = (int)(workflowIdObj ?? 0);
                }
                
                OnboardingType = TempData["OnboardingType"] as string ?? "UNKNOWN";
                UserEmail = TempData["UserEmail"] as string ?? "";

                // Mantener datos en TempData para siguientes requests
                TempData.Keep("ProspectId");
                TempData.Keep("WorkflowId");
                TempData.Keep("OnboardingType");
                TempData.Keep("UserEmail");

                _logger.LogInformation(
                    "Loading wizard for ProspectId={ProspectId}, WorkflowId={WorkflowId}",
                    ProspectId, WorkflowId);

                // Obtener configuración del workflow
                WorkflowConfig = await _apiClient.GetWorkflowConfigurationAsync(WorkflowId);
                
                if (WorkflowConfig == null)
                {
                    ErrorMessage = "No se pudo cargar la configuración del workflow";
                    return Page();
                }

                // Calcular total de steps
                TotalSteps = WorkflowConfig.Phases.Sum(p => p.Steps.Count);
                
                // Obtener step actual (por defecto el primero)
                var stepIndexStr = TempData["CurrentStepIndex"] as string;
                CurrentStepIndex = !string.IsNullOrEmpty(stepIndexStr) && int.TryParse(stepIndexStr, out var idx) ? idx : 0;
                
                LoadCurrentStep();

                _logger.LogInformation(
                    "Wizard loaded: CurrentStepIndex={CurrentStepIndex}/{TotalSteps}",
                    CurrentStepIndex, TotalSteps);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading wizard");
                ErrorMessage = $"Error al cargar el wizard: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(int currentStepIndex)
        {
            try
            {
                // Recuperar datos de TempData
                var prospectIdStr = TempData["ProspectId"] as string;
                
                // NUEVO: Si no está en TempData, buscar en Cookie
                if (string.IsNullOrWhiteSpace(prospectIdStr))
                {
                    prospectIdStr = Request.Cookies["ProspectId"];
                    _logger.LogInformation("ProspectId not in TempData, retrieved from Cookie: {ProspectId}", prospectIdStr);
                }
                
                if (string.IsNullOrWhiteSpace(prospectIdStr) || !Guid.TryParse(prospectIdStr, out var prospectId))
                {
                    ErrorMessage = "No se encontró el prospecto. Por favor, comienza de nuevo.";
                    return Page();
                }

                ProspectId = prospectId;
                
                // Recuperar WorkflowId
                var workflowIdObj = TempData["WorkflowId"];
                if (workflowIdObj == null && int.TryParse(Request.Cookies["WorkflowId"], out var cookieWorkflowId))
                {
                    WorkflowId = cookieWorkflowId;
                }
                else
                {
                    WorkflowId = (int)(workflowIdObj ?? 0);
                }
                
                OnboardingType = TempData["OnboardingType"] as string ?? "UNKNOWN";
                UserEmail = TempData["UserEmail"] as string ?? "";

                TempData.Keep("ProspectId");
                TempData.Keep("WorkflowId");
                TempData.Keep("OnboardingType");
                TempData.Keep("UserEmail");

                _logger.LogInformation(
                    "Processing step {CurrentStepIndex} for ProspectId={ProspectId}",
                    currentStepIndex, ProspectId);

                // 1. Recopilar datos del formulario
                var stepData = new Dictionary<string, object>();
                foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("field_")))
                {
                    var fieldKey = key.Replace("field_", "");
                    var value = Request.Form[key].ToString();
                    
                    // NUEVO: Intentar parsear como JSON si parece ser un array
                    if (value.StartsWith("[") && value.EndsWith("]"))
                    {
                        try
                        {
                            var jsonDoc = JsonDocument.Parse(value);
                            stepData[fieldKey] = jsonDoc.RootElement.Clone();
                        }
                        catch
                        {
                            // Si falla el parseo, guardar como string
                            stepData[fieldKey] = value;
                        }
                    }
                    else
                    {
                        stepData[fieldKey] = value;
                    }
                }

                _logger.LogInformation(
                    "Collected {FieldCount} fields from form",
                    stepData.Count);

                // 2. Enviar datos al backend (comando REST)
                var submitRequest = new SubmitStepDataRequest
                {
                    DataJson = JsonSerializer.Serialize(stepData)
                };

                var submitResponse = await _apiClient.SubmitStepDataAsync(ProspectId, submitRequest);

                if (submitResponse == null)
                {
                    // Error en submit, recargar página con error
                    ErrorMessage = "Error al guardar los datos del paso. Por favor, intenta de nuevo.";
                    _logger.LogWarning("Failed to submit step data for ProspectId={ProspectId}", ProspectId);
                    
                    // Recargar configuración para mostrar el formulario de nuevo
                    WorkflowConfig = await _apiClient.GetWorkflowConfigurationAsync(WorkflowId);
                    TotalSteps = WorkflowConfig!.Phases.Sum(p => p.Steps.Count);
                    CurrentStepIndex = currentStepIndex;
                    LoadCurrentStep();
                    
                    return Page();
                }

                _logger.LogInformation(
                    "Step data submitted successfully: {ValidationResult}",
                    submitResponse.ValidationResult);

                // 3. Avanzar al siguiente step (comando REST)
                var advanceResponse = await _apiClient.AdvanceToNextStepAsync(ProspectId);

                if (advanceResponse == null)
                {
                    ErrorMessage = "Error al avanzar al siguiente paso. Por favor, intenta de nuevo.";
                    _logger.LogWarning("Failed to advance step for ProspectId={ProspectId}", ProspectId);
                    
                    // Recargar configuración para mostrar el formulario de nuevo
                    WorkflowConfig = await _apiClient.GetWorkflowConfigurationAsync(WorkflowId);
                    TotalSteps = WorkflowConfig!.Phases.Sum(p => p.Steps.Count);
                    CurrentStepIndex = currentStepIndex;
                    LoadCurrentStep();
                    
                    return Page();
                }

                // 4. Verificar si el workflow está completo
                if (advanceResponse.Completed)
                {
                    _logger.LogInformation("Workflow completed for ProspectId={ProspectId}", ProspectId);
                    
                    // Limpiar TempData y Cookies
                    TempData.Remove("ProspectId");
                    TempData.Remove("WorkflowId");
                    TempData.Remove("CurrentStepIndex");
                    Response.Cookies.Delete("ProspectId");
                    Response.Cookies.Delete("WorkflowId");
                    
                    // Usar TempData para el mensaje de éxito (persiste en redirect)
                    TempData["SuccessMessage"] = "¡Proceso completado exitosamente!";
                    
                    return RedirectToPage("/Start/Wizard");
                }

                // 5. Actualizar índice del step actual y hacer redirect (PRG pattern)
                CurrentStepIndex = currentStepIndex + 1;
                TempData["CurrentStepIndex"] = CurrentStepIndex.ToString();

                _logger.LogInformation(
                    "Advanced to step {CurrentStepIndex}/{TotalSteps}. Redirecting to reload wizard.",
                    CurrentStepIndex, currentStepIndex + 1);

                // 6. Redirect para recargar la página con el nuevo step (PRG pattern)
                // Esto evita que el navegador reenvíe el POST si el usuario presiona F5
                return RedirectToPage("/Start/Wizard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing form for ProspectId={ProspectId}", ProspectId);
                ErrorMessage = $"Error al procesar el formulario: {ex.Message}";
                
                // Recargar configuración para mostrar el form de nuevo
                WorkflowConfig = await _apiClient.GetWorkflowConfigurationAsync(WorkflowId);
                TotalSteps = WorkflowConfig!.Phases.Sum(p => p.Steps.Count);
                CurrentStepIndex = currentStepIndex;
                LoadCurrentStep();
                
                return Page();
            }
        }

        private void LoadCurrentStep()
        {
            if (WorkflowConfig == null) return;

            int stepCount = 0;
            foreach (var phase in WorkflowConfig.Phases.OrderBy(p => p.Order))
            {
                foreach (var step in phase.Steps.OrderBy(s => s.Order))
                {
                    if (stepCount == CurrentStepIndex)
                    {
                        CurrentStep = step;
                        CurrentPhase = phase;
                        _logger.LogDebug(
                            "Loaded step: PhaseId={PhaseId}, StepId={StepId}, StepName={StepName}",
                            phase.PhaseId, step.StepId, step.Name);
                        return;
                    }
                    stepCount++;
                }
            }
        }
    }
}
