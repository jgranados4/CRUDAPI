using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRUDAPI.Models;
using CRUDAPI.Services;

namespace CRUDAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioAUsController : ControllerBase
    {
        private readonly HolamundoContext _context;
        //Servicios
        private readonly ITokenService _tokenService;

        public UsuarioAUsController(HolamundoContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // GET: api/UsuarioAUs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioAU>>> GetUsuariosAU()
        {
          if (_context.UsuariosAU == null)
          {
              return NotFound();
          }
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
        //login
        [HttpPost("login")]
        public async Task<ActionResult<UsuarioAU>> Login(UsuarioAU usuarioAU)
        {
            var usuario = await _context.UsuariosAU.FirstOrDefaultAsync(u => u.Email == usuarioAU.Email && u.Constrasena == usuarioAU.Constrasena);
            if (usuario == null)
            {
                return NotFound("No encontrado");
            }
            var token= _tokenService.GenerateToken(usuario);
            return Ok(token);
        }

        // POST: api/UsuarioAUs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<UsuarioAU>> PostUsuarioAU(UsuarioAU usuarioAU)
        {
          if (_context.UsuariosAU == null)
          {
              return Problem("Entity set 'HolamundoContext.UsuariosAU'  is null.");
          }
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
