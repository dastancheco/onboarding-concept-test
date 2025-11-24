using Microsoft.Extensions.Logging;
using Onboarding.Core.Interfaces;
using System;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Onboarding.Core.Validators
{
    /// <summary>
    /// Validador de RFC mexicano (Registro Federal de Contribuyentes).
    /// Soporta RFC de Persona Física (13 caracteres) y Persona Moral (12 caracteres).
    /// </summary>
    public class RfcValidator : IFieldValidator
    {
        public string ValidatorType => "RFC";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // RFC Persona Física: 13 caracteres (ej: HEGG560427XXX)
            // RFC Persona Moral: 12 caracteres (ej: ABC123456XXX)
            var pattern = @"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$";
            return Regex.IsMatch(value.Trim().ToUpperInvariant(), pattern);
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            return $"El campo '{fieldKey}' debe ser un RFC válido (12 o 13 caracteres).";
        }
    }

    /// <summary>
    /// Validador de CURP (Clave Única de Registro de Población).
    /// 18 caracteres con formato específico.
    /// </summary>
    public class CurpValidator : IFieldValidator
    {
        public string ValidatorType => "CURP";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // CURP: 18 caracteres
            // Formato: 4 letras + 6 dígitos + H/M + 2 letras + 3 caracteres + 1 alfanumérico + 1 dígito
            var pattern = @"^[A-Z]{4}\d{6}[HM][A-Z]{5}[0-9A-Z]\d$";
            return Regex.IsMatch(value.Trim().ToUpperInvariant(), pattern);
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            return $"El campo '{fieldKey}' debe ser una CURP válida (18 caracteres).";
        }
    }

    /// <summary>
    /// Validador de email usando MailAddress de .NET.
    /// </summary>
    public class EmailValidator : IFieldValidator
    {
        public string ValidatorType => "EMAIL";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                var addr = new MailAddress(value.Trim());
                return addr.Address == value.Trim();
            }
            catch
            {
                return false;
            }
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            return $"El campo '{fieldKey}' debe ser un email válido.";
        }
    }

    /// <summary>
    /// Validador de teléfono mexicano.
    /// Acepta formatos: 10 dígitos, con código de área, con +52, etc.
    /// </summary>
    public class PhoneValidator : IFieldValidator
    {
        public string ValidatorType => "PHONE";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Remover caracteres comunes de formato
            var cleaned = Regex.Replace(value, @"[\s\-\(\)\+]", "");

            // Aceptar 10 dígitos (teléfono local) o 12 dígitos (con +52)
            if (cleaned.StartsWith("52") && cleaned.Length == 12)
                cleaned = cleaned.Substring(2); // Remover código de país

            return cleaned.Length == 10 && Regex.IsMatch(cleaned, @"^\d{10}$");
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            return $"El campo '{fieldKey}' debe ser un teléfono válido (10 dígitos).";
        }
    }

    /// <summary>
    /// Validador de código postal mexicano (5 dígitos).
    /// </summary>
    public class PostalCodeValidator : IFieldValidator
    {
        public string ValidatorType => "POSTAL_CODE";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Regex.IsMatch(value.Trim(), @"^\d{5}$");
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            return $"El campo '{fieldKey}' debe ser un código postal válido (5 dígitos).";
        }
    }

    /// <summary>
    /// Validador de URL.
    /// </summary>
    public class UrlValidator : IFieldValidator
    {
        public string ValidatorType => "URL";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            return $"El campo '{fieldKey}' debe ser una URL válida (http/https).";
        }
    }

    /// <summary>
    /// Validador de fecha.
    /// Soporta formato ISO 8601 y otros formatos comunes.
    /// </summary>
    public class DateValidator : IFieldValidator
    {
        public string ValidatorType => "DATE";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return DateTime.TryParse(value.Trim(), out _);
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            return $"El campo '{fieldKey}' debe ser una fecha válida.";
        }
    }

    /// <summary>
    /// Validador genérico de regex personalizado.
    /// Lee el patrón desde la configuración.
    /// </summary>
    public class RegexValidator : IFieldValidator
    {
        private readonly ILogger<RegexValidator> _logger;

        public RegexValidator(ILogger<RegexValidator> logger)
        {
            _logger = logger;
        }

        public string ValidatorType => "REGEX";

        public bool IsValid(string? value, object? config = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (config is not string pattern || string.IsNullOrWhiteSpace(pattern))
            {
                _logger.LogWarning("REGEX validator requires a pattern in config");
                return false;
            }

            try
            {
                return Regex.IsMatch(value.Trim(), pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid regex pattern: {Pattern}", pattern);
                return false;
            }
        }

        public string GetErrorMessage(string fieldKey, object? config = null)
        {
            return $"El campo '{fieldKey}' no cumple con el formato requerido.";
        }
    }
}
