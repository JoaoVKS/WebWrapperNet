namespace WebWrap.Models
{
    public class HttpResponseResult
    {
        public string Type { get; set; } = "httpResponse";
        public int Status { get; set; }
        public string? StatusText { get; set; }
        public string? RequestId { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = string.Empty;
    }
}
