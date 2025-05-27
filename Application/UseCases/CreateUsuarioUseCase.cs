using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Domain.Services;

namespace CRUDAPI.Application.UseCases
{
   
    public class CreateUsuarioUseCase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUsuarioValidationService _validationService;
        private readonly IPasswordHashingService _passwordHashingService;
        private readonly ILogger<CreateUsuarioUseCase> _logger;
        public CreateUsuarioUseCase(
          IUsuarioRepository usuarioRepository,
          IUsuarioValidationService validationService,
          IPasswordHashingService passwordHashingService,
          ILogger<CreateUsuarioUseCase> logger)
        {
            _usuarioRepository = usuarioRepository;
            _validationService = validationService;
            _passwordHashingService = passwordHashingService;
            _logger = logger;
        }
        public async Task<UsuarioAUResponseDto> ExecuteAsync(CreateUsuarioRequestDTO request)
        {
            _logger.LogInformation("Iniciando creación de usuario para email: {Email}", request.Email);

            try
            {
                // Step 1: Validar las reglas de negocio mediante el servicio de dominio
                await ValidateBusinessRulesAsync(request);

                // Step 2: Transformar el DTO en una entidad de dominio
                var usuario = await TransformToEntityAsync(request);

                // Step 3: Persistencia en el repositorio
                var createdUsuario = await _usuarioRepository.CreateAsync(usuario);

                // Step 4: Transformar en un DTO de respuesta
                var responseDto = TransformToResponseDto(createdUsuario);

                _logger.LogInformation("Usuario creado exitosamente con ID: {Id}", createdUsuario.Id);
                return responseDto;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Validación fallida para usuario: {Email}. Razón: {Reason}", request.Email, ex.Message);
                throw; // Re-throw for controller handling
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante creación de usuario: {Email}", request.Email);
                throw;
            }
        }

        private async Task ValidateBusinessRulesAsync(CreateUsuarioRequestDTO request)
        {
            await _validationService.ValidateUserCreationAsync(request.Email, request.Nombre);
            await _validationService.ValidatePasswordRequirementsAsync(request.Contrasena);

            if (!_validationService.IsValidRole(request.Rol))
            {
                throw new ArgumentException($"Rol inválido: {request.Rol}. Roles válidos: Admin, User, Manager, Guest");
            }
        }

        private async Task<UsuarioAU> TransformToEntityAsync(CreateUsuarioRequestDTO request)
        {
            // Apply domain transformations
            var hashedPassword = _passwordHashingService.EncriptarClave(request.Contrasena);

            return new UsuarioAU
            {
                Nombre = request.Nombre.Trim(),
                Email = request.Email.ToLower().Trim(), // Business rule: emails in lowercase
                Constrasena = hashedPassword,
                Rol = request.Rol
                // CreatedAt and UpdatedAt would be set by the BaseContext if using IBaseEntity
            };
        }
        private static UsuarioAUResponseDto TransformToResponseDto(UsuarioAU usuario)
        {
            return new UsuarioAUResponseDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol
                // Note: Password is never included in response DTOs for security
            };
        }
    }
}

