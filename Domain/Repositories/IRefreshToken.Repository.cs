

using CRUDAPI.Domain.entities;

namespace CRUDAPI.Domain.Repositories
{
    public interface IRefreshTokenRepository
    {
        // ✅ Solo operaciones CRUD básicas
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<RefreshToken?> GetByIdAsync(int id);
        Task<List<RefreshToken>> GetActiveTokensByUserAsync(int userId);
        Task<List<RefreshToken>> GetExpiredRevokedTokensAsync(int userId, DateTime cutoffDate);
        Task SaveAsync(RefreshToken refreshToken);
        Task UpdateAsync(RefreshToken refreshToken);
        Task RemoveRangeAsync(IEnumerable<RefreshToken> tokens);

        // ✅ Consultas específicas para operaciones comerciales
        Task<int> CountActiveTokensByUserAsync(int userId);
        Task<List<RefreshToken>> GetOldestActiveTokensAsync(int userId, int count);
        // ✅ Operaciones empresariales específicas - AGREGADAS
        Task<int> RevokeAllUserTokensAsync(int userId);
        Task CleanupUserTokensAsync(int userId);
        Task CleanupExpiredRevokedTokensAsync(int userId, int cleanupDays);
        Task LimitActiveTokensAsync(int userId, int maxTokens);
        Task<RefreshToken?> GetValidTokenByUserIdAsync(int userId);
    }
}
