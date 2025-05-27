using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using CRUDAPI.Infrastructure.services;

namespace CRUDAPI.Application.UseCases
{
    public class DecodeTokenUseCase
    {
        private readonly ITokenService _tokenService;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IHttpContextService _httpContextService;
        private readonly ILogger<DecodeTokenUseCase> _logger;

        public DecodeTokenUseCase(
            ITokenService tokenService,
            IUsuarioRepository usuarioRepository,
            IHttpContextService httpContextService,
            ILogger<DecodeTokenUseCase> logger)
        {
            _tokenService = tokenService;
            _usuarioRepository = usuarioRepository;
            _httpContextService = httpContextService;
            _logger = logger;
        }

        public async Task<DecodeTokenResponseDTO> ExecuteAsync(DecodeTokenRequestDTO request)
        {
            // Registra el inicio de la operación de decodificación con contexto de seguridad
            // Incluye IP del cliente para rastrear patrones de uso y detectar potenciales amenazas
            var clientIp = _httpContextService.GetClientIpAddress();
            _logger.LogInformation("Iniciando decodificación de token desde IP: {ClientIp}", clientIp);

            try
            {
                // Valida que el token proporcionado tenga formato básico correcto
                // Esta validación previa previene procesamiento innecesario de tokens malformados
                if (!ValidateTokenFormat(request.Token))
                {
                    return CreateErrorResponse("Formato de token inválido");
                }

                // Verifica que el token sea técnicamente válido antes de proceder con decodificación
                // Esta validación incluye verificación de firma y estructura JWT
                if (!_tokenService.ValidarToken(request.Token))
                {
                    return CreateErrorResponse("Token inválido o expirado");
                }

                // Decodifica el token para extraer información de claims básicos
                // Esta operación extrae información sin verificar estado del usuario en base de datos
                var decodedTokenInfo = _tokenService.DecodeToken(request.Token);
                if (decodedTokenInfo == null)
                {
                    return CreateErrorResponse("No se pudo decodificar el contenido del token");
                }

                // Construye la respuesta base con información disponible del token
                var response = await BuildBaseResponseAsync(decodedTokenInfo, request);

                // Enriquece la respuesta con información adicional del usuario si se solicita
                // Esta operación requiere consulta a base de datos y se ejecuta condicionalmente
                if (request.IncludeUserDetails)
                {
                    await EnrichWithUserDetailsAsync(response, decodedTokenInfo.Id);
                }

                // Agrega metadatos técnicos del token si se requieren
                // Los metadatos incluyen información sobre algoritmo, tipo y contexto de creación
                if (request.IncludeMetadata)
                {
                    await AddTokenMetadataAsync(response, request.Token);
                }

                _logger.LogInformation("Token decodificado exitosamente para usuario: {UserId}", decodedTokenInfo.Id);
                return response;
            }
            catch (ArgumentException ex)
            {
                // Registra errores de parámetros inválidos que pueden indicar uso incorrecto de la API
                _logger.LogWarning("Parámetros inválidos en decodificación de token desde {ClientIp}: {Message}",
                    clientIp, ex.Message);
                return CreateErrorResponse($"Parámetros inválidos: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                // Los errores de autorización pueden indicar tokens comprometidos o ataques
                _logger.LogWarning("Error de autorización en decodificación desde {ClientIp}: {Message}",
                    clientIp, ex.Message);
                return CreateErrorResponse("Token no autorizado");
            }
            catch (Exception ex)
            {
                // Errores técnicos inesperados requieren investigación y no deben exponer detalles internos
                _logger.LogError(ex, "Error crítico en decodificación de token desde {ClientIp}", clientIp);
                return CreateErrorResponse("Error interno procesando el token");
            }
        }

        public async Task<TokenClaims?> ExecuteQuickDecodeAsync(string token)
        {
            try
            {
                // Decodificación rápida optimizada para casos donde solo se necesitan claims básicos
                // Omite validaciones costosas y consultas a base de datos para máximo rendimiento
                if (!ValidateTokenFormat(token) || !_tokenService.ValidarToken(token))
                {
                    return null;
                }

                // Extrae únicamente la información esencial del token sin enriquecer con datos adicionales
                var decodedInfo = _tokenService.DecodeToken(token);
                if (decodedInfo == null) return null;
                // Verifica si el token ya expiró antes de continuar
                if (decodedInfo.Expiracion.HasValue && decodedInfo.Expiracion.Value <= DateTime.UtcNow)
                {
                    _logger.LogDebug("Token expirado en decodificación rápida");
                    return null;
                }

                // Mapea la información decodificada a la estructura de claims estandarizada
                return MapToTokenClaims(decodedInfo);
            }
            catch (Exception ex)
            {
                // En decodificación rápida, los errores se registran como debug para evitar spam en logs
                _logger.LogDebug(ex, "Error en decodificación rápida de token");
                return null;
            }
        }

        private bool ValidateTokenFormat(string token)
        {
            // Valida que el token no esté vacío y tenga longitud mínima razonable
            // Esta validación básica previene procesamiento de strings claramente inválidos
            if (string.IsNullOrWhiteSpace(token) || token.Length < 20)
            {
                return false;
            }

            // Verifica estructura básica de JWT (tres partes separadas por puntos)
            // Esta verificación previa evita intentos de decodificación de tokens malformados
            var parts = token.Split('.');
            return parts.Length == 3 && parts.All(part => !string.IsNullOrWhiteSpace(part));
        }

        private async Task<DecodeTokenResponseDTO> BuildBaseResponseAsync(UsuarioResponse decodedInfo, DecodeTokenRequestDTO request)
        {
            // Calcula información temporal del token basada en su fecha de expiración
            var now = DateTime.UtcNow;
            var isExpired = decodedInfo.Expiracion.HasValue && now > decodedInfo.Expiracion.Value;
            var timeUntilExpiration = decodedInfo.Expiracion?.Subtract(now);

            // Valida expiración si se solicita y retorna error si está expirado
            if (request.ValidateExpiration && isExpired)
            {
                return CreateErrorResponse("El token ha expirado");
            }

            // Construye la estructura base de respuesta con información extraída del token
            return new DecodeTokenResponseDTO
            {
                IsSuccessful = true,
                Claims = MapToTokenClaims(decodedInfo),
                ExpiresAt = decodedInfo.Expiracion,
                TimeUntilExpiration = timeUntilExpiration,
                IsExpired = isExpired
            };
        }

        private async Task EnrichWithUserDetailsAsync(DecodeTokenResponseDTO response, int userId)
        {
            try
            {
                // Obtiene información completa del usuario desde el repositorio
                // Esta consulta adicional proporciona datos actualizados que pueden no estar en el token
                var user = await _usuarioRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    // Enriquece la respuesta con detalles actuales del usuario
                    response.UserDetails = new TokenUserDetails
                    {
                        Nombre = user.Nombre ?? string.Empty,
                        IsActive = true, // En implementación completa verificaría campo IsActive
                        RequiresPasswordChange = false // En implementación completa verificaría política de contraseñas
                    };
                }
                else
                {
                    // Si el usuario no existe, marca la respuesta como fallida
                    // Esto puede indicar que el token es válido pero el usuario fue eliminado
                    response.IsSuccessful = false;
                    response.ErrorMessage = "Usuario asociado al token no encontrado";
                }
            }
            catch (Exception ex)
            {
                // Los errores al obtener detalles del usuario no invalidan el token
                // Registra el error pero mantiene la respuesta base válida
                _logger.LogWarning(ex, "Error obteniendo detalles del usuario {UserId} para token", userId);
                response.UserDetails = new TokenUserDetails
                {
                    Nombre = "Usuario no disponible",
                    IsActive = false
                };
            }
        }

        private async Task AddTokenMetadataAsync(DecodeTokenResponseDTO response, string token)
        {
            try
            {
                // Construye metadatos técnicos del token para análisis avanzado
                // Esta información es útil para debugging y auditoría de tokens
                var metadata = new TokenMetadata
                {
                    TokenType = "JWT",
                    Algorithm = "HS256", // En implementación completa extraería del header JWT
                    ClientIp = _httpContextService.GetClientIpAddress(),
                    UserAgent = _httpContextService.GetUserAgent()
                };

                // Agrega información adicional específica del contexto de la petición
                metadata.AdditionalData["RequestId"] = _httpContextService.GetRequestId();
                metadata.AdditionalData["DecodedAt"] = DateTime.UtcNow;

                response.Metadata = metadata;
            }
            catch (Exception ex)
            {
                // Los errores al generar metadatos no afectan la funcionalidad principal
                _logger.LogDebug(ex, "Error generando metadatos del token");
            }
        }

        private TokenClaims MapToTokenClaims(UsuarioResponse decodedInfo)
        {
            // Calcula el tiempo restante si hay fecha de expiración
            double tiempoRestanteSegundos = 0;
            if (decodedInfo.Expiracion.HasValue)
            {
                var tiempoRestante = decodedInfo.Expiracion.Value - DateTime.UtcNow;
                tiempoRestanteSegundos = tiempoRestante.TotalSeconds > 0 ? tiempoRestante.TotalSeconds : 0;
            }
            // Mapea la información decodificada del token a la estructura estandarizada de claims
            // Esta transformación asegura consistencia en la respuesta independiente del formato interno
            return new TokenClaims
            {
                UserId = decodedInfo.Id,
                Nombre=decodedInfo.Nombre,
                Email = decodedInfo.Email ?? string.Empty,
                Role = decodedInfo.Rol ?? string.Empty,
                Expiracion = decodedInfo.Expiracion,
                tiempoRestante = tiempoRestanteSegundos
                // En implementación completa extraería claims adicionales como permisos, issuer, audience
            };
        }

        private DecodeTokenResponseDTO CreateErrorResponse(string errorMessage)
        {
            // Construye respuesta estandarizada para errores manteniendo estructura consistente
            // No expone detalles técnicos internos que podrían comprometer la seguridad
            return new DecodeTokenResponseDTO
            {
                IsSuccessful = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
