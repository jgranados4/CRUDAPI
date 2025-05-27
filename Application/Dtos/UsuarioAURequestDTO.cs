namespace CRUDAPI.Application.Dtos
{
    public class UsuarioAURequestDTO
    {
        public int Id { get; set; }

        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public string? Rol { get; set; } = "Cliente";
    }
}
