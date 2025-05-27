using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Domain.Dtos
{
    public class ResetPasswordDTO
    {
        [Required]
        [MinLength(6)]
        public string CurrentPassword { get; set; }
        
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
