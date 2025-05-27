namespace CRUDAPI.Application.Common.Responses
{
    public class ApiResponse<T>
    {
        public HttpStatusInfo Status { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public ApiResponse(HttpStatusInfo status, T? data = default, string? message = null)
        {
            Status = status;
            Data = data;
            Message = message;
        }
    }
}
