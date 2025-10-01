using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using System.Text.RegularExpressions;

namespace CRUDAPI.Infrastructure.services
{
    public class UsuarioValidationService : IUsuarioValidationService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<UsuarioValidationService> _logger;

        // Valid roles defined by business rules
        private static readonly HashSet<string> ValidRoles = new() { "Admin", "User", "Manager", "Guest" };

        public UsuarioValidationService(IUsuarioRepository usuarioRepository, ILogger<UsuarioValidationService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
        }

        public async Task ValidateUserCreationAsync(string email, string nombre)
        {
            await ValidateEmailFormatAsync(email);
            await ValidateUserUniquenessAsync(email);
            ValidateNombreFormat(nombre);
        }

        public async Task ValidateEmailFormatAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email es requerido");

            var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, emailRegex))
                throw new ArgumentException("Formato de email inválido");

            _logger.LogDebug("Email format validated successfully: {Email}", email);
        }

        public async Task ValidatePasswordRequirementsAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña es requerida");

            if (password.Length < 8)
                throw new ArgumentException("La contraseña debe tener al menos 8 caracteres");

            // Business rule: Password complexity requirements
            var hasUpperCase = password.Any(char.IsUpper);
            var hasLowerCase = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecialChar = password.Any(ch => "!@#$%^&*(),.?\":{}|<>".Contains(ch));

            if (!hasUpperCase || !hasLowerCase || !hasDigit || !hasSpecialChar)
            {
                throw new ArgumentException("La contraseña debe contener al menos: 1 mayúscula, 1 minúscula, 1 número y 1 carácter especial");
            }

            _logger.LogDebug("Password requirements validated successfully");
        }

        public bool IsValidRole(string rol)
        {
            return !string.IsNullOrWhiteSpace(rol) && ValidRoles.Contains(rol);
        }

        public async Task ValidateUserUniquenessAsync(string email)
        {
            var exists = await _usuarioRepository.ExistsByEmailAsync(email);
            if (exists)
                throw new ArgumentException($"Ya existe un usuario con el email: {email}");

            _logger.LogDebug("User uniqueness validated successfully: {Email}", email);
        }

        private static void ValidateNombreFormat(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre es requerido");

            if (nombre.Length < 2)
                throw new ArgumentException("El nombre debe tener al menos 2 caracteres");

            if (nombre.Length > 255)
                throw new ArgumentException("El nombre no puede exceder 255 caracteres");
        }
    }
}
