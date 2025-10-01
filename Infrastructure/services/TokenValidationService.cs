using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;

namespace CRUDAPI.Infrastructure.services
{
    public class TokenValidationService: ITokenValidationService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<TokenValidationService> _logger;

        public TokenValidationService(
            IRefreshTokenRepository refreshTokenRepository,
            IUsuarioRepository usuarioRepository,
            ILogger<TokenValidationService> logger)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _usuarioRepository = usuarioRepository;
            _logger = logger;
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            // En implementación completa, consultaría una lista negra de tokens comprometidos
            // Por simplicidad, asumimos que no hay blacklist implementada
            return false;
        }

        public async Task<bool> IsTokenRevokedAsync(string token)
        {
            try
            {
                var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
                return refreshToken?.IsRevoked == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando revocación de token");
                return true; // En caso de error, asume que está revocado por seguridad
            }
        }

        public async Task<bool> IsUserActiveAsync(int userId)
        {
            try
            {
                var user = await _usuarioRepository.GetByIdAsync(userId);
                return user != null; // En implementación completa verificaría campo IsActive
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando estado de usuario: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> RequiresPasswordChangeAsync(int userId)
        {
            // En implementación completa, verificaría LastPasswordChange
            // Por simplicidad, retorna false
            return false;
        }

        public async Task LogTokenValidationAttemptAsync(string token, bool isValid, string? reason = null)
        {
            if (isValid)
            {
                _logger.LogDebug("Token validado exitosamente");
            }
            else
            {
                _logger.LogWarning("Token inválido detectado: {Reason}", reason);
            }
        }

        public async Task<Dictionary<string, object>> ExtractTokenMetadataAsync(string token)
        {
            var metadata = new Dictionary<string, object>();

            try
            {
                var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
                if (refreshToken != null)
                {
                    metadata["CreatedAt"] = refreshToken.Created;
                    metadata["CreatedByIp"] = refreshToken.CreatedByIp ?? "unknown";
                    metadata["IsRevoked"] = refreshToken.IsRevoked;
                    if (refreshToken.RevokedAt.HasValue)
                    {
                        metadata["RevokedAt"] = refreshToken.RevokedAt.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo metadata del token");
            }

            return metadata;
        }
    }
}
