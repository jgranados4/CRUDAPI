namespace CRUDAPI.Domain.Services
{
    public interface IPasswordHashingService
    {
        string EncriptarClave(string clave);
        bool VerificarClave(string clave, string hashGuardado);
    }
}