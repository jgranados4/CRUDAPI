using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;

namespace CRUDAPI.Application.UseCases
{
    public class RevokeTokenUseCase
    {
        private readonly IRefreshTokenRepository _repository;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<RevokeTokenUseCase> _logger;
        public RevokeTokenUseCase(
           IRefreshTokenRepository repository,
           ILogger<RevokeTokenUseCase> logger,
            IRefreshTokenService _refreshTokenService)
        {
            _repository = repository;
            _logger = logger;
            this._refreshTokenService = _refreshTokenService;
        }
        public async Task<bool> ExecuteAsync(string tokenValue)
        {
            try
            {
                // ✅ STEP 1: Validación de entrada en capa de aplicación
                if (string.IsNullOrWhiteSpace(tokenValue))
                {
                    _logger.LogWarning("Intento de revocación con token vacío o nulo");
                    return false;
                }

                // ✅ STEP 2: Búsqueda usando operación básica del repositorio
                // Principio: Composición de operaciones básicas para crear funcionalidad compleja
                var token = await _repository.GetByTokenAsync(tokenValue);

                if (token == null)
                {
                    _logger.LogInformation("Token no encontrado para revocación: {TokenPrefix}",
                        GetTokenPrefix(tokenValue));
                    return false;
                }

                // ✅ STEP 3: Aplicación de reglas de negocio del dominio
                // Usar el servicio de dominio para determinar si el token es válido para revocación
                if (token.IsRevoked)
                {
                    _logger.LogInformation("Token ya revocado previamente: {TokenId}", token.Id);
                    return true; // Idempotencia: Ya está revocado, operación "exitosa"
                }

                // ✅ STEP 4: Aplicar lógica de negocio de revocación
                await ExecuteTokenRevocationBusinessLogic(token);

                _logger.LogInformation("Token revocado exitosamente: {TokenId} para usuario {UserId}",
                    token.Id, token.UsuarioId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado revocando token: {TokenPrefix}",
                    GetTokenPrefix(tokenValue));
                throw new InvalidOperationException("Error interno en el sistema de revocación de tokens", ex);
            }
        }
        private async Task ExecuteTokenRevocationBusinessLogic(RefreshToken token)
        {
            // 1. Aplicar marca de revocación (usando reglas de negocio del dominio)
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;

            // 2. Persistir cambios usando operación básica del repositorio
            await _repository.UpdateAsync(token);

            // 3. Aplicar política empresarial opcional: Cleanup de tokens relacionados
            // Esta es una regla de negocio que puede variar según la empresa
            if (_refreshTokenService.GetMaxTokensPerUser() > 0)
            {
                await _repository.CleanupUserTokensAsync(token.UsuarioId);
            }

            // 4. [FUTURO] Aquí se pueden agregar más reglas de negocio:
            // - Notificación al usuario por email/SMS
            // - Registro en log de auditoría de seguridad
            // - Actualización de métricas de seguridad en tiempo real
            // - Integración con sistemas de detección de fraudes

            _logger.LogDebug("Lógica de revocación completada para token {TokenId}", token.Id);
        }

        /// <summary>
        /// Obtiene un prefijo seguro del token para logging sin exponer información sensible.
        /// 
        /// PRINCIPIO DE SEGURIDAD: Never log complete tokens
        /// Los tokens completos nunca deben aparecer en logs por razones de seguridad.
        /// Solo se registra información suficiente para debugging sin comprometer la seguridad.
        /// 
        /// COMPLIANCE: Cumple con estándares como PCI-DSS que prohíben
        /// el logging de información sensible de autenticación.
        /// </summary>
        private static string GetTokenPrefix(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length < 8)
                return "[INVALID]";

            return $"{token[..4]}****{token[^2..]}";
        }
    }
}
