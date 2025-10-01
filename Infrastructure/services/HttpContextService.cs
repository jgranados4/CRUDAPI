using System.Security.Claims;

namespace CRUDAPI.Infrastructure.services
{
    public interface IHttpContextService
    {
        string GetClientIpAddress();
        string GetUserAgent();
        string GetRequestId();
        int GetCurrentUserId();
        string GetCurrentUserRole();
        string GetCurrentUserEmail();
        bool IsUserAuthenticated();
        ClaimsPrincipal GetCurrentUser();
    }
    public class HttpContextService : IHttpContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<HttpContextService> _logger;
        public HttpContextService(IHttpContextAccessor httpContextAccessor, ILogger<HttpContextService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public string GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "unknown";

            return context.Connection.RemoteIpAddress?.ToString() ??
                   context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                   context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                   "unknown";
        }

        public string GetUserAgent()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";
        }

        public string GetRequestId()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.TraceIdentifier ?? Guid.NewGuid().ToString();
        }
        public int GetCurrentUserId()
        {
            try
            {
                // Extrae el ID del usuario desde los claims del token JWT
                // Este claim debe ser establecido durante el proceso de autenticación
                var context = _httpContextAccessor.HttpContext;
                var userIdClaim = context?.User?.FindFirst(ClaimTypes.NameIdentifier)
                                 ?? context?.User?.FindFirst("sub")
                                 ?? context?.User?.FindFirst("userId");

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }

                // Registra intentos de acceso sin autenticación para análisis de seguridad
                _logger.LogWarning("Intento de obtener ID de usuario sin autenticación válida desde IP: {IP}",
                    GetClientIpAddress());

                throw new UnauthorizedAccessException("Usuario no autenticado o ID inválido");
            }
            catch (Exception ex) when (!(ex is UnauthorizedAccessException))
            {
                _logger.LogError(ex, "Error obteniendo ID del usuario actual");
                throw;
            }
        }

        public string GetCurrentUserRole()
        {
            try
            {
                // Obtiene el rol del usuario desde los claims para validaciones de autorización
                // Fundamental para implementar control de acceso basado en roles (RBAC)
                var context = _httpContextAccessor.HttpContext;
                var roleClaim = context?.User?.FindFirst(ClaimTypes.Role)
                               ?? context?.User?.FindFirst("role");

                if (!string.IsNullOrEmpty(roleClaim?.Value))
                {
                    return roleClaim.Value;
                }

                _logger.LogWarning("Intento de acceso sin rol válido desde IP: {IP}", GetClientIpAddress());
                throw new UnauthorizedAccessException("Rol de usuario no disponible");
            }
            catch (Exception ex) when (!(ex is UnauthorizedAccessException))
            {
                _logger.LogError(ex, "Error obteniendo rol del usuario actual");
                throw;
            }
        }

        public string GetCurrentUserEmail()
        {
            try
            {
                // Extrae el email del usuario para operaciones que requieren identificación
                var context = _httpContextAccessor.HttpContext;
                var emailClaim = context?.User?.FindFirst(ClaimTypes.Email)
                                ?? context?.User?.FindFirst("email");

                if (!string.IsNullOrEmpty(emailClaim?.Value))
                {
                    return emailClaim.Value;
                }

                _logger.LogWarning("Email de usuario no disponible para solicitud desde IP: {IP}",
                    GetClientIpAddress());

                throw new UnauthorizedAccessException("Email de usuario no disponible");
            }
            catch (Exception ex) when (!(ex is UnauthorizedAccessException))
            {
                _logger.LogError(ex, "Error obteniendo email del usuario actual");
                throw;
            }
        }

        public bool IsUserAuthenticated()
        {
            try
            {
                // Verifica el estado de autenticación del usuario actual
                // Método utilitario para validaciones rápidas de acceso
                var context = _httpContextAccessor.HttpContext;
                return context?.User?.Identity?.IsAuthenticated == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando estado de autenticación");
                return false;
            }
        }

        public ClaimsPrincipal GetCurrentUser()
        {
            try
            {
                // Proporciona acceso completo al principal del usuario para casos avanzados
                // Permite acceso a todos los claims sin filtros específicos
                var context = _httpContextAccessor.HttpContext;

                if (context?.User != null)
                {
                    return context.User;
                }

                throw new UnauthorizedAccessException("Usuario no disponible en el contexto actual");
            }
            catch (Exception ex) when (!(ex is UnauthorizedAccessException))
            {
                _logger.LogError(ex, "Error obteniendo usuario actual del contexto");
                throw;
            }
        }
    }
}
