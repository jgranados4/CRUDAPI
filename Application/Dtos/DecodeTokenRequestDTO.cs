using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Application.Dtos
{
    public class DecodeTokenRequestDTO
    {
        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; } = string.Empty;

        public bool IncludeUserDetails { get; set; } = false;
        public bool ValidateExpiration { get; set; } = true;
        public bool IncludeMetadata { get; set; } = false;
    }
}
