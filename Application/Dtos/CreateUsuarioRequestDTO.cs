using CRUDAPI.Application.Constants;
using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Application.Dtos
{
    public class CreateUsuarioRequestDTO
    {
        [Required(ErrorMessage = ValidationConstants.NombreRequiredMessage)]
        [StringLength(ValidationConstants.NombreMaxLength, MinimumLength = ValidationConstants.NombreMinLength, ErrorMessage = ValidationConstants.NombreLengthMessage)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = ValidationConstants.EmailRequiredMessage)]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [StringLength(ValidationConstants.EmailMaxLength, ErrorMessage = ValidationConstants.EmailMaxLengthMessage)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = ValidationConstants.PasswordRequiredMessage)]
        [StringLength(100, MinimumLength = ValidationConstants.PasswordMinLength, ErrorMessage = ValidationConstants.PasswordLengthMessage)]
        [RegularExpression(ValidationConstants.PasswordRegex,
ErrorMessage = ValidationConstants.PasswordErrorMessage)]
        public string Contrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es requerido")]
        public string Rol { get; set; } = "User"; // Default value
    }
}
