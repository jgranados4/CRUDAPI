using CRUDAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CRUDAPI.Services
{
    public interface ITokenService
    {
        string GenerateToken(UsuarioAU user);
    }
    public class TokenService:ITokenService
    {
        private readonly IConfiguration _config;
        public TokenService(IConfiguration config)
        {
            _config = config;
        }
         public  string GenerateToken(UsuarioAU user)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:key"]));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Nombre),
                new Claim(ClaimTypes.Email, user.Email),
                //new Claim(ClaimTypes.GivenName, user.FirstName),
                //new Claim(ClaimTypes.Surname, user.LastName),
                //new claim(claimtypes.role, user.rol),
            };
            //crear Token
            var token = new JwtSecurityToken(
                               issuer: _config["Jwt:Issuer"],
                                              audience: _config["Jwt:Audience"],
                                                             claims: claims,
                                                                            expires: DateTime.Now.AddMinutes(30),
                                                                                           signingCredentials: credentials
                                                                                                      );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
