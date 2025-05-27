namespace CRUDAPI.Application.Dtos
{
    public class ChangePasswordResponseDTO
    {
        public bool IsSuccessful { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool ShouldLogoutOtherSessions { get; set; }
    }
}
