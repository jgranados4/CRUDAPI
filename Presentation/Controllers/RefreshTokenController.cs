using CRUDAPI.Application.Common.Responses;
using CRUDAPI.Application.Dtos;
using CRUDAPI.Application.UseCases;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using CRUDAPI.Infrastructure.Persistence.context;
using CRUDAPI.Infrastructure.services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security;

namespace CRUDAPI.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefreshTokenController : ControllerBase
    {
        #region Dependencias - Casos de Uso Únicamente

        private readonly RefreshTokenUseCase _refreshTokenUseCase;
        private readonly RevokeTokenUseCase _revokeTokenUseCase;
        private readonly RevokeAllUserTokensUseCase _revokeAllTokensUseCase;
        private readonly ILogger<RefreshTokenController> _logger;
        #endregion

        public RefreshTokenController(
           RefreshTokenUseCase refreshTokenUseCase,
           RevokeTokenUseCase revokeTokenUseCase,
           RevokeAllUserTokensUseCase revokeAllTokensUseCase,
           ILogger<RefreshTokenController> logger)
        {
            _refreshTokenUseCase = refreshTokenUseCase;
            _revokeTokenUseCase = revokeTokenUseCase;
            _revokeAllTokensUseCase = revokeAllTokensUseCase;
            _logger = logger;
        }
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] TokenRequest request)
        {
            // ✅ CAPA DE PRESENTACIÓN: Validación superficial de entrada
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Solicitud de refresh token con modelo inválido");
                return BadRequest(ApiResponseFactory.BadRequest<string>("Datos de entrada inválidos"));
            }

            try
            {
                // 🎯 DELEGACIÓN PURA: El controlador transfiere toda la responsabilidad al caso de uso
                var result = await _refreshTokenUseCase.ExecuteAsync(request);

                _logger.LogInformation("Refresh token exitoso");
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                // ✅ Excepción de validación de dominio → HTTP 400 Bad Request
                _logger.LogWarning("Argumentos inválidos en refresh token: {Message}", ex.Message);
                return BadRequest(ApiResponseFactory.BadRequest<string>(ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                // ✅ Excepción de autorización de dominio → HTTP 401 Unauthorized
                _logger.LogWarning("Token no autorizado: {Message}", ex.Message);
                return Unauthorized(ApiResponseFactory.Unauthorized<string>(ex.Message));
            }
            catch (SecurityException ex)
            {
                // ✅ Excepción de seguridad crítica → HTTP 401 con mensaje específico
                _logger.LogError("Violación de seguridad detectada: {Message}", ex.Message);
                return Unauthorized(ApiResponseFactory.Unauthorized<string>(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                // ✅ Error interno del sistema → HTTP 500 Internal Server Error
                _logger.LogError(ex, "Error interno en refresh token");
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }

        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeToken([FromBody] TokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponseFactory.BadRequest<string>("Datos de entrada inválidos"));
            }

            try
            {
                var success = await _revokeTokenUseCase.ExecuteAsync(request.RefreshToken);

                if (success)
                {
                    _logger.LogInformation("Token revocado exitosamente");
                    return Ok(ApiResponseFactory.Ok("Token revocado exitosamente"));
                }
                else
                {
                    _logger.LogWarning("Intento de revocar token inexistente o ya revocado");
                    return NotFound(ApiResponseFactory.NotFound<string>("Token no encontrado o ya revocado"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interno revocando token");
                return StatusCode(500, ApiResponseFactory.ServerError<string>("Error interno del servidor"));
            }
        }
        [HttpPost("revoke-all")]
        public async Task<IActionResult> RevokeAllTokens([FromBody] RevokeAllTokensRequest request)
        {
            // ✅ Validación de entrada en capa de presentación
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Solicitud de revocación masiva con datos inválidos para usuario {UserId}",
                    request?.UsuarioId);
                return BadRequest(ApiResponseFactory.BadRequest<string>("Datos de entrada inválidos"));
            }

            try
            {
                // 🎯 Delegación completa al caso de uso especializado
                var revokedCount = await _revokeAllTokensUseCase.ExecuteAsync(request.UsuarioId);

                _logger.LogInformation("Revocación masiva exitosa: {Count} tokens revocados para usuario {UserId}",
                    revokedCount, request.UsuarioId);

                return Ok(ApiResponseFactory.Ok($"{revokedCount} tokens revocados exitosamente"));
            }
            catch (Exception ex)
            {
                // ✅ Error crítico: Logging detallado para investigación posterior
                _logger.LogError(ex, "Error crítico en revocación masiva para usuario {UserId}", request.UsuarioId);

                return StatusCode(500, ApiResponseFactory.ServerError<string>(
                    "Error interno del servidor. La operación ha sido registrada para revisión."));
            }
        }
        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                // Verificación ligera: ¿Puede el sistema acceder al repositorio?
                // En un sistema real, podrías verificar conexión a BD, servicios externos, etc.

                _logger.LogDebug("Health check del sistema de refresh tokens");

                return Ok(new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    service = "RefreshTokenService"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check falló");
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    error = "Service temporarily unavailable"
                });
            }
        }
    }
}
