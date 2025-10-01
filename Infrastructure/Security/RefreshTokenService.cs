using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Services;
using System.Security.Cryptography;

namespace CRUDAPI.Infrastructure.Security
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IConfiguration _configuration;
        public RefreshTokenService(IConfiguration configuration) {
            _configuration = configuration;
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public int GetMaxTokensPerUser()
        {
            return _configuration.GetValue<int>("RefreshToken:MaxTokensPerUser", 5);
        }

        public TimeSpan GetTokenLifetime()
        {
            var days = _configuration.GetValue<int>("RefreshToken:LifetimeDays", 7);
            return TimeSpan.FromDays(days);
        }

        public bool IsTokenExpired(RefreshToken token)
        {
            return DateTime.UtcNow > token.Expiration;
        }

        public bool IsTokenValid(RefreshToken token)
        {
            return token != null &&
                  !token.IsRevoked &&
                  !IsTokenExpired(token);
        }
    }
}
