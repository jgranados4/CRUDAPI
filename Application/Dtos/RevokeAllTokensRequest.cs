using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Application.Dtos
{
    public class RevokeAllTokensRequest
    {
        [Required]
        public int UsuarioId { get; set; }
    }
}
