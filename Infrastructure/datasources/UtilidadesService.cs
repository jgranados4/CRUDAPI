using System.Security.Cryptography;
using System.Text;

namespace CRUDAPI.Infrastructure.datasources
{
    public interface IUtilidadesService
    {
        string EncriptarClave(string clave);
    }
    public class UtilidadesService : IUtilidadesService
    {
        public string EncriptarClave(string clave)
        {


            using (SHA256 hash = SHA256.Create())
            {
                StringBuilder sb = new StringBuilder();
                Encoding enc = Encoding.UTF8;

                byte[] result = hash.ComputeHash(enc.GetBytes(clave));

                foreach (byte b in result)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }


        }
    }
}
