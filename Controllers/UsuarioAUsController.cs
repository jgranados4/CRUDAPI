using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRUDAPI.Models;
using CRUDAPI.Services;
using Newtonsoft.Json;
using CRUDAPI.Dtos;
using CRUDAPI.Services.contrato;

namespace CRUDAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioAUsController : ControllerBase
    {
        private readonly HolamundoContext _context;
        //Servicios
        private readonly ITokenService _tokenService;
        private readonly IUtilidadesService _utilidadesService;
        private readonly IEmailService _emailService;
        //Logger
        private readonly ILogger<UsuarioAUsController> _logger;

        public UsuarioAUsController(HolamundoContext context, ITokenService tokenService, ILogger<UsuarioAUsController> logger, IUtilidadesService utilidadesService,IEmailService emailService)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
            _utilidadesService = utilidadesService;
            _emailService = emailService;
        }

        // GET: api/UsuarioAUs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioAU>>> GetUsuariosAU()
        {
            if (_context.UsuariosAU == null)
            {
                return NotFound();
            }
            _logger.LogInformation("Obteniendo datos de la tabla UsuariosAU");
            return await _context.UsuariosAU.ToListAsync();
        }

        // GET: api/UsuarioAUs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioAU>> GetUsuarioAU(int id)
        {
            if (_context.UsuariosAU == null)
            {
                return NotFound();
            }
            var usuarioAU = await _context.UsuariosAU.FindAsync(id);

            if (usuarioAU == null)
            {
                return NotFound();
            }

            return usuarioAU;
        }

        // PUT: api/UsuarioAUs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuarioAU(int id, UsuarioAU usuarioAU)
        {
            if (id != usuarioAU.Id)
            {
                return BadRequest();
            }

            _context.Entry(usuarioAU).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioAUExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
        //[HttpPost("resetPassword")]
        //public async Task<ActionResult<UsuarioAU>> resetPassword(ResetPasswordDTO resetPasswordDTO)
        //{
        //    var usuarios=await _context.UsuariosAU.FindAsync(resetPasswordDTO.Email);
        //    if (usuarios is null)
        //    {
        //        return BadRequest(new AuthResponse
        //        {
        //            Message = "Usuario no encontrado"
        //        });
                
        //    }
        //    var result = await _context.UsuariosAU(resetPasswordDTO);

        //}
        //login
        [HttpPost("login")]
        public async Task<ActionResult<UsuarioAU>> Login(UsuarioAU usuarioAU)
        {
            var EncrypPass = _utilidadesService.EncriptarClave(usuarioAU.Constrasena);
            var usuario = await _context.UsuariosAU.FirstOrDefaultAsync(u => u.Email == usuarioAU.Email && u.Constrasena == EncrypPass);
            if (usuario == null)
            {
                return NotFound("Credenciales Incorrectas");
            }
            var token = _tokenService.GenerateToken(usuario);
            return Ok(new AuthResponse
            {
                Token = token,
                Message = "Login Exitoso"
            });
        }

        // POST: api/UsuarioAUs
        [HttpPost]
        public async Task<ActionResult<UsuarioAU>> PostUsuarioAU(UsuarioAU usuarioAU)
        {
            if (_context.UsuariosAU == null)
            {
                return Problem("Entity set 'HolamundoContext.UsuariosAU'  is null.");
            }
            usuarioAU.Constrasena = _utilidadesService.EncriptarClave(usuarioAU.Constrasena);
            _logger.LogInformation("Insertando datos en la tabla UsuariosAU" + usuarioAU.Constrasena);

            _context.UsuariosAU.Add(usuarioAU);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsuarioAU", new { id = usuarioAU.Id }, usuarioAU);
        }

        // DELETE: api/UsuarioAUs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuarioAU(int id)
        {
            if (_context.UsuariosAU == null)
            {
                return NotFound();
            }
            var usuarioAU = await _context.UsuariosAU.FindAsync(id);
            if (usuarioAU == null)
            {
                return NotFound();
            }

            _context.UsuariosAU.Remove(usuarioAU);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        //Enviar Correos
        [HttpPost("SendEmail")]
        public async Task<IActionResult> SendEmail()
        {
            try
            {
                MailrequestDTO mailrequest = new MailrequestDTO();
                mailrequest.ToEmail = "xavier10001985@gmail.com";
                mailrequest.Subject = "Prueba de Correo";
                mailrequest.Body = "<h1>Prueba de Correo</h1>";
                await _emailService.SendEmailAsync(mailrequest);
                return Ok("Correo Enviado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, "Error al enviar correo");
                return BadRequest(ex.Message);
            }
        }

        private bool UsuarioAUExists(int id)
        {
            return (_context.UsuariosAU?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
