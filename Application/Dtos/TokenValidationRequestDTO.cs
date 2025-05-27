using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Application.Dtos
{
    public class TokenValidationRequestDTO
    {
        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de token a validar (JWT, RefreshToken, etc.)
        /// Permite manejar diferentes formatos de token con un solo endpoint
        /// </summary>
        public TokenType TokenType { get; set; } = TokenType.JWT;

        /// <summary>
        /// Indica si se debe verificar la expiración del token
        /// Útil para casos donde se necesita información de tokens expirados
        /// </summary>
        public bool CheckExpiration { get; set; } = true;

        /// <summary>
        /// Valida si el token debe estar activo (no revocado)
        /// Aplicable principalmente a refresh tokens
        /// </summary>
        public bool CheckRevocation { get; set; } = true;
    }

    public enum TokenType
    {
        JWT = 1,
        RefreshToken = 2
    }
}

