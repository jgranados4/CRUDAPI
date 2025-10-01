using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;

namespace CRUDAPI.Domain.Services
{
    public interface IRefreshTokenService
    {
        
        string GenerateRefreshToken();
        bool IsTokenValid(RefreshToken token);
        bool IsTokenExpired(RefreshToken token);

        // ✅ Domain-specific validation
        TimeSpan GetTokenLifetime();
        int GetMaxTokensPerUser();

    }
}