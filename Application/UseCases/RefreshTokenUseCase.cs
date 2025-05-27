using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using CRUDAPI.Infrastructure.repositories;
using CRUDAPI.Infrastructure.services;
using System.Security;

namespace CRUDAPI.Application.UseCases
{
    public class RefreshTokenUseCase
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextService _httpContextService;
        private readonly ILogger<RefreshTokenUseCase> _logger;

        public RefreshTokenUseCase(
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            IHttpContextService httpContextService,
            IRefreshTokenService _refreshTokenService,
            ILogger<RefreshTokenUseCase> logger)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _httpContextService = httpContextService;
            _logger = logger;
            this._refreshTokenService = _refreshTokenService;
        }
        public async Task<AuthResponse> ExecuteAsync(TokenRequest request)
        {
            try
            {
                // ✅ STEP 1: Validación de entrada del dominio
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    throw new ArgumentException("Refresh token es requerido");

                // ✅ STEP 2: Obtener el token almacenado (sin usuario - separación de responsabilidades)
                var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

                if (storedToken == null)
                    throw new UnauthorizedAccessException("Token no válido");

                // ✅ STEP 3: Aplicar reglas de negocio del dominio usando el servicio
                if (!_refreshTokenService.IsTokenValid(storedToken))
                {
                    if (_refreshTokenService.IsTokenExpired(storedToken))
                        throw new UnauthorizedAccessException("Token expirado");

                    if (storedToken.IsRevoked)
                    {
                        // 🚨 Política de seguridad crítica: Revocar todos los tokens del usuario
                        await _refreshTokenRepository.RevokeAllUserTokensAsync(storedToken.UsuarioId);
                        throw new SecurityException("Intento de reutilización detectado. Todos los tokens revocados.");
                    }
                }

                // ✅ STEP 4: Obtener información del usuario para generar nuevo access token
                // Nota: En un sistema real, podrías tener un IUserRepository separado
                // Aquí asumimos que storedToken.Usuario está disponible por navegación de EF
                var usuarioRequestDto = MapToUsuarioRequestDto(storedToken.Usuario);

                // ✅ STEP 5: Generar nuevos tokens usando servicios de dominio
                var newAccessToken = _tokenService.GenerateToken(usuarioRequestDto);
                var newRefreshTokenValue = _refreshTokenService.GenerateRefreshToken();

                // ✅ STEP 6: Ejecutar la lógica de negocio de rotación de tokens
                await ExecuteTokenRotationAsync(storedToken, newRefreshTokenValue);

                // 🆕 STEP 7: Obtener información de tokens activos
                var activeTokensCount = await _refreshTokenRepository.CountActiveTokensByUserAsync(storedToken.UsuarioId);
                var maxTokens = _refreshTokenService.GetMaxTokensPerUser();
                _logger.LogInformation(
            "Token refresh exitoso para usuario {UserId}. Tokens activos: {Active}/{Max}",
            storedToken.UsuarioId,
            activeTokensCount,
            maxTokens
        );
                return new AuthResponse
                {
                    Token = newAccessToken,
                    RefreshToken = newRefreshTokenValue,
                    ActiveTokensCount = activeTokensCount,
                    MaxTokensAllowed = maxTokens,
                    IsNearLimit = activeTokensCount >= maxTokens - 1,
                    WarningMessage = GenerateWarningMessage(activeTokensCount, maxTokens)
                };
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is UnauthorizedAccessException || ex is SecurityException))
            {
                _logger.LogError(ex, "Error inesperado en refresh token: {Token}", request.RefreshToken);
                throw new InvalidOperationException("Error interno en el sistema de autenticación", ex);
            }
        }
        /// <summary>
        /// Ejecuta la rotación atómica de tokens usando las operaciones básicas del repositorio.
        /// Este método implementa la lógica de negocio específica del dominio de autenticación.
        /// </summary>
        private async Task ExecuteTokenRotationAsync(RefreshToken oldToken, string newRefreshTokenValue)
        {
            // ✅ Aplicar patrón de orquestación de transacciones usando métodos básicos del repositorio

            // 1. Revocar el token anterior
            oldToken.IsRevoked = true;
            oldToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(oldToken);

            // 2. Aplicar política de limpieza de tokens (regla de negocio)
            await _refreshTokenRepository.CleanupUserTokensAsync(oldToken.UsuarioId);

            // 3. Crear y persistir el nuevo token
            var newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenValue,
                Expiration = DateTime.UtcNow.Add(_refreshTokenService.GetTokenLifetime()),
                Created = DateTime.UtcNow,
                CreatedByIp = _httpContextService.GetClientIpAddress(),
                UsuarioId = oldToken.UsuarioId,
                IsRevoked = false
            };

            await _refreshTokenRepository.SaveAsync(newRefreshToken);

            // 4. Aplicar política de limitación de tokens activos (regla de negocio empresarial)
            var maxTokens = _refreshTokenService.GetMaxTokensPerUser();
            await _refreshTokenRepository.LimitActiveTokensAsync(oldToken.UsuarioId, maxTokens);
        }

        /// <summary>
        /// Mapea la entidad Usuario a DTO de request
        /// En un sistema real, usarías AutoMapper o similar
        /// </summary>
        private UsuarioAURequestDTO MapToUsuarioRequestDto(UsuarioAU usuario)
        {
            return new UsuarioAURequestDTO
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol
            };
        }
        // 🆕 Método auxiliar para generar mensajes
        private string? GenerateWarningMessage(int activeTokens, int maxTokens)
        {
            if (activeTokens >= maxTokens)
            {
                return "Has alcanzado el límite máximo de dispositivos conectados. Se ha cerrado sesión en el dispositivo más antiguo.";
            }
            else if (activeTokens >= maxTokens - 1)
            {
                return $"Estás cerca del límite de dispositivos ({activeTokens}/{maxTokens}). El próximo inicio de sesión cerrará la sesión más antigua.";
            }
            else if (activeTokens >= maxTokens - 2)
            {
                return $"Tienes {activeTokens} de {maxTokens} dispositivos conectados.";
            }

            return null;
        }
    }
}

