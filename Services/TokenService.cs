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
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        public TokenService(IConfiguration config)
        {
            _config = config;
        }
        public string GenerateToken(UsuarioAU user)
        {
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var signIn = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.Nombre),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role,user.Rol)
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
    }
}
