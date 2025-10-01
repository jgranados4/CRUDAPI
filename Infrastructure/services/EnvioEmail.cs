using CRUDAPI.Domain.Repositories;
using System.Net;
using System.Net.Mail;

namespace CRUDAPI.Infrastructure.services
{


    public class EnvioEmail : IEnviar
    {
        private readonly IConfiguration configuration;
        public EnvioEmail(IConfiguration configuration)
        {
            this.configuration = configuration;

        }

        public async Task enviar(string emailReceptor, string tema, string cuerpo)
        {
            var emailEmisor = configuration.GetValue<string>("CONFIGURACION_EMAIL:EMAIL");
            var password = configuration.GetValue<string>("CONFIGURACION_EMAIL:PASSWORD");
            var host = configuration.GetValue<string>("CONFIGURACION_EMAIL:HOST");
            var puerto = configuration.GetValue<int>("CONFIGURACION_EMAIL:PUERTO");
            var smtpCliente = new SmtpClient(host, puerto);
            smtpCliente.EnableSsl = true;
            smtpCliente.UseDefaultCredentials = false;
            smtpCliente.Credentials = new NetworkCredential(emailEmisor, password);
            smtpCliente.TargetName = "STARTTLS/smtp.gmail.com";
            smtpCliente.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpCliente.Timeout = 30000;
            var mensaje = new MailMessage(emailEmisor!, emailReceptor, tema, cuerpo);
            mensaje.IsBodyHtml = false;
            try
            {
                // Agregar logging adicional
                Console.WriteLine($"Conectando a {host}:{puerto}");
                Console.WriteLine($"SSL habilitado: {smtpCliente.EnableSsl}");
                Console.WriteLine($"Email emisor: {emailEmisor}");
                await smtpCliente.SendMailAsync(mensaje);
                Console.WriteLine("Email enviado exitosamente");
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"Error SMTP: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general: {ex.Message}");
                throw;
            }

        }
    }
}
