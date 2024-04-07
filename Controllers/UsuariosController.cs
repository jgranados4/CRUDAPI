using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CRUDAPI.Models;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;

namespace CRUDAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly HolamundoContext _context;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(HolamundoContext context, ILogger<UsuariosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            try
            {
                if (_context.Usuarios == null)
                {
                    return NotFound();
                }
                return await _context.Usuarios.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error en el metodo GetUsuarios:",ex.Message);
                return StatusCode(500, "Se produjo un error al obtener los datos");
            }
            
        }
        [HttpPost("ProcedureUsuario")]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuariosPro(Class input)
        {
            try
            {
                if (_context.Usuarios == null)
                {
                    return NotFound();
                }
                string storedProcedure = $"CALL `api_crud`({input.i})";
                return await _context.Usuarios.FromSqlRaw(storedProcedure).ToListAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Se produjo un error al ejecutar el procedimiento {ex.Message}");
            }

            //return await _context.Usuarios.ToListAsync();
        }

        // GET: api/Usuarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            if (_context.Usuarios == null)
            {
                return NotFound();
            }
            var usuario = await _context.Usuarios.FindAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }

        // PUT: api/Usuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            if (id != usuario.Id)
            {
                return BadRequest();
            }

            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(id))
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

        // POST: api/Usuarios
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            if (_context.Usuarios == null)
            {
                return Problem("Entity set 'HolamundoContext.Usuarios'  is null.");
            }
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsuario", new { id = usuario.Id }, usuario);
        }

        // DELETE: api/Usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            if (_context.Usuarios == null)
            {
                return NotFound();
            }
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        //DELETEALL:api/Usuarios
        [HttpDelete("DeleteTotal")]
        public async  Task<IActionResult> TruncateUsuarios()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE usuario");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Se produjo un error  al Eliminar la tablas {ex.Message}");

            }

        }

        private bool UsuarioExists(int id)
        {
            return (_context.Usuarios?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
