namespace CRUDAPI.Application.Dtos
{
    public class AuthResponse
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public int ActiveTokensCount { get; set; }
        public int MaxTokensAllowed { get; set; }
        public string? WarningMessage { get; set; }
        public bool IsNearLimit { get; set; }
    }
}
