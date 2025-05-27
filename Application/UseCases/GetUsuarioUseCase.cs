using CRUDAPI.Application.Dtos;
using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using static CRUDAPI.Application.Dtos.UsuarioListResponseDTO;

namespace CRUDAPI.Application.UseCases
{
    public class GetUsuarioUseCase
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ILogger<GetUsuarioUseCase> _logger;
        public GetUsuarioUseCase(
            IUsuarioRepository usuarioRepository,
            ILogger<GetUsuarioUseCase> logger)
        {
            _usuarioRepository = usuarioRepository;
            _logger = logger;
        }
        public async Task<UsuarioAUResponseDto> GetByIdAsync(int id)
        {
            _logger.LogInformation("Iniciando consulta de usuario por ID: {UsuarioId}", id);

            try
            {
                // Validación de entrada básica
                if (id <= 0)
                {
                    throw new ArgumentException("El ID del usuario debe ser mayor a cero", nameof(id));
                }

                // Delegación al repository para acceso a datos
                var usuario = await _usuarioRepository.GetByIdAsync(id);

                // Validación de existencia del usuario
                if (usuario == null)
                {
                    _logger.LogWarning("Usuario no encontrado con ID: {UsuarioId}", id);
                    throw new KeyNotFoundException($"Usuario con ID {id} no encontrado");
                }

                // Transformación a DTO de respuesta
                var responseDto = TransformToResponseDto(usuario);

                _logger.LogInformation("Usuario obtenido exitosamente: {UsuarioId}", id);
                return responseDto;
            }
            catch (KeyNotFoundException)
            {
                // Re-lanzar excepciones de dominio sin modificar
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado obteniendo usuario por ID: {UsuarioId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<UsuarioAUResponseDto>> GetAllAsync()
        {
            _logger.LogInformation("Iniciando consulta de todos los usuarios");

            try
            {
                // Delegación al repository para obtener todos los usuarios
                var usuarios = await _usuarioRepository.GetAllAsync();

                // Transformación masiva a DTOs usando LINQ
                var responseDtos = usuarios.Select(TransformToResponseDto);

                _logger.LogInformation("Obtenidos {Count} usuarios del sistema", usuarios.Count());
                return responseDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todos los usuarios");
                throw;
            }
        }

        public async Task<UsuarioListResponseDTO> GetPagedAsync(GetUsuarioQueryDTO queryParams)
        {
            _logger.LogInformation("Iniciando consulta paginada - Página: {Page}, Tamaño: {PageSize}, Búsqueda: {SearchTerm}",
                queryParams.Page, queryParams.PageSize, queryParams.SearchTerm);

            try
            {
                // Validación de parámetros de paginación
                ValidatePaginationParameters(queryParams);

                // Delegación al repository con todos los parámetros de filtrado
                var (usuarios, totalCount) = await _usuarioRepository.GetPagedAsync(
                    queryParams.Page,
                    queryParams.PageSize,
                    queryParams.SearchTerm,
                    queryParams.Rol,
                    queryParams.SortBy ?? "Nombre",
                    queryParams.IsAscending);

                // Transformación de entidades a DTOs
                var usuarioDtos = usuarios.Select(TransformToResponseDto);

                // Construcción de metadatos de paginación
                var paginationMetadata = CreatePaginationMetadata(
                    queryParams.Page,
                    queryParams.PageSize,
                    totalCount);

                // Ensamblaje de la respuesta completa
                var response = new UsuarioListResponseDTO
                {
                    Usuarios = usuarioDtos,
                    Pagination = paginationMetadata
                };

                _logger.LogInformation("Consulta paginada completada - Total: {Total}, Página actual: {CurrentPage}",
                    totalCount, queryParams.Page);

                return response;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Parámetros de consulta inválidos: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en consulta paginada");
                throw;
            }
        }

        public async Task<IEnumerable<UsuarioAUResponseDto>> GetByRolAsync(string rol)
        {
            _logger.LogInformation("Iniciando consulta de usuarios por rol: {Rol}", rol);

            try
            {
                // Validación de entrada
                if (string.IsNullOrWhiteSpace(rol))
                {
                    throw new ArgumentException("El rol es requerido", nameof(rol));
                }

                // Delegación al repository
                var usuarios = await _usuarioRepository.GetByRolAsync(rol);

                // Transformación a DTOs
                var responseDtos = usuarios.Select(TransformToResponseDto);

                _logger.LogInformation("Obtenidos {Count} usuarios con rol {Rol}", usuarios.Count(), rol);
                return responseDtos;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuarios por rol: {Rol}", rol);
                throw;
            }
        }

        public async Task<IEnumerable<UsuarioAUResponseDto>> SearchUsersAsync(string searchTerm)
        {
            _logger.LogInformation("Iniciando búsqueda de usuarios con término: {SearchTerm}", searchTerm);

            try
            {
                // Validación de entrada
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    _logger.LogInformation("Término de búsqueda vacío, retornando todos los usuarios");
                    return await GetAllAsync();
                }

                // Delegación al repository
                var usuarios = await _usuarioRepository.SearchUsersAsync(searchTerm);

                // Transformación a DTOs
                var responseDtos = usuarios.Select(TransformToResponseDto);

                _logger.LogInformation("Búsqueda completada - {Count} usuarios encontrados", usuarios.Count());
                return responseDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en búsqueda de usuarios: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            _logger.LogInformation("Obteniendo conteo total de usuarios");

            try
            {
                var count = await _usuarioRepository.CountActiveUsersAsync();

                _logger.LogInformation("Total de usuarios en el sistema: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo conteo de usuarios");
                throw;
            }
        }

     
        private static UsuarioAUResponseDto TransformToResponseDto(UsuarioAU usuario)
        {
            return new UsuarioAUResponseDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol
                // Nota: La contraseña nunca se incluye en los DTOs de respuesta por seguridad
            };
        }
        private static void ValidatePaginationParameters(GetUsuarioQueryDTO queryParams)
        {
            if (queryParams.Page <= 0)
            {
                throw new ArgumentException("El número de página debe ser mayor a cero", nameof(queryParams.Page));
            }

            if (queryParams.PageSize <= 0 || queryParams.PageSize > 100)
            {
                throw new ArgumentException("El tamaño de página debe estar entre 1 y 100", nameof(queryParams.PageSize));
            }
        }
        private static PaginationMetadata CreatePaginationMetadata(int currentPage, int pageSize, int totalItems)
        {
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return new PaginationMetadata
            {
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasNextPage = currentPage < totalPages,
                HasPreviousPage = currentPage > 1
            };
        }
    }
}

