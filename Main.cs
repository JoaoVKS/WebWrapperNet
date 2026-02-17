using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using WebWrap.Controllers;
using WebWrap.Models;

namespace WebWrap
{
    public partial class Main : Form
    {
        private readonly HttpClient _httpClient = new();
        public HttpController? httpController;
        private List<PwshProcess> processList = new List<PwshProcess>();
        private CancellationTokenSource _asyncOutputCts;
        private Task _asyncOutputTask;

        public Main()
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
                WriteIndented = true
            };

            webView.CoreWebView2.WebMessageReceived += async (sender, args) =>
            {
                var message = JsonSerializer.Deserialize<JsonElement>(args.WebMessageAsJson);
                var messageType = message.GetProperty("type").GetString();
                string? requestId = message.TryGetProperty("requestId", out var reqIdProp) ? reqIdProp.GetString() : null;

                if (string.IsNullOrEmpty(requestId))
                    requestId = Guid.NewGuid().ToString();

                string returnType = "";

                switch (messageType)
                {
                    case "httpRequest":
                        var result = await httpController.SendHttpRequest(message, requestId);
                        webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(result, options));
                        break;
                    case "pwshNew":
                        returnType = "pwshResult";
                        try
                        {
                            string name = message.GetProperty("name").GetString() ?? "New window";
                            bool keepOpen = message.GetProperty("keepOpen").GetBoolean();
                            bool asyncOutput = message.TryGetProperty("asyncOutput", out var asyncProp) ? asyncProp.GetBoolean() : false;
                            
                            PwshProcess newWindow = new PwshProcess(requestId, name, keepOpen, asyncOutput)
                            {
                                Type = returnType
                            };
                            processList.Add(newWindow);
                            
                            // Inicia task de polling se algum processo tiver asyncOutput = true
                            EnsureAsyncOutputTaskRunning();
                            
                            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new PwshResult(requestId)
                            {
                                Type = returnType,
                                Status = 0,
                                Output = "PowerShell started successfully"
                            }, options));
                        }
                        catch
                        {
                            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new PwshResult(requestId)
                            {
                                Type = returnType,
                                Status = 1,
                                Output = "Failed to start PowerShell",
                            }, options));
                        }
                        break;
                    case "pwshInput":
                        returnType = "pwshResult";
                        try
                        {
                            string command = message.GetProperty("command").GetString() ?? "";
                            if(string.IsNullOrEmpty(command))
                                throw new Exception("Invalid command");

                            var process = processList.FirstOrDefault(p => p.RequestId == requestId && p.IsRunning);
                            if (process == null)
                            {
                                PwshProcess newWindow = new PwshProcess(requestId, "window-" + requestId, false, false)
                                {
                                    Type = returnType
                                };
                                newWindow.ExecuteCommand(command);
                                string output = newWindow.GetAllOutput();
                                newWindow.Dispose();
                                webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new PwshResult(requestId)
                                {
                                    Type = returnType,
                                    Status = 0,
                                    Output = output,
                                    IsRunning = newWindow.IsRunning
                                }, options));
                            }
                            else
                            {
                                process.ExecuteCommand(command);
                                webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new PwshResult(requestId)
                                {
                                    Type = returnType,
                                    Status = 0,
                                    IsRunning = process.IsRunning,
                                    Output = "Running command"
                                }, options));
                            }
                        }
                        catch (Exception ex)
                        {
                            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new PwshResult(requestId)
                            {
                                Type = returnType,
                                Status = 1,
                                Output = ex.Message,
                            }, options));
                        }
                        break;
                    default:
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

        private void EnsureAsyncOutputTaskRunning()
        {
            if (_asyncOutputTask == null || _asyncOutputTask.IsCompleted)
            {
                _asyncOutputCts = new CancellationTokenSource();
                _asyncOutputTask = StartAsyncOutputPollingAsync(_asyncOutputCts.Token);
            }
        }

        private async Task StartAsyncOutputPollingAsync(CancellationToken cancellationToken)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            const int pollingIntervalMs = 500; // Ajuste conforme necessário

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var asyncProcesses = processList.Where(p => p.AsyncOutput && p.IsRunning).ToList();

                    foreach (var process in asyncProcesses)
                    {
                        string output = process.GetAllOutput();
                        webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new PwshResult(process.RequestId)
                        {
                            Type = "pwshAsyncOutput",
                            Status = 0,
                            Output = output,
                            IsRunning = process.IsRunning
                        }, options));
                    }

                    // Remove processos que não estão mais rodando e não têm asyncOutput
                    processList.RemoveAll(p => !p.IsRunning && !p.Keep);

                    await Task.Delay(pollingIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Task foi cancelada, esperado
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _asyncOutputCts?.Cancel();
            _asyncOutputTask?.Wait(TimeSpan.FromSeconds(2));
            
            foreach (var process in processList)
            {
                process.Dispose();
            }
            
            base.OnFormClosing(e);
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
