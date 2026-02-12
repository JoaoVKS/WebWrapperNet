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
    }
}
