using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;

namespace CRUDAPI.Domain.Services
{
    public interface ITokenService
    {
        UsuarioResponse DecodeToken(string token);
        string GenerateToken(UsuarioAURequestDTO user);
        bool ValidarToken(string token);
    }
}