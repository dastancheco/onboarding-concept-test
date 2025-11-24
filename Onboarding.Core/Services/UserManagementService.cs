using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using OvexDataModelingTest.Entities.App;
using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Onboarding.Core.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IRepository<User> _userRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            IRepository<User> userRepo,
            IUnitOfWork unitOfWork,
            ILogger<UserManagementService> logger)
        {
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<User> GetOrCreateUserAsync(string email, string? payloadJson = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            // Normalizar email (lowercase y trim)
            email = email.Trim().ToLowerInvariant();

            _logger.LogInformation("Looking for user with email: {Email}", email);

            // 1. Buscar usuario existente
            var existingUser = await FindUserByEmailAsync(email);

            if (existingUser != null)
            {
                _logger.LogInformation("User found with UserId: {UserId}", existingUser.UserId);
                return existingUser;
            }

            // 2. Crear nuevo usuario
            _logger.LogInformation("Creating new user with email: {Email}", email);

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("User created successfully. UserId: {UserId}", newUser.UserId);

            return newUser;
        }

        public async Task<User?> FindUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            // Normalizar email
            email = email.Trim().ToLowerInvariant();

            var users = await _userRepo.FindAsync(u => u.Email.ToLower() == email);
            return users.FirstOrDefault();
        }

        public async Task<User?> GetUserByIdAsync(Guid userId)
        {
            return await _userRepo.GetByIdAsync(userId);
        }

        public string? ExtractEmailFromPayload(string payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                _logger.LogWarning("PayloadJson is null or empty");
                return null;
            }

            try
            {
                var jsonNode = JsonNode.Parse(payloadJson);
                if (jsonNode == null)
                {
                    _logger.LogWarning("Failed to parse payloadJson");
                    return null;
                }

                // Buscar email en diferentes campos comunes
                string?[] possibleEmailFields = new[]
                {
                    "email",
                    "user_email",
                    "correo",
                    "userEmail",
                    "Email",
                    "mail"
                };

                foreach (var field in possibleEmailFields)
                {
                    var value = jsonNode[field]?.ToString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        _logger.LogDebug("Email extracted from field: {Field}", field);
                        return value.Trim();
                    }
                }

                _logger.LogWarning("No email field found in payload. Tried fields: {Fields}", 
                    string.Join(", ", possibleEmailFields));

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting email from payload");
                return null;
            }
        }
    }
}
