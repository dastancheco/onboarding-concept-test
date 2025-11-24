using System;

namespace Onboarding.Core.Interfaces
{
    /// <summary>
    /// Interfaz para validadores de campos específicos.
    /// Aplica Strategy Pattern + OCP: Nuevos validadores sin modificar código existente.
    /// </summary>
    public interface IFieldValidator
    {
        /// <summary>
        /// Tipo de validador (RFC, CURP, EMAIL, PHONE, etc.)
        /// </summary>
        string ValidatorType { get; }

        /// <summary>
        /// Valida un valor según las reglas del validador.
        /// </summary>
        /// <param name="value">Valor a validar</param>
        /// <param name="config">Configuración adicional del validador (opcional)</param>
        /// <returns>True si el valor es válido</returns>
        bool IsValid(string? value, object? config = null);

        /// <summary>
        /// Obtiene el mensaje de error personalizado.
        /// </summary>
        /// <param name="fieldKey">Nombre del campo</param>
        /// <param name="config">Configuración adicional (opcional)</param>
        /// <returns>Mensaje de error descriptivo</returns>
        string GetErrorMessage(string fieldKey, object? config = null);
    }
}
