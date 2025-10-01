namespace CRUDAPI.Domain.Services
{
    public interface ITokenValidationService
    {
        Task<bool> IsTokenBlacklistedAsync(string token);
        Task<bool> IsTokenRevokedAsync(string token);
        Task<bool> IsUserActiveAsync(int userId);
        Task<bool> RequiresPasswordChangeAsync(int userId);
        Task LogTokenValidationAttemptAsync(string token, bool isValid, string? reason = null);
        Task<Dictionary<string, object>> ExtractTokenMetadataAsync(string token);
    }
}
