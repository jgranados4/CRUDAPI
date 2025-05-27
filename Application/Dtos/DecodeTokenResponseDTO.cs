namespace CRUDAPI.Application.Dtos
{
    public class DecodeTokenResponseDTO
    {
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }

        public TokenClaims? Claims { get; set; }
        public TokenUserDetails? UserDetails { get; set; }
        public TokenMetadata? Metadata { get; set; }

        public DateTime? IssuedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public TimeSpan? TimeUntilExpiration { get; set; }
        public bool IsExpired { get; set; }
    }
    public class TokenClaims
    {
        public int UserId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime? Expiracion { get; set; }
        public double tiempoRestante { get; set; }
        
        public List<string> Permissions { get; set; } = new List<string>();
    }

    public class TokenUserDetails
    {
        public string Nombre { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool RequiresPasswordChange { get; set; }
    }

    public class TokenMetadata
    {
        public string TokenType { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public string? ClientIp { get; set; }
        public string? UserAgent { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }
}
