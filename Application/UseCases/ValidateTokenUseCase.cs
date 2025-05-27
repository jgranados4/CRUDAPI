using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using CRUDAPI.Infrastructure.services;
using static CRUDAPI.Application.Dtos.TokenValidationResponseDTO;

namespace CRUDAPI.Application.UseCases
{
    public class ValidateTokenUseCase
    {
        private readonly ITokenService _tokenService;
        private readonly ITokenValidationService _validationService;
        private readonly IHttpContextService _httpContextService;
        private readonly ILogger<ValidateTokenUseCase> _logger;
        private readonly IUsuarioRepository usuarioRepository;

        public ValidateTokenUseCase(
            ITokenService tokenService,
            ITokenValidationService validationService,
            IHttpContextService httpContextService,
            IUsuarioRepository usuarioRepository,
            ILogger<ValidateTokenUseCase> logger)
        {
            _tokenService = tokenService;
            _validationService = validationService;
            _httpContextService = httpContextService;
            _logger = logger;
            this.usuarioRepository = usuarioRepository;
        }

        /// <summary>
        /// Valida un token según criterios específicos y devuelve información detallada
        /// Implementa validación multi-capa con logging comprehensivo
        /// </summary>
        public async Task<TokenValidationResponseDTO> ExecuteAsync(TokenValidationRequestDTO request)
        {
            // Registra intento de validación con información contextual
            // Incluye IP para detección de patrones de abuso
            var clientIp = _httpContextService.GetClientIpAddress();
            _logger.LogInformation("Iniciando validación de token desde IP: {ClientIp}", clientIp);

            try
            {
                // Valida parámetros de entrada antes de procesar
                ValidateInputParameters(request);

                // Ejecuta validación según el tipo de token especificado
                var response = request.TokenType switch
                {
                    TokenType.JWT => await ValidateJwtTokenAsync(request),
                    TokenType.RefreshToken => await ValidateRefreshTokenAsync(request),
                    _ => throw new ArgumentException($"Tipo de token no soportado: {request.TokenType}")
                };

                // Registra resultado de validación para auditoría
                await _validationService.LogTokenValidationAttemptAsync(
                    request.Token,
                    response.IsValid,
                    response.ValidationFailureReason);

                return response;
            }
            catch (ArgumentException ex)
            {
                // Errores de parámetros inválidos se registran como warnings
                _logger.LogWarning("Parámetros inválidos en validación de token desde {ClientIp}: {Message}",
                    clientIp, ex.Message);

                return CreateInvalidTokenResponse("Parámetros de validación inválidos");
            }
            catch (Exception ex)
            {
                // Errores técnicos se registran como críticos
                _logger.LogError(ex, "Error crítico en validación de token desde {ClientIp}", clientIp);

                return CreateInvalidTokenResponse("Error interno en validación de token");
            }
        }

        /// <summary>
        /// Validación rápida de token para casos donde solo se necesita confirmación básica
        /// Optimizada para verificaciones frecuentes con mínima sobrecarga
        /// </summary>
        public async Task<bool> ExecuteQuickValidationAsync(string token)
        {
            try
            {
                // Validación básica de formato sin logging extenso
                if (string.IsNullOrWhiteSpace(token))
                {
                    return false;
                }

                // Intenta validar como JWT primero (más común)
                var isValidJwt = _tokenService.ValidarToken(token);

                if (isValidJwt)
                {
                    // Verificación adicional de blacklist para tokens válidos
                    var isBlacklisted = await _validationService.IsTokenBlacklistedAsync(token);
                    return !isBlacklisted;
                }

                return false;
            }
            catch (Exception ex)
            {
                // En validación rápida, errores resultan en token inválido
                _logger.LogDebug(ex, "Error en validación rápida de token");
                return false;
            }
        }

        private async Task<TokenValidationResponseDTO> ValidateJwtTokenAsync(TokenValidationRequestDTO request)
        {
            try
            {
                // Validación básica de formato JWT usando el servicio de dominio
                var isValidFormat = _tokenService.ValidarToken(request.Token);

                if (!isValidFormat)
                {
                    return CreateInvalidTokenResponse("Formato de JWT inválido o token expirado");
                }

                // Decodifica el token para extraer información del usuario
                var decodedToken = _tokenService.DecodeToken(request.Token);

                if (decodedToken == null)
                {
                    return CreateInvalidTokenResponse("No se pudo decodificar el token JWT");
                }
                // ✅ SOLUCIÓN: Obtener la entidad completa del usuario desde el repositorio
                // El token decodificado solo proporciona información básica, necesitamos la entidad completa
                var user = await usuarioRepository.GetByIdAsync(decodedToken.Id);
                if (user == null)
                {
                    return CreateInvalidTokenResponse("Usuario asociado al token no existe");
                }

                // Validaciones adicionales de seguridad empresarial
                if (request.CheckRevocation)
                {
                    var isBlacklisted = await _validationService.IsTokenBlacklistedAsync(request.Token);
                    if (isBlacklisted)
                    {
                        return CreateInvalidTokenResponse("Token está en lista negra");
                    }
                }

                // Verifica que el usuario asociado siga activo en el sistema
                var isUserActive = await _validationService.IsUserActiveAsync(decodedToken.Id);
                if (!isUserActive)
                {
                    return CreateInvalidTokenResponse("Usuario asociado al token está inactivo");
                }

                // Verifica si el usuario requiere cambio de contraseña
                var requiresPasswordChange = await _validationService.RequiresPasswordChangeAsync(decodedToken.Id);
                if (requiresPasswordChange)
                {
                    return CreateInvalidTokenResponse("Usuario requiere cambio de contraseña");
                }

                // ✅ CORRECCIÓN: Ahora usa la entidad UsuarioAU correcta
                return await CreateValidTokenResponseAsync(user, request.Token, decodedToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando JWT token");
                return CreateInvalidTokenResponse("Error procesando token JWT");
            }
        }

        private async Task<TokenValidationResponseDTO> ValidateRefreshTokenAsync(TokenValidationRequestDTO request)
        {
            try
            {
                // Validación específica para refresh tokens requiere consulta a base de datos
                var isRevoked = await _validationService.IsTokenRevokedAsync(request.Token);

                if (request.CheckRevocation && isRevoked)
                {
                    return CreateInvalidTokenResponse("Refresh token ha sido revocado");
                }

                // Extrae metadata específica del refresh token
                var metadata = await _validationService.ExtractTokenMetadataAsync(request.Token);

                // Verifica expiración si se solicita
                if (request.CheckExpiration && metadata.ContainsKey("ExpiresAt"))
                {
                    var expiresAt = (DateTime)metadata["ExpiresAt"];
                    if (DateTime.UtcNow > expiresAt)
                    {
                        return CreateInvalidTokenResponse("Refresh token ha expirado");
                    }
                }

                // Para refresh tokens válidos, no se expone información del usuario por seguridad
                return new TokenValidationResponseDTO
                {
                    IsValid = true,
                    Metadata = metadata,
                    ExpiresAt = metadata.ContainsKey("ExpiresAt") ? (DateTime?)metadata["ExpiresAt"] : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando refresh token");
                return CreateInvalidTokenResponse("Error procesando refresh token");
            }
        }

        private static void ValidateInputParameters(TokenValidationRequestDTO request)
        {
            // Validación de token no vacío
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                throw new ArgumentException("El token no puede estar vacío", nameof(request.Token));
            }

            // Validación de longitud mínima para prevenir ataques
            if (request.Token.Length < 10)
            {
                throw new ArgumentException("El token es demasiado corto para ser válido", nameof(request.Token));
            }

            // Validación de caracteres permitidos (base64url para JWT)
            if (request.TokenType == TokenType.JWT && !IsValidJwtFormat(request.Token))
            {
                throw new ArgumentException("El formato del token JWT no es válido", nameof(request.Token));
            }
        }

        private static bool IsValidJwtFormat(string token)
        {
            // Verifica formato básico de JWT (3 partes separadas por puntos)
            var parts = token.Split('.');
            return parts.Length == 3 && parts.All(part => !string.IsNullOrWhiteSpace(part));
        }

        private async Task<TokenValidationResponseDTO> CreateValidTokenResponseAsync(UsuarioAU user, string token, UsuarioResponse? decodedTokenInfo = null)
        {
            try
            {
                // Construye respuesta completa para tokens válidos
                var metadata = await _validationService.ExtractTokenMetadataAsync(token);

                // Si tenemos información del token decodificado, la incluimos
                DateTime? expiresAt = null;
                TimeSpan? timeUntilExpiration = null;

                if (decodedTokenInfo != null && decodedTokenInfo.Expiracion.HasValue)
                {
                    expiresAt = decodedTokenInfo.Expiracion.Value;
                    timeUntilExpiration = expiresAt.Value.Subtract(DateTime.UtcNow);

                    // Si el tiempo es negativo, el token ya expiró
                    if (timeUntilExpiration.Value.TotalSeconds <= 0)
                    {
                        return CreateInvalidTokenResponse("Token JWT ha expirado");
                    }
                }

                return new TokenValidationResponseDTO
                {
                    IsValid = true,
                    UserInfo = new TokenUserInfo
                    {
                        UserId = user.Id,
                        Email = user.Email ?? string.Empty,
                        Role = user.Rol ?? string.Empty
                    },
                    ExpiresAt = expiresAt,
                    TimeUntilExpiration = timeUntilExpiration,
                    Metadata = metadata
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando respuesta de token válido");
                return CreateInvalidTokenResponse("Error procesando información del token");
            }
        }

        private static TokenValidationResponseDTO CreateInvalidTokenResponse(string reason)
        {
            // Respuesta estandarizada para tokens inválidos
            return new TokenValidationResponseDTO
            {
                IsValid = false,
                ValidationFailureReason = reason
            };
        }
    }
}
