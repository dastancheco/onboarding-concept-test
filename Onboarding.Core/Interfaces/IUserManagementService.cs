using System;
using System.Threading.Tasks;
using OvexDataModelingTest.Entities.App;

namespace Onboarding.Core.Interfaces
{
    /// <summary>
    /// Servicio para gestionar usuarios y evitar duplicados.
    /// </summary>
    public interface IUserManagementService
    {
        /// <summary>
        /// Obtiene un usuario existente o crea uno nuevo si no existe.
        /// Usa el email como identificador único para deduplicación.
        /// </summary>
        /// <param name="email">Email del usuario</param>
        /// <param name="payloadJson">JSON con datos adicionales del usuario (opcional)</param>
        /// <returns>Usuario existente o recién creado</returns>
        Task<User> GetOrCreateUserAsync(string email, string? payloadJson = null);

        /// <summary>
        /// Busca un usuario por email.
        /// </summary>
        /// <param name="email">Email a buscar</param>
        /// <returns>Usuario encontrado, o null si no existe</returns>
        Task<User?> FindUserByEmailAsync(string email);

        /// <summary>
        /// Busca un usuario por ID.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <returns>Usuario encontrado, o null si no existe</returns>
        Task<User?> GetUserByIdAsync(Guid userId);

        /// <summary>
        /// Extrae el email del payload JSON.
        /// Busca en diferentes campos comunes: email, user_email, correo.
        /// </summary>
        /// <param name="payloadJson">JSON del evento</param>
        /// <returns>Email extraído, o null si no se encuentra</returns>
        string? ExtractEmailFromPayload(string payloadJson);
    }
}
