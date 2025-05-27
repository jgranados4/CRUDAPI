namespace CRUDAPI.Application.Common.Responses
{
    public class HttpStatusInfo
    {
        public int Code { get; set; }
        public string? Description { get; set; } 
        public HttpStatusCategory Category { get; set; }
    }
}
