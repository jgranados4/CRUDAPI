namespace CRUDAPI.Application.Common.Responses
{
    public static class HttpStatusHelper
    {
        private static readonly Dictionary<int, string> Descriptions = new()
        {
            [100] = "Continue",
            [200] = "OK",
            [201] = "Created",
            [204] = "No Content",
            [301] = "Moved Permanently",
            [302] = "Found",
            [400] = "Bad Request",
            [401] = "Unauthorized",
            [403] = "Forbidden",
            [404] = "Not Found",
            [500] = "Internal Server Error",
            [502] = "Bad Gateway",
            [503] = "Service Unavailable"
            // Puedes agregar más códigos si necesitas
        };

        public static HttpStatusInfo GetStatusInfo(int code)
        {
            var description = Descriptions.TryGetValue(code, out var desc)
                ? desc
                : "Unknown";

            var category = code switch
            {
                >= 100 and < 200 => HttpStatusCategory.Informational,
                >= 200 and < 300 => HttpStatusCategory.Success,
                >= 300 and < 400 => HttpStatusCategory.Redirection,
                >= 400 and < 500 => HttpStatusCategory.ClientError,
                >= 500 and < 600 => HttpStatusCategory.ServerError,
                _ => throw new ArgumentOutOfRangeException(nameof(code), "Código fuera de rango.")
            };

            return new HttpStatusInfo
            {
                Code = code,
                Description = description,
                Category = category
            };
        }
    }
}
