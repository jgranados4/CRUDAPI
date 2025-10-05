using CRUDAPI.Domain.entities;

namespace CRUDAPI.Domain.Repositories
{
    public interface IUsuarioRepository
    {
        Task<UsuarioAU?> GetByEmailAsync(string email);
        Task<UsuarioAU?> GetByIdAsync(int id);
        Task<IEnumerable<UsuarioAU>> GetAllAsync();
        Task<UsuarioAU> CreateAsync(UsuarioAU usuario);
        Task UpdateAsync(UsuarioAU usuario);
        Task DeleteAsync(int id);
        // ✅ Operaciones de búsqueda avanzada
        Task<IEnumerable<UsuarioAU>> GetByRolAsync(string rol);
        Task<int> CountActiveUsersAsync();
        // ✅ Consultas de negocio específicas
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByIdAsync(int id);
        Task<(IEnumerable<UsuarioAU> usuarios, int totalCount)> GetPagedAsync(int page, int pageSize, string? searchTerm = null, string? rol = null, string sortBy = "Nombre", bool isAscending = true);
        Task<IEnumerable<UsuarioAU>> SearchUsersAsync(string searchTerm);
        Task UpdatePasswordAsync(int userId, string newPasswordHash);
    }
}