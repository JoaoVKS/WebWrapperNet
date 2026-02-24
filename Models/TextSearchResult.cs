namespace WebWrap.Models
{
    public class TextSearchResult : BaseModel
    {
        public bool Found { get; set; }
        public List<TextOccurrence> Occurrences { get; set; } = new();
        public string? Output { get; set; }
        public int Status { get; set; }

        public TextSearchResult(string requestId)
        {
            this.Type = "fileTextSearch";
            this.RequestId = requestId;
        }
    }

    public class TextOccurrence
    {
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string LineContent { get; set; } = string.Empty;
    }
}
