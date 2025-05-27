using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Application.Dtos
{
    public class DeleteUsuarioRequestDTO
    {
        [Required(ErrorMessage = "La contraseña de confirmación es requerida")]
        public string ConfirmationPassword { get; set; } = string.Empty;

        public string? Reason { get; set; }
    }
}
