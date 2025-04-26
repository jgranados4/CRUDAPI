using CRUDAPI.Domain.DataSources;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;

namespace CRUDAPI.Infrastructure.repositories
{
    public class tokenRepository : ItokenRepository
    {
        private readonly ITokenDataSource tokenDataSource;
        public tokenRepository(ITokenDataSource itoken )
        {
            tokenDataSource = itoken;
        }

        public UsuarioResponse DecodeToken(String token)
        {
             return tokenDataSource.DecodeToken(token);
        }

        public string GenerateToken(UsuarioAU user)
        {
            return tokenDataSource.GenerateToken(user);   
        }

        public bool ValidarToken(string token)
        {
            return tokenDataSource.ValidarToken(token);
        }
    }
}
