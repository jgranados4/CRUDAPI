using CRUDAPI.Domain.entities;

namespace CRUDAPI.Domain.DataSources
{
    public interface ITokenDataSource
    {
        string GenerateToken(UsuarioAU user);
        bool ValidarToken(string token);
        UsuarioAU DecodeToken(string token);
    }
}
