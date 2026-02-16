using System.Diagnostics;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using WebWrap.Controllers;
using WebWrap.Models;

namespace WebWrap
{
    public partial class Form1 : Form
    {
        private readonly HttpClient _httpClient = new();
        public HttpController? httpController;

        public Form1()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
            TitleBarController.EnableDarkMode(Handle);
            _ = InitializeWebViewAsync();
        }

        private async Task InitializeWebViewAsync()
        {
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };

            httpController = new HttpController(_httpClient);

            Controls.Add(webView);

            await webView.EnsureCoreWebView2Async(null);

            webView.CoreWebView2.NavigationCompleted += CoreWebView2OnNavigationCompleted;
            webView.CoreWebView2.DocumentTitleChanged += CoreWebView2OnDocumentTitleChanged;

            webView.CoreWebView2.PermissionRequested += (sender, args) =>
            {
                args.State = CoreWebView2PermissionState.Allow;
            };
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true // Optional: for pretty-printing
            };

            webView.CoreWebView2.WebMessageReceived += async (sender, args) =>
            {
                var message = JsonSerializer.Deserialize<JsonElement>(args.WebMessageAsJson);
                var messageType = message.GetProperty("type").GetString();
                string? requestId = message.TryGetProperty("requestId", out var reqIdProp) ? reqIdProp.GetString() : null;

                if (string.IsNullOrEmpty(requestId))
                    requestId = Guid.NewGuid().ToString();

                switch (messageType)
                {
                    case "httpRequest":
                        var result = await httpController.SendHttpRequest(message, requestId);
                        webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(result, options));
                        break;
                    default:
                        // Unrecognized message type
                        webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new { type = "error", message = "Unrecognized message type" }, options));
                        break;
                }

            };

            string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index.html");
            if (File.Exists(htmlPath))
            {
                webView.CoreWebView2.Navigate("file:///" + htmlPath.Replace("\\", "/"));
            }
            else
            {
                webView.CoreWebView2.NavigateToString("<h1 style='color:white'>index.html not found!</h1><p style='color:white'>Place it in the same folder as the .exe</p>");
            }
        }

        private void CoreWebView2OnDocumentTitleChanged(object? sender, object e)
        {
            try
            {
                var title = webView.CoreWebView2.DocumentTitle;
                if (!string.IsNullOrWhiteSpace(title))
                    Text = title;
            }
            catch
            {
                // ignore
            }
        }

        private async void CoreWebView2OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                return;

            await UpdateTitleAndFaviconAsync();
        }

        private async Task UpdateTitleAndFaviconAsync()
        {
            try
            {
                var title = await webView.ExecuteScriptAsync("document.title");
                var parsedTitle = Helper.ParseJsString(title);
                if (!string.IsNullOrWhiteSpace(parsedTitle))
                    Text = parsedTitle;

                var faviconHrefJson = await webView.ExecuteScriptAsync(@"(() => {
                    const rels = ['icon','shortcut icon','apple-touch-icon','mask-icon'];
                    for (const rel of rels) {
                      const el = document.querySelector(`link[rel~='${rel.split(' ')[0]}']`) || document.querySelector(`link[rel='${rel}']`);
                      if (el && el.href) return el.href;
                    }
                    const any = document.querySelector('link[rel*=icon]');
                    if (any && any.href) return any.href;
                    return new URL('/favicon.ico', location.href).href;
                })()");

                var faviconHref = Helper.ParseJsString(faviconHrefJson);
                if (string.IsNullOrWhiteSpace(faviconHref))
                    return;

                var icon = await TitleBarController.TryLoadIconFromUrlAsync(faviconHref, _httpClient);
                if (icon is not null)
                    Icon = icon;
            }
            catch
            {
                // ignore
            }
        }
    }
}
