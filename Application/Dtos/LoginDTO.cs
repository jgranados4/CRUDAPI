namespace CRUDAPI.Application.Dtos
{
    public class LoginDTO
    {

        public string? Constrasena { get; set; }
        public string? Email { get; set; }
        public string? Rol { get; set; } = "Cliente";
    }
}
