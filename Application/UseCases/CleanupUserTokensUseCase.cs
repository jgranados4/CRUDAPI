using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using NuGet.Configuration;
using System.Runtime;
using System.Text.RegularExpressions;

namespace CRUDAPI.Application.UseCases
{
    public class CleanupUserTokensUseCase
    {
        private const int MAX_TOKENS_PER_USER = 5;
        private const int CLEANUP_DAYS = 30;
        private readonly IRefreshTokenRepository _repository;
        private readonly IRefreshTokenService _tokenService;
        private readonly ILogger<RevokeAllUserTokensUseCase> _logger;
        public CleanupUserTokensUseCase(IRefreshTokenRepository refreshTokenRepository, IRefreshTokenService refreshTokenService, ILogger<RevokeAllUserTokensUseCase> logger)
        {
            _repository = refreshTokenRepository;
            _tokenService = refreshTokenService;
            _logger = logger;
        }
        public async Task ExecuteAsync(int userId)
        {
            _logger.LogInformation("Iniciando limpieza de tokens para usuario {UserId}", userId);
            try
            {
                //Limpiar tokens revocados vencidos(usando una regla de negocio)
                await CleanupExpiredRevokedTokensAsync(userId);

                // Limitar tokens activos (usando una regla de negocio)
                await LimitActiveTokensAsync(userId);

                _logger.LogInformation("Limpieza de tokens completada para usuario {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante limpieza de tokens para usuario {UserId}", userId);
                throw;
            }
        }
        // ✅ Private methods handle specific business workflows
        private async Task CleanupExpiredRevokedTokensAsync(int userId)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-CLEANUP_DAYS);
            var expiredTokens = await _repository.GetExpiredRevokedTokensAsync(userId, cutoffDate);

            if (expiredTokens.Any())
            {
                await _repository.RemoveRangeAsync(expiredTokens);
                _logger.LogInformation("Eliminados {Count} tokens expirados para usuario {UserId}",
                    expiredTokens.Count, userId);
            }
        }

        private async Task LimitActiveTokensAsync(int userId)
        {
            var maxTokens = _tokenService.GetMaxTokensPerUser();
            var activeCount = await _repository.CountActiveTokensByUserAsync(userId);

            if (activeCount >= maxTokens)
            {
                var tokensToRemove = activeCount - maxTokens + 1;
                var oldestTokens = await _repository.GetOldestActiveTokensAsync(userId, tokensToRemove);

                await _repository.RemoveRangeAsync(oldestTokens);
                _logger.LogInformation("Eliminados {Count} tokens antiguos para usuario {UserId}",
                    oldestTokens.Count, userId);
            }
        }
    }
}
