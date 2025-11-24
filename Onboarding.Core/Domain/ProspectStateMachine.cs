using System;
using System.Collections.Generic;
using System.Linq;

namespace Onboarding.Core.Domain
{
    /// <summary>
    /// Máquina de estados para validar transiciones de estado de prospectos.
    /// Implementa el patrón State Machine para garantizar flujos válidos.
    /// </summary>
    public static class ProspectStateMachine
    {
        /// <summary>
        /// Estados disponibles en el sistema
        /// </summary>
        public static class States
        {
            public const string Started = "STARTED";
            public const string InProgress = "IN_PROGRESS";
            public const string PendingReview = "PENDING_REVIEW";
            public const string InReview = "IN_REVIEW";
            public const string MoreInfoRequired = "MORE_INFO_REQUIRED";
            public const string Approved = "APPROVED";
            public const string Rejected = "REJECTED";
            public const string Cancelled = "CANCELLED";
            public const string Failed = "FAILED";
            public const string Finalized = "FINALIZED";
        }

        /// <summary>
        /// Define todas las transiciones válidas del sistema.
        /// Clave: Estado actual, Valor: Lista de estados válidos a los que puede transicionar
        /// </summary>
        private static readonly Dictionary<string, List<string>> AllowedTransitions = new()
        {
            { 
                States.Started, 
                new List<string> { States.InProgress, States.Cancelled } 
            },
            { 
                States.InProgress, 
                new List<string> { States.PendingReview, States.Cancelled, States.Failed, States.MoreInfoRequired } 
            },
            { 
                States.PendingReview, 
                new List<string> { States.InReview, States.Cancelled, States.MoreInfoRequired } 
            },
            { 
                States.InReview, 
                new List<string> { States.Approved, States.Rejected, States.MoreInfoRequired } 
            },
            { 
                States.MoreInfoRequired, 
                new List<string> { States.InProgress, States.Cancelled } 
            },
            { 
                States.Approved, 
                new List<string> { States.Finalized } 
            },
            { 
                States.Rejected, 
                new List<string>() // Estado terminal
            },
            { 
                States.Cancelled, 
                new List<string>() // Estado terminal
            },
            { 
                States.Failed, 
                new List<string> { States.InProgress } // Permitir reintento
            },
            { 
                States.Finalized, 
                new List<string>() // Estado terminal
            }
        };

        /// <summary>
        /// Verifica si una transición de estado es válida.
        /// </summary>
        /// <param name="currentStatus">Estado actual</param>
        /// <param name="newStatus">Estado al que se quiere transicionar</param>
        /// <returns>True si la transición es válida</returns>
        public static bool CanTransition(string currentStatus, string newStatus)
        {
            if (string.IsNullOrWhiteSpace(currentStatus) || string.IsNullOrWhiteSpace(newStatus))
                return false;

            if (!AllowedTransitions.ContainsKey(currentStatus))
                return false;

            return AllowedTransitions[currentStatus].Contains(newStatus);
        }

        /// <summary>
        /// Valida una transición y lanza excepción si es inválida.
        /// </summary>
        /// <param name="prospectId">ID del prospecto</param>
        /// <param name="currentStatus">Estado actual</param>
        /// <param name="newStatus">Estado deseado</param>
        /// <exception cref="InvalidOperationException">Si la transición no es válida</exception>
        public static void ValidateTransition(Guid prospectId, string currentStatus, string newStatus)
        {
            if (!CanTransition(currentStatus, newStatus))
            {
                throw new InvalidOperationException(
                    $"Transición de estado inválida para Prospecto {prospectId}: " +
                    $"No se puede cambiar de '{currentStatus}' a '{newStatus}'. " +
                    $"Transiciones permitidas desde '{currentStatus}': {GetAllowedTransitionsText(currentStatus)}");
            }
        }

        /// <summary>
        /// Obtiene todas las transiciones válidas desde un estado.
        /// </summary>
        /// <param name="currentStatus">Estado actual</param>
        /// <returns>Lista de estados válidos</returns>
        public static List<string> GetAllowedTransitions(string currentStatus)
        {
            if (string.IsNullOrWhiteSpace(currentStatus) || !AllowedTransitions.ContainsKey(currentStatus))
                return new List<string>();

            return AllowedTransitions[currentStatus];
        }

        /// <summary>
        /// Obtiene un texto descriptivo de las transiciones permitidas.
        /// </summary>
        private static string GetAllowedTransitionsText(string currentStatus)
        {
            var allowed = GetAllowedTransitions(currentStatus);
            return allowed.Any() ? string.Join(", ", allowed) : "Ninguna (estado terminal)";
        }

        /// <summary>
        /// Verifica si un estado es terminal (no permite más transiciones).
        /// </summary>
        public static bool IsTerminalState(string status)
        {
            if (string.IsNullOrWhiteSpace(status) || !AllowedTransitions.ContainsKey(status))
                return false;

            return !AllowedTransitions[status].Any();
        }

        /// <summary>
        /// Obtiene todos los estados disponibles en el sistema.
        /// </summary>
        public static List<string> GetAllStates()
        {
            return new List<string>
            {
                States.Started,
                States.InProgress,
                States.PendingReview,
                States.InReview,
                States.MoreInfoRequired,
                States.Approved,
                States.Rejected,
                States.Cancelled,
                States.Failed,
                States.Finalized
            };
        }
    }
}
