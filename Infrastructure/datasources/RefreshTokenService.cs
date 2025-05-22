using CRUDAPI.Domain.DataSources;
using System.Security.Cryptography;

namespace CRUDAPI.Infrastructure.datasources
{
    public class RefreshTokenService : IRefreshTokensource
    {
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

    }
}
