using CRUDAPI.Domain.Repositories;
using NuGet.Protocol.Core.Types;

namespace CRUDAPI.Application.UseCases
{
    public class RevokeAllUserTokensUseCase
    {
        private readonly IRefreshTokenRepository _repository;
        private readonly ILogger<RevokeAllUserTokensUseCase> _logger;
        public RevokeAllUserTokensUseCase(IRefreshTokenRepository refresh, ILogger<RevokeAllUserTokensUseCase> logger)
        {
            _repository = refresh;
            _logger = logger;
        }
        public async Task<int> ExecuteAsync(int userId)
        {
            try
            {
                var activeTokens = await _repository.GetActiveTokensByUserAsync(userId);

                if (!activeTokens.Any())
                {
                    _logger.LogInformation("No hay tokens activos para revocar del usuario {UserId}", userId);
                    return 0;
                }

                // Aplicar regla de negocio: marcar como revocado con marca de tiempo
                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }
                // Actualización masiva para mejorar el rendimiento
                foreach (var token in activeTokens)
                {
                    await _repository.UpdateAsync(token);
                }
                _logger.LogInformation("Se revocaron {Count} tokens del usuario {UserId}",
                    activeTokens.Count, userId);

                return activeTokens.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revocando tokens del usuario {UserId}", userId);
                throw;
            }
        }

    }
}
