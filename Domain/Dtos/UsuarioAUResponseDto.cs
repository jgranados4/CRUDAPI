namespace CRUDAPI.Domain.Dtos
{
    public class UsuarioAUResponseDto
    {
        public int Id { get; set; }

        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public string? Rol { get; set; } = "Cliente";
    }
}
