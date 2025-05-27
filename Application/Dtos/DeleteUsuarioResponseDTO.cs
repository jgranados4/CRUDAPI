namespace CRUDAPI.Application.Dtos
{
    public class DeleteUsuarioResponseDTO
    {
        public int DeletedUserId { get; set; }
        public string DeletedUserEmail { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; }
        public string? Reason { get; set; }
        public bool WasSuccessful { get; set; }
    }
}
