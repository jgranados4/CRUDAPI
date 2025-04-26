namespace CRUDAPI.Domain.entities
{
    public class UsuarioResponse
    {
        public int Id { get; set; }

        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public string? Rol { get; set; }
        public DateTime? Expiracion { get; set; }
        public double tiempoRestante { get; set; }
    }
}
