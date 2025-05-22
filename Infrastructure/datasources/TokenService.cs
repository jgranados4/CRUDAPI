using CRUDAPI.Domain.DataSources;
using CRUDAPI.Domain.entities;
using Microsoft.IdentityModel.Tokens;
using NuGet.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CRUDAPI.Infrastructure.datasources
{
    
    public class TokenService : ITokenDataSource
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TokenService(IConfiguration config,IHttpContextAccessor httpContext)
        {
            _config = config;
            _httpContextAccessor = httpContext;
        }
        public string GenerateToken(UsuarioAU user)
        {
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var signIn = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256Signature);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.Nombre),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("Rol",user.Rol)
            };
            //crear Token
            var token = new JwtSecurityToken(
                               issuer: _config["Jwt:Issuer"],
                                              audience: _config["Jwt:Audience"],
                                                             claims: claims,
                                                                            expires: DateTime.Now.AddMinutes(30),
                            signingCredentials: signIn);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public bool ValidarToken(string token)
        {
            var claimsPrincipal = new ClaimsPrincipal();
            var tokenHandler = new JwtSecurityTokenHandler();
            var validacionParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!))

            };
            try
            {
                claimsPrincipal = tokenHandler.ValidateToken(token, validacionParameters, out SecurityToken validateToken);
                return true;
            }

            catch (SecurityTokenExpiredException)
            {
                Console.Error.WriteLine("El token ha expirado.");
                return false;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                Console.Error.WriteLine("La firma del token no es válida.");
                return false;
            }
            catch (SecurityTokenException ex)
            {
                Console.Error.WriteLine($"Error de validación del token: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error inesperado: {ex}");
                throw;
            }

        }
        public UsuarioResponse DecodeToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

                if (jsonToken == null)
                {
                    throw new Exception("Token no válido.");
                }

                // Extraer los claims
                var userClaims = jsonToken?.Claims;
                var expClaim = userClaims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)?.Value;
                DateTime? expirationTime = null;
                TimeSpan? tiempoRestante = null;
                if (expClaim != null && long.TryParse(expClaim, out long expUnix))
                {
                    expirationTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                    tiempoRestante = expirationTime - DateTime.UtcNow;
                }

                return new UsuarioResponse
                {
                    Id = int.Parse(userClaims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.NameId)?.Value ?? "0"),
                    Nombre = userClaims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Name)?.Value ?? "Desconocido",
                    Email = userClaims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Email)?.Value ?? "Sin email",
                    Rol = userClaims?.FirstOrDefault(x => x.Type == "Rol")?.Value ?? "Sin rol",
                    Expiracion = expirationTime,
                    tiempoRestante = tiempoRestante?.TotalMinutes ?? 0,

                };
            }
            catch (Exception ex)
            {
                throw new Exception("Error al decodificar el token: " + ex.Message);
            }
        }

    }
}
