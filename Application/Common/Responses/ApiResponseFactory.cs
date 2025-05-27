namespace CRUDAPI.Application.Common.Responses
{
    public static class ApiResponseFactory
    {
        public static ApiResponse<T> NotFound<T>(string message)
            => new(HttpStatusHelper.GetStatusInfo(404), default, message);

        public static ApiResponse<T> BadRequest<T>(string message)
            => new(HttpStatusHelper.GetStatusInfo(400), default, message);

        public static ApiResponse<T> Ok<T>(T data, string? message = null)
            => new(HttpStatusHelper.GetStatusInfo(200), data, message);

        public static ApiResponse<T> Created<T>(T data, string? message = null)
            => new(HttpStatusHelper.GetStatusInfo(201), data, message);

        public static ApiResponse<T> Unauthorized<T>(string message)
            => new(HttpStatusHelper.GetStatusInfo(401), default, message);

        public static ApiResponse<T> ServerError<T>(string message)
            => new(HttpStatusHelper.GetStatusInfo(500), default, message);
    }
}
