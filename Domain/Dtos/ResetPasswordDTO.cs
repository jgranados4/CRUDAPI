using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Domain.Dtos
{
    public class ResetPasswordDTO
    {
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
