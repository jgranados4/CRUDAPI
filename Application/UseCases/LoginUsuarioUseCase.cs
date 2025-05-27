using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using CRUDAPI.Infrastructure.repositories;
using CRUDAPI.Infrastructure.Security;
using CRUDAPI.Infrastructure.services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyModel;
using NuGet.Protocol.Plugins;
using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Application.UseCases
{
    public class LoginUsuarioUseCase
    {
        // ✅ Solo abstracciones - Dependency Inversion Principle
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IRefreshTokenRepository _tokenRepository;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ITokenService _jwtTokenService;
        private readonly IPasswordHashingService _hashingService;
        private readonly IHttpContextService _httpContextService;
        private readonly CleanupUserTokensUseCase _cleanupUseCase;
        private readonly ILogger<LoginUsuarioUseCase> _logger;

        public LoginUsuarioUseCase(
            IUsuarioRepository usuarioRepository,
            IRefreshTokenRepository tokenRepository,
            IRefreshTokenService refreshTokenService,
            ITokenService jwtTokenService,
            IPasswordHashingService hashingService,
            IHttpContextService httpContextService,
            CleanupUserTokensUseCase cleanupUseCase,
            ILogger<LoginUsuarioUseCase> logger)
        {
            _usuarioRepository = usuarioRepository;
            _tokenRepository = tokenRepository;
            _refreshTokenService = refreshTokenService;
            _jwtTokenService = jwtTokenService;
            _hashingService = hashingService;
            _httpContextService = httpContextService;
            _cleanupUseCase = cleanupUseCase;
            _logger = logger;
        }

        public async Task<AuthResponse> ExecuteAsync(LoginRequestDTO request)
        {
            _logger.LogInformation("Iniciando proceso de login para email: {Email}", request.Email);

            try
            {
                // Step 1: Validar credenciales (delegado a método privado)
                var usuario = await ValidateCredentialsAsync(request);
                // Step 2: Verificar si existe un refresh token válido
                var existingToken = await _tokenRepository.GetValidTokenByUserIdAsync(usuario.Id);

                if (existingToken != null)
                {
                    _logger.LogInformation(
                        "Reutilizando refresh token existente para usuario: {UserId}",
                        usuario.Id
                    );

                    // ✅ REUTILIZAR el refresh token existente
                    var response = await ReuseExistingTokenAsync(usuario, existingToken);
                    return response;
                }
                // Step 3: Limpieza de tokens antiguos (delegados a casos de uso especializados)
                await _cleanupUseCase.ExecuteAsync(usuario.Id);

                // Step 4: Generar tokens (delegados a servicios de dominio)
                var authResponse = await GenerateAuthTokensAsync(usuario);

                _logger.LogInformation("Login exitoso para usuario: {UserId}", usuario.Id);
                return authResponse;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Intento de login fallido para email: {Email}. Razón: {Reason}",
                    request.Email, ex.Message);
                throw; // Re-throw for controller handling
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante login para email: {Email}", request.Email);
                throw;
            }
        }
        private async Task<UsuarioAU> ValidateCredentialsAsync(LoginRequestDTO request)
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("El email es requerido");

            if (string.IsNullOrWhiteSpace(request.Constrasena))
                throw new ArgumentException("La contraseña es requerida");

            // Find user through repository abstraction
            var usuario = await _usuarioRepository.GetByEmailAsync(request.Email);
            if (usuario == null)
                throw new UnauthorizedAccessException("Credenciales inválidas");

            // Verify password using domain service
            var isPasswordValid = _hashingService.VerificarClave(request.Constrasena, usuario.Constrasena);
            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Credenciales inválidas");

            return usuario;
        }
        private async Task<AuthResponse> GenerateAuthTokensAsync(UsuarioAU usuario)
        {
            // Create JWT using domain service
            var usuarioRequestDto = MapToRequestDto(usuario);
            var jwtToken = _jwtTokenService.GenerateToken(usuarioRequestDto);

            // Generate refresh token using domain service
            var refreshTokenValue = _refreshTokenService.GenerateRefreshToken();
            var tokenLifetime = _refreshTokenService.GetTokenLifetime();

            // Create refresh token entity with business rules
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshTokenValue,
                Created = DateTime.UtcNow,
                Expiration = DateTime.UtcNow.Add(tokenLifetime),
                UsuarioId = usuario.Id,
                CreatedByIp = _httpContextService.GetClientIpAddress(),
                IsRevoked = false
            };

            // Persist through repository abstraction
            await _tokenRepository.SaveAsync(refreshTokenEntity);

            return new AuthResponse
            {
                Token = jwtToken,
                RefreshToken = refreshTokenValue
            };
        }
        private static UsuarioAURequestDTO MapToRequestDto(UsuarioAU usuario)
        {
            return new UsuarioAURequestDTO
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol
            };
        }
        private async Task<AuthResponse> ReuseExistingTokenAsync(
           UsuarioAU usuario,
           RefreshToken existingToken)
        {
            // Generar SOLO un nuevo JWT (el refresh token se mantiene)
            var usuarioRequestDto = MapToRequestDto(usuario);
            var jwtToken = _jwtTokenService.GenerateToken(usuarioRequestDto);

            // Opcionalmente, actualizar la fecha de último uso del refresh token
            existingToken.LastUsed = DateTime.UtcNow;
            existingToken.LastUsedByIp = _httpContextService.GetClientIpAddress();
            await _tokenRepository.UpdateAsync(existingToken);

            _logger.LogInformation(
                "Token JWT regenerado. Refresh token reutilizado: {TokenId}",
                existingToken.Id
            );

            return new AuthResponse
            {
                Token = jwtToken,
                RefreshToken = existingToken.Token // ✅ Mismo refresh token
            };
        }
    }
}
