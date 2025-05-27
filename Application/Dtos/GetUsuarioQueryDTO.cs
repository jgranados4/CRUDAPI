using System.ComponentModel.DataAnnotations;

namespace CRUDAPI.Application.Dtos
{
    public class GetUsuarioQueryDTO
    {
        public string? Rol { get; set; }

        [StringLength(100, ErrorMessage = "El término de búsqueda no puede exceder 100 caracteres")]
        public string? SearchTerm { get; set; }


        [Range(1, int.MaxValue, ErrorMessage = "La página debe ser mayor a 0")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "El tamaño de página debe estar entre 1 y 100")]
        public int PageSize { get; set; } = 10;

        public string? SortBy { get; set; } = "Nombre";
       
        public bool IsAscending { get; set; } = true;
    }
}
