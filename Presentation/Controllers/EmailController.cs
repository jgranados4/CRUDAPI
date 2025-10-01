using CRUDAPI.Domain.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CRUDAPI.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEnviar ServicioEnviar;
        public EmailController(IEnviar enviar) {
            this.ServicioEnviar = enviar;
        }
        [HttpPost]
        public async Task<ActionResult> Enviar(string email,string tema,string cuerpo)
        {
            await ServicioEnviar.enviar(email,tema,cuerpo);
            return Ok();
        }
    }
}
