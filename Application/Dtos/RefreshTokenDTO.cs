using CRUDAPI.Domain.entities;

namespace CRUDAPI.Application.Dtos
{
    public class RefreshTokenDTO
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public int UsuarioId { get; set; }
        public UsuarioAU Usuario { get; set; } = null!;
        public bool IsRevoked { get; set; } = false;
        public DateTime Created { get; set; }
        public string CreatedByIp { get; set; } = string.Empty;

        public DateTime? RevokedAt { get; set; }
    }
}
