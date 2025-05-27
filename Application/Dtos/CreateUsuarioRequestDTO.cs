using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Application.Dtos
{
    public class CreateUsuarioRequestDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [StringLength(300, ErrorMessage = "El email no puede exceder 300 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
            ErrorMessage = "La contraseña debe contener al menos: 1 mayúscula, 1 minúscula, 1 número y 1 carácter especial")]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es requerido")]
        public string Rol { get; set; } = "User"; // Default value
    }
}
