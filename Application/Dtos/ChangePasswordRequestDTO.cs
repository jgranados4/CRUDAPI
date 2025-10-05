using CRUDAPI.Application.Constants;
using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Application.Dtos
{
    public class ChangePasswordRequestDTO
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = ValidationConstants.PasswordRequiredMessage)]
        [StringLength(100, MinimumLength = ValidationConstants.PasswordMinLength, ErrorMessage = ValidationConstants.PasswordLengthMessage)]
        [RegularExpression(ValidationConstants.PasswordRegex,
ErrorMessage = ValidationConstants.PasswordErrorMessage)]
        public string NewPassword { get; set; } = string.Empty;
        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
