namespace CRUDAPI.Domain.Dtos
{
    public class AuthResponse
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
    }
}
