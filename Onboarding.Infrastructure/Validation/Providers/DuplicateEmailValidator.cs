using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using Onboarding.Core.Validation;
using OvexDataModelingTest.Entities.App;
using System.Text.Json;

namespace Onboarding.Infrastructure.Validation.Providers
{
    /// <summary>
    /// Validador para detectar emails duplicados y retornar información del prospecto existente
    /// </summary>
    public class DuplicateEmailValidator : IValidationProvider
    {
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<Prospect> _prospectRepo;
        private readonly ILogger<DuplicateEmailValidator> _logger;

        private DuplicateEmailConfig _config = new();

        public string ProviderKey => "CHECK_DUPLICATE_EMAIL";
        public string ProviderType => "INTERNAL_CODE";

        public DuplicateEmailValidator(
            IRepository<User> userRepo,
            IRepository<Prospect> prospectRepo,
            ILogger<DuplicateEmailValidator> logger)
        {
            _userRepo = userRepo;
            _prospectRepo = prospectRepo;
            _logger = logger;
        }

        public void Configure(string? configJson)
        {
            if (!string.IsNullOrWhiteSpace(configJson))
            {
                try
                {
                    _config = JsonSerializer.Deserialize<DuplicateEmailConfig>(configJson)
                        ?? new DuplicateEmailConfig();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error deserializing config. Using defaults.");
                }
            }
        }

        public async Task<ValidationStepResult> ValidateAsync(ValidationContext context)
        {
            var result = new ValidationStepResult
            {
                ProviderKey = ProviderKey,
                IsValid = true
            };

            var email = context.Email;

            if (string.IsNullOrWhiteSpace(email))
            {
                result.IsValid = false;
                result.Severity = ValidationSeverity.Error;
                result.Message = "Email is required";
                return result;
            }

            _logger.LogInformation("Checking for duplicate email: {Email}", email);

            // 1. Buscar usuario existente
            var allUsers = await _userRepo.GetAllAsync();
            var existingUser = allUsers.FirstOrDefault(u =>
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (existingUser == null)
            {
                // No hay duplicado - OK
                result.Message = "Email is available";
                result.Severity = ValidationSeverity.Info;
                _logger.LogInformation("Email is available: {Email}", email);
                return result;
            }

            _logger.LogInformation(
                "Duplicate email found: {Email} (UserId: {UserId})",
                email, existingUser.UserId);

            // 2. Buscar prospectos del usuario
            var allProspects = await _prospectRepo.GetAllAsync();
            var userProspects = allProspects
                .Where(p => p.UserId == existingUser.UserId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            _logger.LogInformation(
                "Found {Count} prospects for user {UserId}",
                userProspects.Count, existingUser.UserId);

            // 3. Filtrar prospectos según configuración
            var relevantProspects = FilterProspects(userProspects);

            if (!relevantProspects.Any())
            {
                // Usuario existe pero sin prospectos relevantes
                result.Message = "Email exists but no active prospects found";
                result.Severity = ValidationSeverity.Warning;
                result.Data["user_exists"] = true;
                result.Data["user_id"] = existingUser.UserId.ToString();
                return result;
            }

            // 4. Obtener prospecto más reciente en el mismo workflow (si aplica)
            var recentInSameWorkflow = relevantProspects.FirstOrDefault(p =>
                p.WorkflowId == context.WorkflowId);

            // 5. Construir resultado detallado
            result.IsValid = false; // Duplicado detectado
            result.Severity = ValidationSeverity.Error;
            result.Message = BuildDuplicateMessage(
                recentInSameWorkflow ?? relevantProspects.First(),
                context.WorkflowId);

            // 6. Enriquecer datos
            result.Data["duplicate_detected"] = true;
            result.Data["user_id"] = existingUser.UserId.ToString();
            result.Data["total_prospects"] = relevantProspects.Count;
            result.Data["same_workflow"] = recentInSameWorkflow != null;

            var recentProspect = recentInSameWorkflow ?? relevantProspects.First();
            result.Data["recent_prospect_id"] = recentProspect.ProspectId.ToString();
            result.Data["recent_workflow_id"] = recentProspect.WorkflowId;
            result.Data["recent_status"] = recentProspect.Status;
            result.Data["recent_current_step"] = recentProspect.CurrentStepId ?? 0;
            result.Data["days_since_created"] = (DateTime.UtcNow - recentProspect.CreatedAt).Days;

            _logger.LogWarning(
                "Duplicate email validation failed: {Email} - Prospect: {ProspectId}, Status: {Status}",
                email, recentProspect.ProspectId, recentProspect.Status);

            return result;
        }

        private List<Prospect> FilterProspects(List<Prospect> prospects)
        {
            var filtered = prospects.AsEnumerable();

            // Filtrar prospectos inactivos si está configurado
            if (!_config.IncludeInactiveUsers)
            {
                filtered = filtered.Where(p =>
                    p.Status != "INACTIVE" &&
                    p.Status != "CANCELLED" &&
                    p.Status != "REJECTED");
            }

            // Filtrar prospectos expirados
            if (!_config.IncludeExpiredProspects && _config.MaxDaysBack > 0)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-_config.MaxDaysBack);
                filtered = filtered.Where(p => p.CreatedAt >= cutoffDate);
            }

            return filtered.ToList();
        }

        private string BuildDuplicateMessage(Prospect prospect, int? requestedWorkflowId)
        {
            var daysSince = (DateTime.UtcNow - prospect.CreatedAt).Days;
            var isSameWorkflow = prospect.WorkflowId == requestedWorkflowId;

            return prospect.Status switch
            {
                "IN_PROGRESS" or "STARTED" when isSameWorkflow && daysSince < 7 =>
                    $"Ya tienes un registro en progreso iniciado hace {daysSince} días. ¿Deseas continuarlo?",

                "IN_REVIEW" when isSameWorkflow =>
                    "Tu solicitud está en revisión. Por favor espera la respuesta antes de iniciar un nuevo registro.",

                "COMPLETED" =>
                    "Ya tienes un registro completado. Si deseas iniciar uno nuevo, contacta a soporte.",

                _ when !isSameWorkflow =>
                    $"Ya tienes un registro en otro producto (Workflow {prospect.WorkflowId}). Puedes continuar con este nuevo registro.",

                _ =>
                    $"Ya existe un registro con este email (Estado: {prospect.Status}). Verifica tu información."
            };
        }

        /// <summary>
        /// Configuración del validador de duplicados
        /// </summary>
        private class DuplicateEmailConfig
        {
            public bool IncludeInactiveUsers { get; set; } = false;
            public bool IncludeExpiredProspects { get; set; } = false;
            public int MaxDaysBack { get; set; } = 365;
        }
    }
}
