namespace WebWrap.Models
{
    public class HttpResponseResult : BaseModel
    {
        public int Status { get; set; }
        public string? StatusText { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = string.Empty;

        public HttpResponseResult(string RequestId)
        {
            this.Type = "httpResponse";
            this.RequestId = RequestId;
        }
    }
   
}
