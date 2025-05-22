namespace CRUDAPI.Domain.entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public int UsuarioId { get; set; }
        public UsuarioAU Usuario { get; set; } = null!;
        public bool IsRevoked { get; set; } = false;
    }
}
