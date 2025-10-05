using CRUDAPI.Application.Common.Responses;
using CRUDAPI.Application.Dtos;
using CRUDAPI.Application.UseCases;
using CRUDAPI.Domain.entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRUDAPI.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioAUsController : ControllerBase
    {
        private readonly ILogger<UsuarioAUsController> _logger;

        // ✅ Use Cases - Single Responsibility per endpoint
        private readonly LoginUsuarioUseCase _loginUseCase;
        private readonly CreateUsuarioUseCase _createUsuarioUseCase;
        private readonly GetUsuarioUseCase _getUsuarioUseCase;
        private readonly UpdateUsuarioUseCase _updateUsuarioUseCase;
        private readonly DeleteUsuarioUseCase _deleteUsuarioUseCase;
        private readonly ChangePasswordUseCase _changePasswordUseCase;

        // ✅ Token-specific Use Cases
        private readonly ValidateTokenUseCase _validateTokenUseCase;
        private readonly DecodeTokenUseCase _decodeTokenUseCase;

        public UsuarioAUsController(
            ILogger<UsuarioAUsController> logger,
            LoginUsuarioUseCase loginUseCase,
            CreateUsuarioUseCase createUsuarioUseCase,
            GetUsuarioUseCase getUsuarioUseCase,
            UpdateUsuarioUseCase updateUsuarioUseCase,
            DeleteUsuarioUseCase deleteUsuarioUseCase,
            ChangePasswordUseCase changePasswordUseCase,
            ValidateTokenUseCase validateTokenUseCase,
            DecodeTokenUseCase decodeTokenUseCase)
        {
            _logger = logger;
            _loginUseCase = loginUseCase;
            _createUsuarioUseCase = createUsuarioUseCase;
            _getUsuarioUseCase = getUsuarioUseCase;
            _updateUsuarioUseCase = updateUsuarioUseCase;
            _deleteUsuarioUseCase = deleteUsuarioUseCase;
            _changePasswordUseCase = changePasswordUseCase;
            _validateTokenUseCase = validateTokenUseCase;
            _decodeTokenUseCase = decodeTokenUseCase;
        }

        /// <summary>
        /// Login endpoint - Clean architecture implementation
        /// Responsibility: HTTP concerns only
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                // ✅ Input validation (Controller responsibility)
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponseFactory.BadRequest<string>("Datos de entrada inválidos"));
                }

                // ✅ Delegate business logic to Use Case
                var authResponse = await _loginUseCase.ExecuteAsync(request);

                // ✅ HTTP response formatting (Controller responsibility)
                return Ok(ApiResponseFactory.Ok(authResponse, "Login exitoso"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Datos inválidos en login: {Message}", ex.Message);
                return BadRequest(ApiResponseFactory.BadRequest<string>(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogWarning("Intento de login fallido para email: {Email}", request.Email);
                return Unauthorized(ApiResponseFactory.Unauthorized<string>("Credenciales incorrectas"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante el login");
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }
        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioAU>>> GetUsuariosAU()
        {
            try
            {
                var usuarios = await _getUsuarioUseCase.GetAllAsync();
                return Ok(ApiResponseFactory.Ok(usuarios));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuarios");
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioAU>> GetUsuarioAU(int id)
        {
            try
            {
                var usuario = await _getUsuarioUseCase.GetByIdAsync(id);
                return Ok(ApiResponseFactory.Ok(usuario));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponseFactory.NotFound<UsuarioAU>("Usuario no encontrado"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuario {Id}", id);
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }

        [HttpPost]
        public async Task<ActionResult<UsuarioAUResponseDto>> PostUsuarioAU([FromBody] CreateUsuarioRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponseFactory.BadRequest<string>("Datos de entrada inválidos"));
                }

                var usuario = await _createUsuarioUseCase.ExecuteAsync(request);
                return CreatedAtAction(
                    nameof(GetUsuarioAU),
                    new { id = usuario.Id },
                    ApiResponseFactory.Ok(usuario, "Usuario creado exitosamente"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseFactory.BadRequest<string>(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando usuario");
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuarioAU(int id, [FromBody] UpdateUsuarioRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponseFactory.BadRequest<string>("Datos de entrada inválidos"));
                }

                await _updateUsuarioUseCase.ExecuteAsync(id, request);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseFactory.BadRequest<string>(ex.Message));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponseFactory.NotFound<string>("Usuario no encontrado"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando usuario {Id}", id);
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuarioAU(int id)
        {
            try
            {
                await _deleteUsuarioUseCase.ExecuteAsync(id);
                return Ok(ApiResponseFactory.Ok<string>(null, "Usuario eliminado correctamente"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponseFactory.NotFound<string>("Usuario no encontrado"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando usuario {Id}", id);
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }

        [HttpPut("ChangePassword/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponseFactory.BadRequest<string>("Datos de entrada inválidos"));
                }

                await _changePasswordUseCase.ExecuteAsync(id, request);
                return Ok(ApiResponseFactory.Ok<string>(null, "Contraseña cambiada exitosamente"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponseFactory.NotFound<string>("Usuario no encontrado"));
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(ApiResponseFactory.BadRequest<string>(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando contraseña para usuario {Id}", id);
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }
        [HttpPost("VToken")]
        public async Task<ActionResult<bool>> ValidarToken([FromBody] string token)
        {
            try
            {
                var isValid = await _validateTokenUseCase.ExecuteQuickValidationAsync(token);
                return Ok(ApiResponseFactory.Ok(isValid,"Token Valido"));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseFactory.BadRequest<string>(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando token");
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }

        [HttpGet("DecodeToken")]
        public async Task<ActionResult<UsuarioAU>> DecodificarToken([FromQuery] string token)
        {
            try
            {
                var decoded = await _decodeTokenUseCase.ExecuteQuickDecodeAsync(token);
                return Ok(ApiResponseFactory.Ok(decoded));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ApiResponseFactory.BadRequest<string>(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decodificando token");
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }
    }
}