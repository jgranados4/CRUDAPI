using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;

namespace CRUDAPI.Infrastructure.services
{
    public class UsuarioDeletionService: IUsuarioDeletionService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<UsuarioDeletionService> _logger;

        public UsuarioDeletionService(
            IUsuarioRepository usuarioRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ILogger<UsuarioDeletionService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
        }

        public async Task ValidateDeletionPermissionsAsync(int userId, string currentUserRole)
        {
            // Implementa reglas de negocio específicas sobre quién puede eliminar usuarios
            if (currentUserRole != "Admin")
            {
                throw new UnauthorizedAccessException("Solo los administradores pueden eliminar usuarios");
            }
        }

        public async Task ValidateUserCanBeDeletedAsync(int userId)
        {
            // Previene la eliminación de usuarios críticos del sistema
            var user = await _usuarioRepository.GetByIdAsync(userId);
            if (user?.Rol == "Admin")
            {
                var adminCount = (await _usuarioRepository.GetByRolAsync("Admin")).Count();
                if (adminCount <= 1)
                {
                    throw new InvalidOperationException("No se puede eliminar el último administrador del sistema");
                }
            }
        }

        public async Task<bool> HasPendingDependenciesAsync(int userId)
        {
            // Verifica si existen dependencias que impiden la eliminación
            var activeTokens = await _refreshTokenRepository.GetActiveTokensByUserAsync(userId);
            return activeTokens.Any();
        }

        public async Task HandlePreDeletionCleanupAsync(int userId)
        {
            // Limpia datos relacionados antes de eliminar el usuario principal
            await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);

            _logger.LogInformation("Pre-eliminación completada para usuario: {UserId}", userId);
        }

    }
}
