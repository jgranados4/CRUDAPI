using CRUDAPI.Application.Constants;
using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Application.Dtos
{
    public class UpdateUsuarioRequestDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 255 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = ValidationConstants.EmailRequiredMessage)]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [StringLength(ValidationConstants.EmailMaxLength, ErrorMessage = ValidationConstants.EmailMaxLengthMessage)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es requerido")]
        public string Rol { get; set; } = string.Empty;
    }
}
