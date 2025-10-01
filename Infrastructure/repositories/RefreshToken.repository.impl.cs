using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using CRUDAPI.Domain.entities;
using CRUDAPI.Infrastructure.Persistence.context;
using CRUDAPI.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace CRUDAPI.Infrastructure.repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly BaseContext _context;
        private readonly ILogger<RefreshToken> _logger;
        public RefreshTokenRepository(BaseContext context, ILogger<RefreshToken> logger)
        {
            _logger=logger;
            _context = context;
        }

        public async Task<int> CountActiveTokensByUserAsync(int userId)
        {
            return await _context.RefreshTokens
                .CountAsync(t => t.UsuarioId == userId && !t.IsRevoked);
        }

        public async Task<List<Domain.entities.RefreshToken>> GetActiveTokensByUserAsync(int userId)
        {
            return await _context.RefreshTokens
                .Where(t => t.UsuarioId == userId && !t.IsRevoked)
                .OrderBy(t => t.Created)
                .ToListAsync();
        }

        public async Task<RefreshToken?> GetByIdAsync(int id)
        {
            return await _context.RefreshTokens.FindAsync(id);
        }

        public async  Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.Usuario)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<List<RefreshToken>> GetExpiredRevokedTokensAsync(int userId, DateTime cutoffDate)
        {
            return await _context.RefreshTokens
                .Where(t => t.UsuarioId == userId &&
                           t.IsRevoked &&
                           t.Expiration < cutoffDate)
                .ToListAsync();
        }

        public async  Task<List<RefreshToken>> GetOldestActiveTokensAsync(int userId, int count)
        {
            return await _context.RefreshTokens
                .Where(t => t.UsuarioId == userId && !t.IsRevoked)
                .OrderBy(t => t.Created)
                .Take(count)
                .ToListAsync();
        }

        public async Task RemoveRangeAsync(IEnumerable<RefreshToken> tokens)
        {
            _context.RefreshTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync();
        }

        public async Task SaveAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async  Task UpdateAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }
        /// <summary>
        /// Revoca todos los tokens activos de un usuario específico
        /// Operación crítica para invalidar sesiones en casos de seguridad
        /// </summary>
        public async Task<int> RevokeAllUserTokensAsync(int userId)
        {
            try
            {
                // Obtiene todos los tokens activos del usuario de forma eficiente
                // La consulta se optimiza para evitar carga innecesaria de datos
                var activeTokens = await _context.RefreshTokens
                    .Where(t => t.UsuarioId == userId && !t.IsRevoked)
                    .ToListAsync();

                // Valida si existen tokens para revocar evitando operaciones innecesarias
                if (!activeTokens.Any())
                {
                    _logger.LogInformation("No se encontraron tokens activos para revocar del usuario: {UserId}", userId);
                    return 0;
                }

                // Aplica la revocación con timestamp para auditoría
                // El timestamp permite rastrear cuándo se ejecutó la revocación
                var revocationTime = DateTime.UtcNow;
                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = revocationTime;
                }

                // Persiste los cambios de forma eficiente en una sola operación
                await _context.SaveChangesAsync();

                _logger.LogInformation("Se revocaron {Count} tokens del usuario: {UserId}",
                    activeTokens.Count, userId);

                return activeTokens.Count;
            }
            catch (Exception ex)
            {
                // Registra errores técnicos con contexto completo para debugging
                _logger.LogError(ex, "Error revocando tokens del usuario: {UserId}", userId);
                throw;
            }
        }
        /// <summary>
        /// Ejecuta limpieza completa de tokens para un usuario
        /// Combina múltiples operaciones de limpieza en un flujo coordinado
        /// </summary>
        public async Task CleanupUserTokensAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Iniciando limpieza completa de tokens para usuario: {UserId}", userId);

                // Configuración empresarial para políticas de limpieza
                // Estos valores podrían provenir de configuración externa en sistemas más complejos
                const int maxActiveTokens = 5;    // Límite de sesiones concurrentes
                const int cleanupDays = 30;       // Retención de tokens revocados

                // Ejecuta limpieza en orden específico para mantener integridad
                // Primero limpia tokens antiguos, luego limita activos
                await CleanupExpiredRevokedTokensAsync(userId, cleanupDays);
                await LimitActiveTokensAsync(userId, maxActiveTokens);

                _logger.LogInformation("Limpieza completa finalizada para usuario: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en limpieza completa de tokens para usuario: {UserId}", userId);
                throw;
            }
        }
        /// <summary>
        /// Elimina tokens revocados expirados según política de retención
        /// Optimiza espacio de almacenamiento manteniendo requisitos de auditoría
        /// </summary>
        public async Task CleanupExpiredRevokedTokensAsync(int userId, int cleanupDays)
        {
            try
            {
                // Calcula fecha límite para retención de tokens revocados
                // Los tokens más antiguos que esta fecha son candidatos para eliminación
                var cutoffDate = DateTime.UtcNow.AddDays(-cleanupDays);

                // Consulta optimizada que combina múltiples filtros para eficiencia
                var expiredTokens = await _context.RefreshTokens
                    .Where(t => t.UsuarioId == userId &&
                               t.IsRevoked &&
                               t.Expiration < cutoffDate)
                    .ToListAsync();

                // Solo procede si hay tokens para limpiar, evitando operaciones innecesarias
                if (!expiredTokens.Any())
                {
                    _logger.LogDebug("No hay tokens expirados para limpiar del usuario: {UserId}", userId);
                    return;
                }

                // Elimina tokens en bloque para eficiencia
                _context.RefreshTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Eliminados {Count} tokens expirados del usuario: {UserId}",
                    expiredTokens.Count, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error limpiando tokens expirados del usuario: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Limita la cantidad de tokens activos por usuario según política de seguridad
        /// Previene acumulación excesiva de sesiones activas
        /// </summary>
        public async Task LimitActiveTokensAsync(int userId, int maxTokens)
        {
            try
            {
                // Cuenta tokens activos antes de aplicar limitaciones
                var activeCount = await CountActiveTokensByUserAsync(userId);

                // Solo aplica limitación si se excede el máximo permitido
                if (activeCount <= maxTokens)
                {
                    _logger.LogDebug("Usuario {UserId} tiene {Count} tokens activos, dentro del límite de {Max}",
                        userId, activeCount, maxTokens);
                    return;
                }

                // Calcula cuántos tokens deben eliminarse para cumplir la política
                var tokensToRemoveCount = activeCount - maxTokens;

                // Obtiene los tokens más antiguos para eliminación (FIFO policy)
                var oldestTokens = await GetOldestActiveTokensAsync(userId, tokensToRemoveCount);

                // Elimina tokens excedentes manteniendo los más recientes
                _context.RefreshTokens.RemoveRange(oldestTokens);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Eliminados {Count} tokens antiguos del usuario: {UserId} para cumplir límite de {Max}",
                    oldestTokens.Count, userId, maxTokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error limitando tokens activos del usuario: {UserId}", userId);
                throw;
            }
        }
        public async Task<RefreshToken?> GetValidTokenByUserIdAsync(int userId)
        {
            try
            {
                _logger.LogDebug("Buscando token válido para usuario: {UserId}", userId);

                var validToken = await _context.RefreshTokens
                    .Include(rt => rt.Usuario) // Incluir navegación para usar en UseCase
                    .Where(rt =>
                        rt.UsuarioId == userId &&
                        !rt.IsRevoked &&
                        rt.Expiration > DateTime.UtcNow
                    )
                    .OrderByDescending(rt => rt.Created) // El más reciente primero
                    .FirstOrDefaultAsync();

                if (validToken != null)
                {
                    _logger.LogInformation("Token válido encontrado para usuario: {UserId}", userId);
                }
                else
                {
                    _logger.LogDebug("No se encontró token válido para usuario: {UserId}", userId);
                }

                return validToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando token válido para usuario: {UserId}", userId);
                throw;
            }
        }
    }
}
