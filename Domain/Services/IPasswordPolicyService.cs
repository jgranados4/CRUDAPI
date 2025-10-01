namespace CRUDAPI.Domain.Services
{
    public interface IPasswordPolicyService
    {
        Task ValidatePasswordStrengthAsync(string password);
        Task ValidatePasswordHistoryAsync(int userId, string newPassword);
        bool IsPasswordRecentlyUsed(string password, IEnumerable<string> previousPasswords);
        Task<TimeSpan> GetPasswordExpirationTimeAsync(int userId);
        bool RequiresPasswordChange(DateTime lastPasswordChange);
        Task LogPasswordChangeAttemptAsync(int userId, bool wasSuccessful, string? failureReason = null);
    }
}
