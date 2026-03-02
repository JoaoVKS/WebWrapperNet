using MarkItDown;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class LlmContentController
{
    private readonly MarkItDownClient _markItDownClient;

    public LlmContentController()
    {
        _markItDownClient = new MarkItDownClient();
    }

    /// <summary>
    /// Converts various document formats (PDF, HTML, JSON, XML, Word, Excel, images, etc.) to Markdown.
    /// MarkItDown automatically detects the document type and converts accordingly.
    /// </summary>
    public async Task<string> ProcessToMarkdown(byte[] rawData)
    {
        if (rawData == null || rawData.Length == 0)
            return "Empty Content";

        try
        {
            // Detect file extension based on magic bytes
            string extension = DetectFileExtension(rawData);

            // Create a temporary file with proper extension so MarkItDown can detect the format
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + extension);
            File.WriteAllBytes(tempFile, rawData);

            try
            {
                // MarkItDown automatically detects document type and converts to markdown
                var result = await _markItDownClient.ConvertAsync(tempFile);

                if (result?.Markdown == null)
                    return "Unable to process content";

                // Extract markdown content
                string markdown = result.Markdown;

                // Append document metadata if present
                if (result.Metadata != null && result.Metadata.Count > 0)
                {
                    var metadataLines = new StringBuilder();
                    metadataLines.AppendLine("---");
                    foreach (var kvp in result.Metadata)
                    {
                        metadataLines.AppendLine($"{kvp.Key}: {kvp.Value}");
                    }
                    metadataLines.AppendLine("---");
                    metadataLines.AppendLine();
                    metadataLines.Append(markdown);
                    markdown = metadataLines.ToString();
                }

                return CollapseWhitespace(markdown);
            }
            catch
            {
                return rawData.ToString();
            }
            finally
            {
                // Clean up temporary file
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        catch (Exception ex)
        {
            return $"Error processing content: {ex.Message}";
        }
    }

    /// <summary>
    /// Detects the file extension based on magic bytes (file signatures).
    /// </summary>
    private string DetectFileExtension(byte[] data)
    {
        if (data == null || data.Length == 0)
            return ".bin";

        // PDF: magic bytes %PDF
        if (data.Length >= 4 && data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46)
            return ".pdf";

        // DOCX/XLSX/PPTX: PK (ZIP format)
        if (data.Length >= 2 && data[0] == 0x50 && data[1] == 0x4B)
        {
            // Check for specific Office formats by examining central directory
            if (data.Length >= 30)
            {
                // DOCX typically has [Content_Types].xml
                string headerStr = Encoding.ASCII.GetString(data, 0, Math.Min(100, data.Length));
                if (headerStr.Contains("word/") || headerStr.Contains("document"))
                    return ".docx";
                if (headerStr.Contains("xl/") || headerStr.Contains("sheet"))
                    return ".xlsx";
                if (headerStr.Contains("ppt/") || headerStr.Contains("slide"))
                    return ".pptx";
            }
            return ".zip";
        }

        // PNG: 89 50 4E 47
        if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            return ".png";

        // JPEG: FF D8 FF
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return ".jpg";

        // GIF: 47 49 46
        if (data.Length >= 3 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
            return ".gif";

        // JSON: starts with { or [ or whitespace
        string text = Encoding.UTF8.GetString(data);
        string trimmed = text.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            return ".json";

        // XML: starts with <?xml or <
        if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("<"))
            return ".xml";

        // HTML: common HTML tags
        if (trimmed.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            return ".html";

        return ".bin";
    }

    /// <summary>
    /// Cleanup whitespace in the markdown content.
    /// </summary>
    private string CollapseWhitespace(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Collapse 3+ newlines into 2, and trim trailing spaces
        return Regex.Replace(input, @"\n{3,}", "\n\n").Trim();
    }
}