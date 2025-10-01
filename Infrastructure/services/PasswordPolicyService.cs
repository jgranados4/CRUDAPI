using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;

namespace CRUDAPI.Infrastructure.services
{
    public class PasswordPolicyService: IPasswordPolicyService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPasswordHashingService _hashingService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PasswordPolicyService> _logger;

        public PasswordPolicyService(
            IUsuarioRepository usuarioRepository,
            IPasswordHashingService hashingService,
            IConfiguration configuration,
            ILogger<PasswordPolicyService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _hashingService = hashingService;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task ValidatePasswordStrengthAsync(string password)
        {
            // Valida longitud mínima según políticas empresariales
            var minLength = _configuration.GetValue<int>("PasswordPolicy:MinLength", 8);
            if (password.Length < minLength)
            {
                throw new ArgumentException($"La contraseña debe tener al menos {minLength} caracteres");
            }

            // Valida complejidad de caracteres requerida
            var requiresUpperCase = _configuration.GetValue<bool>("PasswordPolicy:RequireUpperCase", true);
            var requiresLowerCase = _configuration.GetValue<bool>("PasswordPolicy:RequireLowerCase", true);
            var requiresDigit = _configuration.GetValue<bool>("PasswordPolicy:RequireDigit", true);
            var requiresSpecialChar = _configuration.GetValue<bool>("PasswordPolicy:RequireSpecialChar", true);

            if (requiresUpperCase && !password.Any(char.IsUpper))
                throw new ArgumentException("La contraseña debe contener al menos una letra mayúscula");

            if (requiresLowerCase && !password.Any(char.IsLower))
                throw new ArgumentException("La contraseña debe contener al menos una letra minúscula");

            if (requiresDigit && !password.Any(char.IsDigit))
                throw new ArgumentException("La contraseña debe contener al menos un número");

            if (requiresSpecialChar && !password.Any(ch => "!@#$%^&*(),.?\":{}|<>".Contains(ch)))
                throw new ArgumentException("La contraseña debe contener al menos un carácter especial");
        }

        public async Task ValidatePasswordHistoryAsync(int userId, string newPassword)
        {
            // En implementación completa, verificaría contra historial de contraseñas
            // Por simplicidad, validamos que no sea igual a la actual
            var user = await _usuarioRepository.GetByIdAsync(userId);
            if (user != null && _hashingService.VerificarClave(newPassword, user.Constrasena))
            {
                throw new ArgumentException("La nueva contraseña no puede ser igual a la contraseña actual");
            }
        }

        public bool IsPasswordRecentlyUsed(string password, IEnumerable<string> previousPasswords)
        {
            // Implementación simplificada - en sistema completo verificaría historial
            return previousPasswords.Any(prev => _hashingService.VerificarClave(password, prev));
        }

        public async Task<TimeSpan> GetPasswordExpirationTimeAsync(int userId)
        {
            var expirationDays = _configuration.GetValue<int>("PasswordPolicy:ExpirationDays", 90);
            return TimeSpan.FromDays(expirationDays);
        }

        public bool RequiresPasswordChange(DateTime lastPasswordChange)
        {
            var maxAge = _configuration.GetValue<int>("PasswordPolicy:MaxAgeDays", 90);
            return DateTime.UtcNow.Subtract(lastPasswordChange).TotalDays > maxAge;
        }

        public async Task LogPasswordChangeAttemptAsync(int userId, bool wasSuccessful, string? failureReason = null)
        {
            if (wasSuccessful)
            {
                _logger.LogInformation("Cambio de contraseña exitoso para usuario: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Intento fallido de cambio de contraseña para usuario {UserId}: {Reason}",
                    userId, failureReason);
            }
        }
    }
}
