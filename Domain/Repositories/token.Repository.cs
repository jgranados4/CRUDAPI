using CRUDAPI.Domain.entities;

namespace CRUDAPI.Domain.Repositories
{
    public interface ItokenRepository
    {
        string GenerateToken(UsuarioAU user);
        bool ValidarToken(string token);
        UsuarioResponse DecodeToken(string token);
    }
}
