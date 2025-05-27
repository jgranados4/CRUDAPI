using CRUDAPI.Domain.DataSources;
using CRUDAPI.Domain.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Infrastructure.datasources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUDAPI.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefreshTokenController : ControllerBase
    {
        private readonly HolamundoContext _context;
        private readonly ItokenRepository _tokenRepository;
        private readonly IRefreshTokensource refresh;
        public RefreshTokenController(
            HolamundoContext context, ItokenRepository tokenRepo,IRefreshTokensource refreshTokensource, IRefreshTokensource refreshT) 
        {
            _context = context;
            _tokenRepository = tokenRepo;
            refresh = refreshTokensource;

        }
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] TokenRequest request)
        {
            var storedToken = await _context.RefreshTokens
                .Include(r => r.Usuario)
                .FirstOrDefaultAsync(r => r.Token == request.RefreshToken && !r.IsRevoked);

            if (storedToken == null || storedToken.Expiration < DateTime.UtcNow)
            {
                return Unauthorized("Refresh Token inválido o expirado.");
            }

            var newAccessToken = _tokenRepository.GenerateToken(storedToken.Usuario);
            var newRefreshToken = refresh.GenerateRefreshToken();

            // Revocar el token anterior
            storedToken.IsRevoked = true;

            // Guardar nuevo token
            var newTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                Expiration = DateTime.UtcNow.AddDays(7),
                UsuarioId = storedToken.UsuarioId
            };

            _context.RefreshTokens.Add(newTokenEntity);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

    }
}
