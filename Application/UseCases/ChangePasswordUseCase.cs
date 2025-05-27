using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using CRUDAPI.Infrastructure.services;

namespace CRUDAPI.Application.UseCases
{
    public class ChangePasswordUseCase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly IPasswordPolicyService _passwordPolicyService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IHttpContextService _httpContextService;
        private readonly ILogger<ChangePasswordUseCase> _logger;

        public ChangePasswordUseCase(
            IUsuarioRepository usuarioRepository,
            IPasswordHashingService passwordHashingService,
            IPasswordPolicyService passwordPolicyService,
            IRefreshTokenRepository refreshTokenRepository,
            IHttpContextService httpContextService,
            ILogger<ChangePasswordUseCase> logger)
        {
            _usuarioRepository = usuarioRepository;
            _passwordHashingService = passwordHashingService;
            _passwordPolicyService = passwordPolicyService;
            _refreshTokenRepository = refreshTokenRepository;
            _httpContextService = httpContextService;
            _logger = logger;
        }
        public async Task<ChangePasswordResponseDTO> ExecuteAsync(int userId, ChangePasswordRequestDTO request)
        {
            // Registra inicio de operación crítica de seguridad
            // Los cambios de contraseña requieren trazabilidad completa para auditoría
            _logger.LogInformation("Iniciando cambio de contraseña para usuario: {UserId}", userId);

            try
            {
                // Valida que el ID sea empresarialmente válido
                // Previene operaciones contra identificadores malformados
                if (userId <= 0)
                {
                    throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(userId));
                }

                // Valida que el usuario esté autenticado y autorizado para esta operación
                // Solo permite cambios por el propio usuario o administradores
                await ValidateUserAuthorizationAsync(userId);

                // Obtiene la entidad del usuario para validaciones de contraseña actual
                var user = await _usuarioRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Intento de cambio de contraseña para usuario inexistente: {UserId}", userId);
                    throw new KeyNotFoundException($"Usuario con ID {userId} no encontrado");
                }

                // Ejecuta todas las validaciones de seguridad antes de proceder
                // Garantiza que el cambio cumpla con todas las políticas empresariales
                await ExecuteSecurityValidationsAsync(user, request);

                // Genera el hash seguro de la nueva contraseña
                // Utiliza el servicio de dominio para mantener consistencia criptográfica
                var newPasswordHash = _passwordHashingService.EncriptarClave(request.NewPassword);

                // Actualiza la contraseña en la entidad del usuario
                user.Constrasena = newPasswordHash;
                // En implementación completa, se actualizaría también LastPasswordChange

                // Persiste los cambios a través del repositorio
                await _usuarioRepository.UpdateAsync(user);

                // Invalida todas las sesiones activas por seguridad
                // Fuerza re-autenticación en todos los dispositivos
                await InvalidateAllUserSessionsAsync(userId);

                // Registra el cambio exitoso para auditoría
                await _passwordPolicyService.LogPasswordChangeAttemptAsync(userId, true);

                // Construye respuesta de confirmación
                var response = CreateSuccessResponse();

                _logger.LogInformation("Cambio de contraseña completado exitosamente para usuario: {UserId}", userId);
                return response;
            }
            catch (ArgumentException ex)
            {
                // Registra errores de validación para análisis de patrones
                await _passwordPolicyService.LogPasswordChangeAttemptAsync(userId, false, ex.Message);
                _logger.LogWarning("Validación fallida en cambio de contraseña {UserId}: {Message}", userId, ex.Message);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                // Los errores de autorización son críticos para detección de intrusiones
                await _passwordPolicyService.LogPasswordChangeAttemptAsync(userId, false, "Acceso no autorizado");
                _logger.LogError("Acceso no autorizado en cambio de contraseña {UserId}: {Message}", userId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Errores técnicos en operaciones de seguridad requieren atención inmediata
                await _passwordPolicyService.LogPasswordChangeAttemptAsync(userId, false, "Error técnico");
                _logger.LogError(ex, "Error crítico en cambio de contraseña para usuario {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Cambio de contraseña administrativo con validaciones adicionales
        /// Permite a administradores cambiar contraseñas de otros usuarios
        /// </summary>
        public async Task<ChangePasswordResponseDTO> ExecuteAdministrativeChangeAsync(
            int targetUserId,
            string newPassword,
            string adminReason)
        {
            // Inicia operación administrativa con logging específico
            // Las operaciones administrativas requieren justificación documentada
            _logger.LogWarning("Iniciando cambio administrativo de contraseña para usuario: {UserId}, Razón: {Reason}",
                targetUserId, adminReason);

            try
            {
                // Valida que el usuario actual tenga permisos administrativos
                var currentUserRole = _httpContextService.GetCurrentUserRole();
                if (currentUserRole != "Admin")
                {
                    throw new UnauthorizedAccessException("Solo administradores pueden ejecutar cambios administrativos");
                }

                // Obtiene el usuario objetivo
                var targetUser = await _usuarioRepository.GetByIdAsync(targetUserId);
                if (targetUser == null)
                {
                    throw new KeyNotFoundException($"Usuario objetivo con ID {targetUserId} no encontrado");
                }

                // Aplica validaciones de política a la nueva contraseña
                // Mismo estándar de seguridad que cambios regulares
                await _passwordPolicyService.ValidatePasswordStrengthAsync(newPassword);

                // Genera hash de la nueva contraseña
                var newPasswordHash = _passwordHashingService.EncriptarClave(newPassword);

                // Actualiza la contraseña del usuario objetivo
                targetUser.Constrasena = newPasswordHash;
                await _usuarioRepository.UpdateAsync(targetUser);

                // Invalida todas las sesiones del usuario objetivo
                await InvalidateAllUserSessionsAsync(targetUserId);

                // Registra la operación administrativa
                var currentUserId = _httpContextService.GetCurrentUserId();
                _logger.LogWarning("Cambio administrativo ejecutado por usuario {AdminId} para usuario {TargetId}",
                    currentUserId, targetUserId);

                await _passwordPolicyService.LogPasswordChangeAttemptAsync(targetUserId, true,
                    $"Cambio administrativo: {adminReason}");

                return CreateSuccessResponse(isAdministrativeChange: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en cambio administrativo para usuario {UserId}", targetUserId);
                throw;
            }
        }

        private async Task ValidateUserAuthorizationAsync(int userId)
        {
            // Verifica que el usuario esté autenticado
            if (!_httpContextService.IsUserAuthenticated())
            {
                throw new UnauthorizedAccessException("Usuario no autenticado");
            }

            // Obtiene el ID del usuario actual desde el contexto
            var currentUserId = _httpContextService.GetCurrentUserId();
            var currentUserRole = _httpContextService.GetCurrentUserRole();

            // Permite el cambio si es el propio usuario o si es administrador
            if (currentUserId != userId && currentUserRole != "Admin")
            {
                throw new UnauthorizedAccessException("Solo puedes cambiar tu propia contraseña");
            }
        }

        private async Task ExecuteSecurityValidationsAsync(UsuarioAU user, ChangePasswordRequestDTO request)
        {
            // Verifica que la contraseña actual sea correcta
            // Medida fundamental para prevenir cambios no autorizados
            var isCurrentPasswordValid = _passwordHashingService.VerificarClave(request.CurrentPassword, user.Constrasena);
            if (!isCurrentPasswordValid)
            {
                throw new UnauthorizedAccessException("La contraseña actual es incorrecta");
            }

            // Valida que las contraseñas nuevas coincidan
            // Previene errores de tipeo que podrían bloquear al usuario
            if (request.NewPassword != request.ConfirmPassword)
            {
                throw new ArgumentException("La nueva contraseña y su confirmación no coinciden");
            }

            // Aplica políticas empresariales de fortaleza de contraseña
            await _passwordPolicyService.ValidatePasswordStrengthAsync(request.NewPassword);

            // Verifica que la nueva contraseña no sea igual a la actual
            await _passwordPolicyService.ValidatePasswordHistoryAsync(user.Id, request.NewPassword);
        }

        private async Task InvalidateAllUserSessionsAsync(int userId)
        {
            try
            {
                // Revoca todos los tokens activos para forzar re-autenticación
                // Medida de seguridad crucial después de cambios de credenciales
                var revokedCount = await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);

                _logger.LogInformation("Invalidadas {Count} sesiones activas para usuario: {UserId}",
                    revokedCount, userId);
            }
            catch (Exception ex)
            {
                // Error en invalidación no debe impedir el cambio de contraseña
                // Pero debe registrarse para seguimiento de seguridad
                _logger.LogWarning(ex, "Error invalidando sesiones para usuario {UserId} - Contraseña cambiada exitosamente",
                    userId);
            }
        }

        private static ChangePasswordResponseDTO CreateSuccessResponse(bool isAdministrativeChange = false)
        {
            // Construye respuesta de confirmación con metadatos útiles
            return new ChangePasswordResponseDTO
            {
                IsSuccessful = true,
                ChangedAt = DateTime.UtcNow,
                Message = isAdministrativeChange
                    ? "Contraseña cambiada exitosamente por administrador"
                    : "Contraseña cambiada exitosamente",
                ShouldLogoutOtherSessions = true // Indica al cliente que otras sesiones fueron invalidadas
            };
        }
    }
}
