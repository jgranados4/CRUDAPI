using CRUDAPI.Domain.entities;
using CRUDAPI.Domain.Repositories;
using CRUDAPI.Infrastructure.Persistence.context;
using Microsoft.EntityFrameworkCore;

namespace CRUDAPI.Infrastructure.repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly BaseContext _context;
        private readonly ILogger<UsuarioRepository> _logger;
        public UsuarioRepository(BaseContext context, ILogger<UsuarioRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<(IEnumerable<UsuarioAU> usuarios, int totalCount)> GetPagedAsync(
           int page,
           int pageSize,
           string? searchTerm = null,
           string? rol = null,
           string sortBy = "Nombre",
           bool isAscending = true)
        {
            try
            {
                // Construcción de consulta base con filtros aplicados
                var query = _context.UsuariosAU.AsNoTracking().AsQueryable();

                // Aplicar filtros de búsqueda si se proporcionan
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchTermLower = searchTerm.ToLower();
                    query = query.Where(u =>
                        u.Nombre!.ToLower().Contains(searchTermLower) ||
                        u.Email!.ToLower().Contains(searchTermLower));
                }

                // Aplicar filtro por rol si se especifica
                if (!string.IsNullOrWhiteSpace(rol))
                {
                    query = query.Where(u => u.Rol == rol);
                }

                // Obtener el total de registros antes de aplicar paginación
                var totalCount = await query.CountAsync();

                // Aplicar ordenamiento dinámico basado en el parámetro sortBy
                query = ApplyOrdering(query, sortBy, isAscending);

                // Aplicar paginación
                var usuarios = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Consulta paginada ejecutada: Página {Page}, Tamaño {PageSize}, Total {Total}",
                    page, pageSize, totalCount);

                return (usuarios, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en consulta paginada: Página {Page}, Términos: {SearchTerm}",
                    page, searchTerm);
                throw;
            }
        }
        public async Task<IEnumerable<UsuarioAU>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return await GetAllAsync();

                var searchTermLower = searchTerm.ToLower();
                return await _context.UsuariosAU
                    .AsNoTracking()
                    .Where(u =>
                        u.Nombre!.ToLower().Contains(searchTermLower) ||
                        u.Email!.ToLower().Contains(searchTermLower))
                    .OrderBy(u => u.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en búsqueda de usuarios: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<UsuarioAU?> GetByEmailAsync(string email)
        {
            try
            {
                return await _context.UsuariosAU
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email!.ToLower() == email.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuario por email: {Email}", email);
                throw;
            }
        }

        public async Task<UsuarioAU?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.UsuariosAU
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuario por ID: {Id}", id);
                throw;
            }
        }

        public async Task<UsuarioAU> CreateAsync(UsuarioAU usuario)
        {
            try
            {
                var entity = _context.UsuariosAU.Add(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario creado exitosamente con ID: {Id}", entity.Entity.Id);
                return entity.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando usuario: {Email}", usuario.Email);
                throw;
            }
        }
        public async Task<IEnumerable<UsuarioAU>> GetAllAsync()
        {
            try
            {
                return await _context.UsuariosAU
                    .AsNoTracking()
                    .OrderBy(u => u.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todos los usuarios");
                throw;
            }
        }
        public async Task UpdateAsync(UsuarioAU usuario)
        {
            try
            {
                var existingUser = await _context.UsuariosAU.FindAsync(usuario.Id);
                if (existingUser == null)
                    throw new KeyNotFoundException($"Usuario con ID {usuario.Id} no encontrado");

                // Actualice solo campos específicos para evitar sobrescribir datos confidenciales
                existingUser.Nombre = usuario.Nombre;
                existingUser.Email = usuario.Email;
                existingUser.Rol = usuario.Rol;
                // Note:La contraseña debe actualizarse a través de un método separado por seguridad.

                _context.UsuariosAU.Update(existingUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario actualizado exitosamente: {Id}", usuario.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando usuario: {Id}", usuario.Id);
                throw;
            }
        }
        public async Task UpdatePasswordAsync(int userId, string newPasswordHash) {
            try
            {

                var existingUser = await _context.UsuariosAU.FindAsync(userId);
                if (existingUser == null)
                    throw new KeyNotFoundException($"Usuario con ID {userId} no encontrado");

                _logger.LogInformation("Actualizando contraseña para usuario: {UserId}", userId);
                _logger.LogInformation("Hash anterior: {OldHash}", existingUser.Constrasena);
                _logger.LogInformation("Hash nuevo: {NewHash}", newPasswordHash);

                // Actualiza solo la contraseña
                existingUser.Constrasena = newPasswordHash;

                // Marca explícitamente como modificado
                _context.Entry(existingUser).Property(u => u.Constrasena).IsModified = true;

                await _context.SaveChangesAsync();

                // Verifica que se guardó
                var updatedUser = await _context.UsuariosAU.FindAsync(userId);
                _logger.LogInformation("Contraseña actualizada. Hash en BD: {CurrentHash}", updatedUser?.Constrasena);
                _logger.LogInformation("¿Se guardó correctamente? {Success}", updatedUser?.Constrasena == newPasswordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando contraseña del usuario: {UserId}", userId);
                throw;
            }
        }
        public async Task DeleteAsync(int id)
        {
            try
            {
                var usuario = await _context.UsuariosAU.FindAsync(id);
                if (usuario == null)
                    throw new KeyNotFoundException($"Usuario con ID {id} no encontrado");

                _context.UsuariosAU.Remove(usuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuario eliminado exitosamente: {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando usuario: {Id}", id);
                throw;
            }
        }
        public async Task<IEnumerable<UsuarioAU>> GetByRolAsync(string rol)
        {
            try
            {
                return await _context.UsuariosAU
                    .AsNoTracking()
                    .Where(u => u.Rol == rol)
                    .OrderBy(u => u.Nombre)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuarios por rol: {Rol}", rol);
                throw;
            }
        }

        public async Task<int> CountActiveUsersAsync()
        {
            try
            {
                return await _context.UsuariosAU.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error contando usuarios activos");
                throw;
            }
        }
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            try
            {
                return await _context.UsuariosAU
                    .AnyAsync(u => u.Email!.ToLower() == email.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando existencia por email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> ExistsByIdAsync(int id)
        {
            try
            {
                return await _context.UsuariosAU.AnyAsync(u => u.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando existencia por ID: {Id}", id);
                throw;
            }
        }
        private static IQueryable<UsuarioAU> ApplyOrdering(
           IQueryable<UsuarioAU> query,
           string sortBy,
           bool isAscending)
        {
            // Implementación de ordenamiento dinámico seguro
            return sortBy.ToLower() switch
            {
                "nombre" => isAscending ? query.OrderBy(u => u.Nombre) : query.OrderByDescending(u => u.Nombre),
                "email" => isAscending ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
                "rol" => isAscending ? query.OrderBy(u => u.Rol) : query.OrderByDescending(u => u.Rol),
                "id" => isAscending ? query.OrderBy(u => u.Id) : query.OrderByDescending(u => u.Id),
                _ => isAscending ? query.OrderBy(u => u.Nombre) : query.OrderByDescending(u => u.Nombre) // Ordenamiento por defecto
            };
        }

    }
}
