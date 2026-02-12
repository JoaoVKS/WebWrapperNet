using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using WebWrap.Models;

namespace WebWrap.Controllers
{
    public class HttpController
    {
        private readonly HttpClient? _httpClient;

        public HttpController(HttpClient? httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseResult> SendHttpRequest(JsonElement message, string requestId)
        {
            if (_httpClient == null)
                throw new InvalidOperationException("HttpClient is not initialized.");
            try
            {
                string url = message.GetProperty("url").GetString();
                string method = message.GetProperty("method").GetString();
                var request = new HttpRequestMessage(new HttpMethod(method), url);

                // Add headers if provided
                if (message.TryGetProperty("headers", out var headersElement))
                {
                    foreach (var header in headersElement.EnumerateObject())
                    {
                        try
                        {
                            request.Headers.TryAddWithoutValidation(header.Name, header.Value.GetString());
                        }
                        catch
                        {
                            // Skip invalid headers
                        }
                    }
                }

                // Add body if provided
                if (message.TryGetProperty("body", out var bodyElement))
                {
                    string body = bodyElement.GetString();
                    string contentType = "application/json";

                    if (message.TryGetProperty("contentType", out var ctElement))
                    {
                        contentType = ctElement.GetString();
                    }

                    request.Content = new StringContent(body, Encoding.UTF8, contentType);
                }

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                var responseHeaders = new Dictionary<string, string>();
                foreach (var header in response.Headers)
                {
                    responseHeaders[header.Key] = string.Join(", ", header.Value);
                }
                foreach (var header in response.Content.Headers)
                {
                    responseHeaders[header.Key] = string.Join(", ", header.Value);
                }

                var result = new HttpResponseResult
                {
                    Status = (int)response.StatusCode,
                    StatusText = response.ReasonPhrase,
                    Headers = responseHeaders,
                    Body = responseBody,
                    RequestId = requestId
                };
                return result;
            }
            catch (Exception ex)
            {
                return new HttpResponseResult
                {
                    Type = "httpError",
                    StatusText = JsonSerializer.Serialize(ex.Message),
                    RequestId = requestId,
                };
            }
        }
    }
}
