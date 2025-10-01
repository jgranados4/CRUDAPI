namespace CRUDAPI.Domain.Repositories
{
    public interface IEnviar
    {
        Task enviar(string  emailReceptor, string tema, string cuerpo);
    }
}
