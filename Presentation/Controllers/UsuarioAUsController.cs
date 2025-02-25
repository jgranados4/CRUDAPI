using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using CRUDAPI.Domain.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Infrastructure.datasources;
using CRUDAPI.Domain.DataSources;
using CRUDAPI.Domain.Repositories;

namespace CRUDAPI.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioAUsController : ControllerBase
    {
        private readonly HolamundoContext _context;
        //Servicios
        private readonly ItokenRepository _tokenRepository;
        private readonly IUtilidadesService _utilidadesService;
      
        //Logger
        private readonly ILogger<UsuarioAUsController> _logger;

        public UsuarioAUsController(HolamundoContext context, ItokenRepository tokenRepo, ILogger<UsuarioAUsController> logger, IUtilidadesService utilidadesService)
        {
            _context = context;
            _tokenRepository = tokenRepo;
            _logger = logger;
            _utilidadesService = utilidadesService;
            
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
        //Cambiar contrasena
        [HttpPut("ChangePassword/{id}")]
        public async Task<IActionResult> ChangePassword(int id, UsuarioAU usuarioAU)
        {
            if (id != usuarioAU.Id)
            {
                return BadRequest();
            }
            var usuario = await _context.UsuariosAU.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }
            usuario.Constrasena = _utilidadesService.EncriptarClave(usuarioAU.Constrasena);
            _context.Entry(usuario).State = EntityState.Modified;
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
        [HttpPost("login")]
        public async Task<ActionResult<UsuarioResponseDTO>> Login(UsuarioResponseDTO usuarioResponse)
        {
            var EncrypPass = _utilidadesService.EncriptarClave(usuarioResponse.Constrasena);
            var usuario = await _context.UsuariosAU.FirstOrDefaultAsync(u => u.Email == usuarioResponse.Email && u.Constrasena == EncrypPass);
            if (usuario == null)
            {
                return NotFound("Credenciales Incorrectas");
            }
            var token = _tokenRepository.GenerateToken(usuario);
            return Ok(new AuthResponse
            {
                Token = token,
                Message = "Login Exitoso"
            });
        }
        [HttpPost("VToken")]
        public ActionResult<bool> ValidarToken(string token)
        {
            var vali = _tokenRepository.ValidarToken(token);
            Console.WriteLine($"validar token , {token}");
            return Ok(vali);
        }
        [HttpGet("DecodeToken")]
        public  ActionResult<UsuarioAU> DecodificarToken(string token)
        {
            var deco=_tokenRepository.DecodeToken(token);
            return Ok(deco);
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


        private bool UsuarioAUExists(int id)
        {
            return (_context.UsuariosAU?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
