using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;

namespace CRUDAPI.Application.UseCases
{
    public class UpdateUsuarioUseCase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUsuarioValidationService _validationService;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly ILogger<UpdateUsuarioUseCase> _logger;

        public UpdateUsuarioUseCase(
            IUsuarioRepository usuarioRepository,
            IUsuarioValidationService validationService,
            IPasswordHashingService passwordHashingService,
            ILogger<UpdateUsuarioUseCase> logger)
        {
            _usuarioRepository = usuarioRepository;
            _validationService = validationService;
            _passwordHashingService = passwordHashingService;
            _logger = logger;
        }
        public async Task<UsuarioAUResponseDto> ExecuteAsync(int id, UpdateUsuarioRequestDTO request)
        {
            // Registra el inicio de la operación con información contextual para auditoría
            // y troubleshooting posterior en caso de errores
            _logger.LogInformation("Iniciando actualización de usuario ID: {UsuarioId}, Email: {Email}", id, request.Email);

            try
            {
                // Valida que el ID proporcionado sea un valor válido empresarialmente
                // Previene operaciones contra IDs malformados que podrían causar errores de base de datos
                if (id <= 0)
                {
                    throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(id));
                }

                // Obtiene la entidad existente del repositorio para validar su existencia
                // Esta operación es crucial para mantener integridad referencial
                var existingUser = await _usuarioRepository.GetByIdAsync(id);
                if (existingUser == null)
                {
                    _logger.LogWarning("Intento de actualización de usuario inexistente: {UsuarioId}", id);
                    throw new KeyNotFoundException($"Usuario con ID {id} no encontrado");
                }

                // Ejecuta validaciones de reglas de negocio específicas
                // Garantiza que los cambios propuestos cumplan con políticas empresariales
                await ValidateUpdateBusinessRulesAsync(request, existingUser);

                // Aplica las actualizaciones a la entidad existente preservando datos críticos
                // Este método mantiene la integridad de información sensible como contraseñas
                ApplyUpdatesToEntity(existingUser, request);

                // Persiste los cambios a través del repositorio manteniendo la separación de capas
                // El repositorio se encarga de los detalles técnicos de persistencia
                await _usuarioRepository.UpdateAsync(existingUser);

                // Transforma la entidad actualizada a DTO de respuesta excluyendo información sensible
                // Mantiene principios de seguridad al no exponer datos internos
                var responseDto = TransformToResponseDto(existingUser);

                _logger.LogInformation("Usuario actualizado exitosamente: {UsuarioId}", id);
                return responseDto;
            }
            catch (ArgumentException ex)
            {
                // Captura errores de validación de entrada y los registra para análisis
                // Permite identificar patrones de uso incorrecto de la API
                _logger.LogWarning("Datos inválidos en actualización de usuario {UsuarioId}: {Message}", id, ex.Message);
                throw;
            }
            catch (KeyNotFoundException ex)
            {
                // Registra intentos de actualización de entidades inexistentes
                // Útil para detectar problemas de sincronización entre sistemas
                _logger.LogWarning("Usuario no encontrado para actualización {UsuarioId}: {Message}", id, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Captura cualquier error técnico inesperado con contexto completo
                // Fundamental para debugging y monitoreo de salud del sistema
                _logger.LogError(ex, "Error inesperado actualizando usuario {UsuarioId}", id);
                throw;
            }
        }

        public async Task UpdatePasswordAsync(int id, UpdatePasswordRequestDTO request)
        {
            // Inicia el proceso de cambio de contraseña con logging específico
            // Las operaciones de seguridad requieren trazabilidad especial para auditorías
            _logger.LogInformation("Iniciando cambio de contraseña para usuario ID: {UsuarioId}", id);

            try
            {
                // Valida el formato del ID antes de proceder con operaciones costosas
                if (id <= 0)
                {
                    throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(id));
                }

                // Obtiene la entidad completa incluyendo la contraseña actual
                // Necesaria para verificar las credenciales actuales del usuario
                var existingUser = await _usuarioRepository.GetByIdAsync(id);
                if (existingUser == null)
                {
                    _logger.LogWarning("Intento de cambio de contraseña en usuario inexistente: {UsuarioId}", id);
                    throw new KeyNotFoundException($"Usuario con ID {id} no encontrado");
                }

                // Verifica que la contraseña actual proporcionada sea correcta
                // Medida de seguridad fundamental para prevenir cambios no autorizados
                var isCurrentPasswordValid = _passwordHashingService.VerificarClave(request.CurrentPassword, existingUser.Constrasena);
                if (!isCurrentPasswordValid)
                {
                    _logger.LogWarning("Intento de cambio de contraseña con credenciales incorrectas para usuario: {UsuarioId}", id);
                    throw new UnauthorizedAccessException("La contraseña actual es incorrecta");
                }

                // Aplica validaciones de complejidad a la nueva contraseña
                // Garantiza que cumpla con políticas de seguridad empresariales
                await _validationService.ValidatePasswordRequirementsAsync(request.NewPassword);

                // Genera el hash seguro de la nueva contraseña antes de persistir
                // El servicio de hashing encapsula la lógica criptográfica específica
                var hashedNewPassword = _passwordHashingService.EncriptarClave(request.NewPassword);

                // Actualiza únicamente el campo de contraseña preservando otros datos
                existingUser.Constrasena = hashedNewPassword;

                // Persiste el cambio a través del repositorio
                await _usuarioRepository.UpdateAsync(existingUser);

                _logger.LogInformation("Contraseña actualizada exitosamente para usuario: {UsuarioId}", id);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Los errores de autorización requieren logging especial para detección de intrusiones
                _logger.LogWarning("Acceso no autorizado en cambio de contraseña {UsuarioId}: {Message}", id, ex.Message);
                throw;
            }
            catch (ArgumentException ex)
            {
                // Registra errores de validación de contraseña para análisis de patrones
                _logger.LogWarning("Validación de contraseña fallida {UsuarioId}: {Message}", id, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                // Errores técnicos en operaciones de seguridad requieren atención inmediata
                _logger.LogError(ex, "Error crítico en cambio de contraseña para usuario {UsuarioId}", id);
                throw;
            }
        }

        private async Task ValidateUpdateBusinessRulesAsync(UpdateUsuarioRequestDTO request, UsuarioAU existingUser)
        {
            // Valida el formato del email según estándares empresariales
            // Previene datos malformados que podrían causar problemas downstream
            await _validationService.ValidateEmailFormatAsync(request.Email);

            // Verifica si el email ha cambiado para validar unicidad solo cuando sea necesario
            // Optimización importante para evitar consultas innecesarias a la base de datos
            if (!string.Equals(existingUser.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                // Solo valida unicidad si el email realmente cambió
                // Evita falsos positivos en actualizaciones que no modifican el email
                await _validationService.ValidateUserUniquenessAsync(request.Email);
            }

            // Aplica validaciones de rol según reglas de negocio establecidas
            // Garantiza que solo roles válidos sean asignados a usuarios
            if (!_validationService.IsValidRole(request.Rol))
            {
                throw new ArgumentException($"Rol inválido: {request.Rol}. Roles válidos: Admin, User, Manager, Guest");
            }
        }

        private static void ApplyUpdatesToEntity(UsuarioAU existingUser, UpdateUsuarioRequestDTO request)
        {
            // Actualiza campos específicos manteniendo normalización de datos
            // La normalización garantiza consistencia en el almacenamiento
            existingUser.Nombre = request.Nombre.Trim();
            existingUser.Email = request.Email.ToLower().Trim();
            existingUser.Rol = request.Rol;

            // Deliberadamente no actualiza la contraseña aquí
            // Las contraseñas requieren un flujo separado con validaciones específicas de seguridad
            // Esta separación previene modificaciones accidentales de credenciales
        }

        private static UsuarioAUResponseDto TransformToResponseDto(UsuarioAU usuario)
        {
            // Crea el DTO de respuesta excluyendo información sensible
            // Esta transformación es crítica para mantener principios de seguridad
            return new UsuarioAUResponseDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol
                // La contraseña nunca se incluye en respuestas por políticas de seguridad
                // Evita exposición accidental de credenciales en logs o respuestas de API
            };
        }
    }
}

