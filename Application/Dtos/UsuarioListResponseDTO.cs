namespace CRUDAPI.Application.Dtos
{
    public class UsuarioListResponseDTO
    {
        
        public IEnumerable<UsuarioAUResponseDto> Usuarios { get; set; } = new List<UsuarioAUResponseDto>();
        public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();
        public class PaginationMetadata
        {
            public int CurrentPage { get; set; }
            public int PageSize { get; set; }
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public bool HasNextPage { get; set; }
            public bool HasPreviousPage { get; set; }
        }
    }
}
