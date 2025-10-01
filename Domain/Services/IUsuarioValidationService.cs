namespace CRUDAPI.Domain.Services
{
    public interface  IUsuarioValidationService
    {
        Task ValidateUserCreationAsync(string email, string nombre);
        Task ValidateEmailFormatAsync(string email);
        Task ValidatePasswordRequirementsAsync(string password);
        bool IsValidRole(string rol);
        Task ValidateUserUniquenessAsync(string email);
    }
}
