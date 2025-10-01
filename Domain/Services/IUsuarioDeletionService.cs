namespace CRUDAPI.Domain.Services
{
    public interface IUsuarioDeletionService
    {
        Task ValidateDeletionPermissionsAsync(int userId, string currentUserRole);
        Task ValidateUserCanBeDeletedAsync(int userId);
        Task<bool> HasPendingDependenciesAsync(int userId);
        Task HandlePreDeletionCleanupAsync(int userId);
    }
}
