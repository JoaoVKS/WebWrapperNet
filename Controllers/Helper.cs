using System;
using System.Collections.Generic;
using System.Text;

namespace WebWrap.Controllers
{
    public static class Helper
    {
        public static string? ParseJsString(string? jsResult)
        {
            if (string.IsNullOrWhiteSpace(jsResult))
                return null;

            // ExecuteScriptAsync retorna JSON string (ex.: "\"Title\"")
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<string>(jsResult);
            }
            catch
            {
                return jsResult.Trim('"');
            }
        }
        public static bool IsFileText(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new InvalidDataException("File doesn't exist");
            }

            using (var reader = new StreamReader(filePath, true))
            {
                reader.Peek(); // Forces the reader to look at the BOM
                var encoding = reader.CurrentEncoding;
                // If encoding is found, it's a strong indicator it's text
                if (encoding != null)
                {
                    // Additional logic can be added here if needed
                    if (encoding is UTF8Encoding || encoding is UnicodeEncoding || encoding is UTF32Encoding || encoding is ASCIIEncoding)
                    {
                        // Likely a text file, proceed to read
                        return true;
                    }
                    else
                    {
                        throw new InvalidDataException("File encoding is not recognized as text");
                    }
                }
            }
            return false;
        }
    }
}
