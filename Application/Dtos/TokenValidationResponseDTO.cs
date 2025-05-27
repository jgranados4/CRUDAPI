namespace CRUDAPI.Application.Dtos
{
    public class TokenValidationResponseDTO
    {
        /// <summary>
        /// Indica si el token es válido según todos los criterios solicitados
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Razón específica de invalidez si el token no es válido
        /// Útil para debugging y logging de problemas de autenticación
        /// </summary>
        public string? ValidationFailureReason { get; set; }

        /// <summary>
        /// Información básica del usuario propietario del token (si es válido)
        /// </summary>
        public TokenUserInfo? UserInfo { get; set; }

        /// <summary>
        /// Fecha de expiración del token
        /// Permite al cliente conocer cuándo debe renovar el token
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Tiempo restante antes de la expiración
        /// Útil para implementar renovación proactiva de tokens
        /// </summary>
        public TimeSpan? TimeUntilExpiration { get; set; }

        /// <summary>
        /// Metadatos adicionales del token
        /// Incluye información como IP de creación, dispositivo, etc.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public class TokenUserInfo
        {
            public int UserId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public DateTime? LastPasswordChange { get; set; }
        }
    }
}
