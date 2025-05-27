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
using CRUDAPI.Helpers;

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
        private readonly IRefreshTokensource refresh;

        //Logger
        private readonly ILogger<UsuarioAUsController> _logger;

        public UsuarioAUsController(HolamundoContext context, ItokenRepository tokenRepo, ILogger<UsuarioAUsController> logger, IUtilidadesService utilidadesService, IRefreshTokensource refreshT)
        {
            _context = context;
            _tokenRepository = tokenRepo;
            _logger = logger;
            _utilidadesService = utilidadesService;
            refresh = refreshT;
        }

        // GET: api/UsuarioAUs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioAU>>> GetUsuariosAU()
        {
            if (_context.UsuariosAU == null)
            {
                return NotFound(ApiResponseFactory.NotFound<IEnumerable<UsuarioAU>>("No se encontró la entidad UsuariosAU."));


            }
            _logger.LogInformation("Obteniendo datos de la tabla UsuariosAU");
            var usuarios = await _context.UsuariosAU.ToListAsync();
            return Ok(ApiResponseFactory.Ok(usuarios));
        }

        // GET: api/UsuarioAUs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioAU>> GetUsuarioAU(int id)
        {
            var usuario = await _context.UsuariosAU.FindAsync(id);

            if (usuario == null)
            {
                return NotFound(ApiResponseFactory.NotFound<UsuarioAU>("Usuario no encontrado."));
            }

            return Ok(ApiResponseFactory.Ok(usuario));
        }
        // POST: api/UsuarioAUs
        [HttpPost]
        public async Task<ActionResult<UsuarioAUResponseDto>> PostUsuarioAU(UsuarioAU usuarioAU)
        {
            if (_context.UsuariosAU == null)
            {
                return StatusCode(500, new ApiResponse<string>(
                    HttpStatusHelper.GetStatusInfo(500),
                    message: "El contexto UsuariosAU no está disponible."
                ));
            }
            usuarioAU.Constrasena = _utilidadesService.EncriptarClave(usuarioAU.Constrasena);
            _logger.LogInformation("Insertando datos en la tabla UsuariosAU" + usuarioAU.Constrasena);

            _context.UsuariosAU.Add(usuarioAU);
            await _context.SaveChangesAsync();
            var responseDto = new UsuarioAUResponseDto
            {
                Id = usuarioAU.Id,
                Nombre = usuarioAU.Nombre,
                Email = usuarioAU.Email,
                Rol=usuarioAU.Rol
            };


            return CreatedAtAction(nameof(GetUsuarioAU), new { id = usuarioAU.Id }, new ApiResponse<UsuarioAUResponseDto>(
               HttpStatusHelper.GetStatusInfo(201),
               responseDto,
               "Usuario creado exitosamente."
           ));
        }

        // DELETE: api/UsuarioAUs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuarioAU(int id)
        {
            if (_context.UsuariosAU == null)
            {
                return NotFound(ApiResponseFactory.NotFound<string>("Contexto UsuariosAU no encontrado."));
            }
            var usuarioAU = await _context.UsuariosAU.FindAsync(id);
            if (usuarioAU == null)
            {
                return NotFound(ApiResponseFactory.NotFound<string>("Usuario no encontrado para eliminar."));
            }

            _context.UsuariosAU.Remove(usuarioAU);
            await _context.SaveChangesAsync();

            return Ok(ApiResponseFactory.Ok<string>(null, "Usuario eliminado correctamente."));
        }
        
        // PUT: api/UsuarioAUs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuarioAU(int id, UsuarioAU usuarioAU)
        {
            if (id != usuarioAU.Id)
            {
                return BadRequest(ApiResponseFactory.BadRequest<string>("ID del usuario no coincide."));
            }

            _context.Entry(usuarioAU).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent(); // 204
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioAUExists(id))
                {
                    return NotFound(ApiResponseFactory.NotFound<string>("Usuario no encontrado."));
                }
                else
                {
                    return StatusCode(500, ApiResponseFactory.ServerError<string>("Error actualizando el usuario."));


                }

            }
        }
        //Cambiar contrasena
        [HttpPut("ChangePassword/{id}")]
        public async Task<IActionResult> ChangePassword(int id, ResetPasswordDTO usuarioAU)
        {
            
            var usuario = await _context.UsuariosAU.FindAsync(id);
            if (usuario == null)
            {
                return NotFound(ApiResponseFactory.NotFound<string>("Usuario no encontrado."));
            }
            bool passwordCorrecta = _utilidadesService.VerificarClave(usuarioAU.CurrentPassword, usuario.Constrasena);
            if (!passwordCorrecta)
            {
                return BadRequest(ApiResponseFactory.BadRequest<string>("La contraseña actual es incorrecta."));
            }


            usuario.Constrasena = _utilidadesService.EncriptarClave(usuarioAU.NewPassword);
            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error cambiando contraseña: " + ex.Message);
                if (!UsuarioAUExists(id))
                    return NotFound(ApiResponseFactory.NotFound<string>("Usuario no encontrado."));
                else
                    throw;
            }
        }
        [HttpPost("login")]
        public async Task<ActionResult<UsuarioResponseDTO>> Login(UsuarioResponseDTO usuarioResponse)
        {
            var EncrypPass = _utilidadesService.EncriptarClave(usuarioResponse.Constrasena);
            var usuario = await _context.UsuariosAU.FirstOrDefaultAsync(u => u.Email == usuarioResponse.Email);
            if (usuario == null)
            {
                return Unauthorized(ApiResponseFactory.Unauthorized<string>("Credenciales incorrectas."));
            }
            bool passwordCorrecta = _utilidadesService.VerificarClave(usuarioResponse.Constrasena, usuario.Constrasena);
            if (!passwordCorrecta)
            {
                return Unauthorized(ApiResponseFactory.Unauthorized<string>("Credenciales incorrectas."));
            }

            var token = _tokenRepository.GenerateToken(usuario);
            var refreshToken = refresh.GenerateRefreshToken();
            ///**Token Refresh**
            var refreshtokenEntity = new RefreshToken
            {
                Token = refreshToken,
                Expiration = DateTime.UtcNow.AddDays(2),
                UsuarioId = usuario.Id,
            };
            var authResponse = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken
            };
            return Ok(ApiResponseFactory.Ok(authResponse, "Login exitoso"));
        }
        [HttpPost("VToken")]
        public ActionResult<bool> ValidarToken(string token)
        {
            var valido = _tokenRepository.ValidarToken(token);
            return Ok(valido);
        }
        [HttpGet("DecodeToken")]
        public ActionResult<UsuarioAU> DecodificarToken(string token)
        {
            var decoded = _tokenRepository.DecodeToken(token);
            return Ok(decoded);
        }
        private bool UsuarioAUExists(int id)
        {
            return (_context.UsuariosAU?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
