using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;
using CRUDAPI.Infrastructure.services;

namespace CRUDAPI.Application.UseCases
{
    public class DeleteUsuarioUseCase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUsuarioDeletionService _deletionService;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly IHttpContextService _httpContextService;
        private readonly ILogger<DeleteUsuarioUseCase> _logger;

        public DeleteUsuarioUseCase(
            IUsuarioRepository usuarioRepository,
            IUsuarioDeletionService deletionService,
            IPasswordHashingService passwordHashingService,
            IHttpContextService httpContextService,
            ILogger<DeleteUsuarioUseCase> logger)
        {
            _usuarioRepository = usuarioRepository;
            _deletionService = deletionService;
            _passwordHashingService = passwordHashingService;
            _httpContextService = httpContextService;
            _logger = logger;
        }

        public async Task<DeleteUsuarioResponseDTO> ExecuteAsync(int id)
        {
            // Registra el inicio de la operación crítica de eliminación
            // Las eliminaciones requieren trazabilidad completa para auditorías de seguridad
            _logger.LogWarning("Iniciando eliminación de usuario ID: {UsuarioId}", id);

            try
            {
                // Valida que el ID sea un valor empresarialmente válido
                // Previene operaciones contra identificadores malformados o negativos
                if (id <= 0)
                {
                    throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(id));
                }

                // Obtiene la entidad completa antes de proceder con validaciones
                // La eliminación requiere validar el estado actual del usuario
                var userToDelete = await _usuarioRepository.GetByIdAsync(id);
                if (userToDelete == null)
                {
                    _logger.LogWarning("Intento de eliminación de usuario inexistente: {UsuarioId}", id);
                    throw new KeyNotFoundException($"Usuario con ID {id} no encontrado");
                }

                // Ejecuta todas las validaciones de reglas de negocio antes de proceder
                // Las validaciones previenen eliminaciones que comprometan la integridad del sistema
                await ExecuteBusinessValidationsAsync(id, userToDelete.Email);

                // Realiza limpieza de datos relacionados antes de la eliminación principal
                // Garantiza que no queden referencias órfanas que comprometan la integridad referencial
                await _deletionService.HandlePreDeletionCleanupAsync(id);

                // Ejecuta la eliminación física a través del repositorio
                // El repositorio encapsula los detalles técnicos de la eliminación en la base de datos
                await _usuarioRepository.DeleteAsync(id);

                // Construye la respuesta de confirmación con metadatos de auditoría
                // Proporciona información completa sobre la operación ejecutada
                var response = CreateDeletionResponse(userToDelete);

                _logger.LogWarning("Usuario eliminado exitosamente: {UsuarioId}, Email: {Email}", id, userToDelete.Email);
                return response;
            }
            catch (ArgumentException ex)
            {
                // Registra errores de validación de entrada para análisis de patrones de uso
                _logger.LogWarning("Parámetros inválidos en eliminación de usuario {UsuarioId}: {Message}", id, ex.Message);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                // Los errores de autorización en eliminaciones son críticos para seguridad
                // Pueden indicar intentos de escalación de privilegios o ataques
                _logger.LogError("Acceso no autorizado en eliminación de usuario {UsuarioId}: {Message}", id, ex.Message);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                // Registra violaciones de reglas de negocio que impiden la eliminación
                // Útil para identificar intentos de eliminación de usuarios críticos
                _logger.LogWarning("Operación de eliminación bloqueada por reglas de negocio {UsuarioId}: {Message}", id, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Errores técnicos en eliminaciones requieren atención inmediata
                // La pérdida de datos accidental es un riesgo crítico empresarial
                _logger.LogError(ex, "Error crítico en eliminación de usuario {UsuarioId}", id);
                throw;
            }
        }

        public async Task<DeleteUsuarioResponseDTO> ExecuteSoftDeleteAsync(int id, string reason = null)
        {
            // Registra el inicio de eliminación lógica como alternativa segura
            // La eliminación lógica permite recuperación posterior si es necesario
            _logger.LogInformation("Iniciando eliminación lógica de usuario ID: {UsuarioId}", id);

            try
            {
                // Valida parámetros de entrada con las mismas reglas que eliminación física
                if (id <= 0)
                {
                    throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(id));
                }

                // Obtiene el usuario objetivo y valida su existencia
                var userToDelete = await _usuarioRepository.GetByIdAsync(id);
                if (userToDelete == null)
                {
                    throw new KeyNotFoundException($"Usuario con ID {id} no encontrado");
                }

                // Aplica las mismas validaciones de negocio que la eliminación física
                // La eliminación lógica sigue siendo una operación crítica que requiere validación
                await ExecuteBusinessValidationsAsync(id, userToDelete.Email);

                // Marca el usuario como inactivo en lugar de eliminarlo físicamente
                // Preserva los datos para auditoría y posible recuperación posterior
                await MarkUserAsInactiveAsync(userToDelete, reason);

                // Revoca todos los tokens activos para invalidar sesiones inmediatamente
                // Garantiza que el usuario marcado como eliminado no pueda seguir accediendo
                await _deletionService.HandlePreDeletionCleanupAsync(id);

                var response = CreateDeletionResponse(userToDelete, isLogicalDeletion: true);

                _logger.LogInformation("Usuario marcado como inactivo exitosamente: {UsuarioId}", id);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en eliminación lógica de usuario {UsuarioId}", id);
                throw;
            }
        }

        public async Task<DeleteUsuarioResponseDTO> ExecuteWithConfirmationAsync(int id, DeleteUsuarioRequestDTO request)
        {
            // Inicia el flujo de eliminación con confirmación adicional de seguridad
            // Este método implementa una capa extra de protección para operaciones críticas
            _logger.LogWarning("Iniciando eliminación con confirmación para usuario ID: {UsuarioId}", id);

            try
            {
                // Obtiene información del usuario que ejecuta la operación desde el contexto HTTP
                // Necesario para validar permisos y registrar quién ejecuta la eliminación
                var currentUserId = _httpContextService.GetCurrentUserId();
                var currentUser = await _usuarioRepository.GetByIdAsync(currentUserId);

                if (currentUser == null)
                {
                    throw new UnauthorizedAccessException("No se pudo identificar al usuario actual");
                }

                // Verifica la contraseña del usuario que ejecuta la eliminación
                // Medida de seguridad adicional para operaciones de alto riesgo
                var isPasswordValid = _passwordHashingService.VerificarClave(
                    request.ConfirmationPassword,
                    currentUser.Constrasena);

                if (!isPasswordValid)
                {
                    _logger.LogWarning("Intento de eliminación con contraseña incorrecta por usuario: {UserId}", currentUserId);
                    throw new UnauthorizedAccessException("Contraseña de confirmación incorrecta");
                }

                // Ejecuta la eliminación normal después de las validaciones adicionales
                // Delega al método principal manteniendo la separación de responsabilidades
                var result = await ExecuteAsync(id);
                result.Reason = request.Reason;

                _logger.LogWarning("Eliminación con confirmación completada por usuario {ExecutorId} para usuario {TargetId}",
                    currentUserId, id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en eliminación con confirmación de usuario {UsuarioId}", id);
                throw;
            }
        }

        private async Task ExecuteBusinessValidationsAsync(int userId, string userEmail)
        {
            // Obtiene el rol del usuario actual desde el contexto de la aplicación
            // Necesario para aplicar reglas de autorización basadas en roles
            var currentUserRole = _httpContextService.GetCurrentUserRole();

            // Valida que el usuario actual tenga permisos para eliminar usuarios
            // Implementa el principio de menor privilegio en operaciones críticas
            await _deletionService.ValidateDeletionPermissionsAsync(userId, currentUserRole);

            // Verifica que el usuario objetivo pueda ser eliminado según reglas de negocio
            // Previene eliminación de usuarios críticos como el último administrador
            await _deletionService.ValidateUserCanBeDeletedAsync(userId);

            // Verifica si existen dependencias activas que impidan la eliminación
            // Las dependencias pueden incluir sesiones activas, transacciones pendientes, etc.
            var hasDependencies = await _deletionService.HasPendingDependenciesAsync(userId);
            if (hasDependencies)
            {
                _logger.LogWarning("Intento de eliminación de usuario con dependencias activas: {Email}", userEmail);
                // Nota: En este caso permitimos la eliminación pero realizamos limpieza previa
                // En otros contextos empresariales podría ser necesario bloquear la operación
            }
        }

        private async Task MarkUserAsInactiveAsync(UsuarioAU user, string reason)
        {
            // Modifica campos específicos para marcar el usuario como inactivo
            // Preserva datos originales agregando metadatos de eliminación lógica
            user.Email = $"deleted_{user.Id}_{DateTime.UtcNow:yyyyMMdd}@deleted.local";
            user.Nombre = $"Usuario Eliminado - {user.Id}";
            // En un sistema más complejo, se agregarían campos como IsDeleted, DeletedAt, DeletedReason

            // Persiste los cambios manteniendo el registro en la base de datos
            await _usuarioRepository.UpdateAsync(user);

            _logger.LogInformation("Usuario marcado como inactivo con razón: {Reason}", reason ?? "No especificada");
        }

        private static DeleteUsuarioResponseDTO CreateDeletionResponse(UsuarioAU deletedUser, bool isLogicalDeletion = false)
        {
            // Construye la respuesta de confirmación con información completa de la operación
            // Incluye metadatos necesarios para auditoría y confirmación al cliente
            return new DeleteUsuarioResponseDTO
            {
                DeletedUserId = deletedUser.Id,
                DeletedUserEmail = deletedUser.Email,
                DeletedAt = DateTime.UtcNow,
                WasSuccessful = true
                // El campo Reason se establece externamente según el contexto de la eliminación
            };
        }
    }
}
