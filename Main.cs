using AngleSharp.Dom;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Policy;
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
        private ConcurrentBag<PwshProcess> processList = new ConcurrentBag<PwshProcess>();
        private CancellationTokenSource _asyncOutputCts;
        private Task _asyncOutputTask;

        const string MSG_HTTP_REQUEST = "httpRequest";
        const string MSG_BRWS_RELOAD = "brwsReload";
        const string MSG_PWSH_NEW = "pwshNew";
        const string MSG_PWSH_INPUT = "pwshInput";
        const string MSG_PWSH_KILL = "pwshKill";
        const string MSG_PWSH_STOP = "pwshStop";
        const string MSG_PWSH_ASYNC_OUTPUT = "pwshAsyncOutput";
        const string MSG_TYPE_PWSH_RESULT = "pwshResult";
        const string MSG_FILE_WRITE = "fileWrite";
        const string MSG_FILE_READ = "fileRead";
        const string MSG_FILE_TEXT_SEARCH = "fileTextSearch";
        const string MSG_SYS_INFO = "sysInfo";
        const string MSG_RAW_TO_MD = "rawToMd";
        const string MSG_TYPE_ERROR = "error";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private object _processListLock = new object();

        public Main()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
            TitleBarController.EnableDarkMode(Handle);
            _ = InitializeWebViewAsync();
        }

        private async Task InitializeWebViewAsync()
        {
            try
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

                webView.CoreWebView2.WebMessageReceived += async (sender, args) =>
                {
                    await HandleWebMessageAsync(args);
                };

                // Wait for the server to start and get the URL
                string? navigationUrl = Program.ServerUrl;
                if (!string.IsNullOrEmpty(navigationUrl))
                {
                    webView.CoreWebView2.Navigate(navigationUrl);
                }
                else
                {
                    // Fallback if server didn't start
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize WebView: {ex.Message}");
            }
        }

        private async Task HandleWebMessageAsync(CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var message = JsonSerializer.Deserialize<JsonElement>(args.WebMessageAsJson);
                var messageType = message.GetProperty("type").GetString();
                string? requestId = message.TryGetProperty("requestId", out var reqIdProp) ? reqIdProp.GetString() : null;

                if (string.IsNullOrEmpty(requestId))
                    requestId = Guid.NewGuid().ToString();

                switch (messageType)
                {
                    case MSG_HTTP_REQUEST:
                        await HandleHttpRequestAsync(message, requestId);
                        break;
                    case MSG_BRWS_RELOAD:
                        await HandleBrwsReload();
                        break;
                    case MSG_PWSH_NEW:
                        await HandlePwshNewAsync(message, requestId);
                        break;
                    case MSG_PWSH_INPUT:
                        await HandlePwshInputAsync(message, requestId);
                        break;
                    case MSG_PWSH_KILL:
                        await HandlePwshKillAsync(requestId);
                        break;
                    case MSG_PWSH_STOP:
                        await HandlePwshStopAsync(requestId);
                        break;
                    case MSG_FILE_WRITE:
                        await HandleFileWriteAsync(message, requestId);
                        break;
                    case MSG_FILE_READ:
                        await HandleFileReadAsync(message, requestId);
                        break;
                    case MSG_FILE_TEXT_SEARCH:
                        await HandleFileTextSearch(message, requestId);
                        break;
                    case MSG_SYS_INFO:
                        await HandleSystemInfo(requestId);
                        break;
                    case MSG_RAW_TO_MD:
                        await HandleRawToMarkdownAsync(message, requestId);
                        break;
                    default:
                        PostErrorMessage("Unrecognized message type");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling web message: {ex.Message}");
                PostErrorMessage($"Error processing request: {ex.Message}");
            }
        }
        private async Task HandleHttpRequestAsync(JsonElement message, string requestId)
        {
            var result = await httpController!.SendHttpRequest(message, requestId);
            webView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(result, JsonOptions));
        }

        private async Task HandleBrwsReload()
        {
            try
            {
                List<PwshProcess> processesToDispose;
                lock (_processListLock)
                {
                    processesToDispose = processList.ToList();
                }

                foreach (var process in processesToDispose)
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disposing process {process.RequestId}: {ex.Message}");
                    }
                }

                processList = new ConcurrentBag<PwshProcess>();

                await webView.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.clearBrowserCache", "{}");
                webView.CoreWebView2.Reload();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reloading browser: {ex.Message}");
                PostErrorMessage($"Error reloading browser: {ex.Message}");
            }
        }

        private async Task HandlePwshNewAsync(JsonElement message, string requestId)
        {
            try
            {
                string name = message.GetProperty("name").GetString();
                if (string.IsNullOrWhiteSpace(name))
                    name = "New window";

                bool keepOpen = message.GetProperty("keepOpen").GetBoolean();
                bool asyncOutput = message.TryGetProperty("asyncOutput", out var asyncProp) ? asyncProp.GetBoolean() : false;

                PwshProcess newWindow = new PwshProcess(requestId, name, keepOpen, asyncOutput)
                {
                    Type = MSG_TYPE_PWSH_RESULT
                };
                processList.Add(newWindow);

                EnsureAsyncOutputTaskRunning();

                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_TYPE_PWSH_RESULT,
                    Status = 0,
                    Output = "Pwsh started successfully",
                    IsRunning = newWindow.IsRunning
                });
            }
            catch (Exception ex)
            {
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_TYPE_PWSH_RESULT,
                    Status = 1,
                    Output = $"Failed to start PowerShell: {ex.Message}"
                });
            }
        }

        private async Task HandlePwshInputAsync(JsonElement message, string requestId)
        {
            try
            {
                string command = message.GetProperty("command").GetString();
                if (string.IsNullOrWhiteSpace(command))
                    throw new ArgumentException("Command cannot be empty");

                var inputProcess = GetProcessByRequestId(requestId);

                if (inputProcess == null)
                {
                    PwshProcess newWindow = new PwshProcess(requestId, "window-" + requestId, false, false)
                    {
                        Type = MSG_TYPE_PWSH_RESULT
                    };

                    try
                    {
                        newWindow.ExecuteCommand(command);
                        string output = newWindow.GetAllOutput();
                        PostWebMessage(new PwshResult(requestId)
                        {
                            Type = MSG_TYPE_PWSH_RESULT,
                            Status = 0,
                            Output = output,
                            IsRunning = newWindow.IsRunning
                        });
                    }
                    finally
                    {
                        newWindow.Dispose();
                    }
                }
                else
                {
                    inputProcess.ExecuteCommand(command);
                    PostWebMessage(new PwshResult(requestId)
                    {
                        Type = MSG_TYPE_PWSH_RESULT,
                        Status = 0,
                        IsRunning = inputProcess.IsRunning,
                        Output = "Running command"
                    });
                }
            }
            catch (Exception ex)
            {
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_TYPE_PWSH_RESULT,
                    Status = 1,
                    Output = ex.Message
                });
            }
        }

        private async Task HandlePwshKillAsync(string requestId)
        {
            try
            {
                var processToKill = GetProcessByRequestId(requestId);

                if (processToKill == null)
                {
                    PostWebMessage(new PwshResult(requestId)
                    {
                        Type = MSG_TYPE_PWSH_RESULT,
                        Status = 1,
                        Output = "Pwsh process not found or already stopped"
                    });
                }
                else
                {
                    processToKill.Dispose();
                    PostWebMessage(new PwshResult(requestId)
                    {
                        Type = MSG_TYPE_PWSH_RESULT,
                        Status = 0,
                        Output = "Pwsh process killed successfully"
                    });
                }
            }
            catch (Exception ex)
            {
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_TYPE_PWSH_RESULT,
                    Status = 1,
                    Output = $"Error killing process: {ex.Message}"
                });
            }
        }

        private async Task HandlePwshStopAsync(string requestId)
        {
            try
            {
                var processToStop = GetProcessByRequestId(requestId);

                if (processToStop == null)
                {
                    PostWebMessage(new PwshResult(requestId)
                    {
                        Type = MSG_TYPE_PWSH_RESULT,
                        Status = 1,
                        Output = "Pwsh process not found or already stopped"
                    });
                }
                else
                {
                    processToStop.StopCommand();
                    PostWebMessage(new PwshResult(requestId)
                    {
                        Type = MSG_TYPE_PWSH_RESULT,
                        Status = 0,
                        Output = "Command stopped successfully",
                        IsRunning = processToStop.IsRunning
                    });
                }
            }
            catch (Exception ex)
            {
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_TYPE_PWSH_RESULT,
                    Status = 1,
                    Output = $"Error stopping command: {ex.Message}"
                });
            }
        }

        private async Task HandleFileWriteAsync(JsonElement message, string requestId)
        {
            try
            {
                string filePath = message.GetProperty("filePath").GetString() ?? string.Empty;
                string content = message.GetProperty("content").GetString() ?? string.Empty;
                filePath = Helper.GetActualPath(filePath);
                if (string.IsNullOrEmpty(filePath))
                {
                    PostWebMessage(new PwshResult(requestId)
                    {
                        Type = MSG_FILE_WRITE,
                        Status = 1,
                        Output = "Invalid file path"
                    });
                    return;
                }
                await File.WriteAllTextAsync(filePath, content);
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_FILE_WRITE,
                    Status = 0,
                    Output = "File written successfully"
                });
            }
            catch (Exception ex)
            {
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_FILE_WRITE,
                    Status = 1,
                    Output = $"Error writing file: {ex.Message}"
                });
            }
        }

        private async Task HandleFileReadAsync(JsonElement element, string requestId)
        {
            try
            {
                string filePath = element.GetProperty("filePath").GetString() ?? string.Empty;
                filePath = Helper.GetActualPath(filePath);
                if (!Helper.IsFileText(filePath))
                {
                    PostWebMessage(new PwshResult(requestId)
                    {
                        Type = MSG_FILE_READ,
                        Status = 1,
                        Output = "File is not a text file"
                    });
                    return;
                }

                string content = File.ReadAllText(filePath);
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_FILE_READ,
                    Status = 0,
                    Output = content
                });
            }
            catch (Exception ex)
            {
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_FILE_READ,
                    Status = 1,
                    Output = $"Error reading file: {ex.Message}"
                });
            }
        }

        private async Task HandleFileTextSearch(JsonElement message, string requestId)
        {
            try
            {
                string filePath = message.GetProperty("filePath").GetString() ?? string.Empty;
                string searchText = message.GetProperty("searchText").GetString() ?? string.Empty;
                filePath = Helper.GetActualPath(filePath);
                if (string.IsNullOrEmpty(searchText) || !Helper.IsFileText(filePath))
                {
                    PostWebMessage(new PwshResult(requestId)
                    {
                        Type = MSG_FILE_TEXT_SEARCH,
                        Status = 1,
                        Output = "Invalid search text or file is not a text file"
                    });
                    return;
                }

                string content = File.ReadAllText(filePath);
                string lowerContent = content.ToLower();
                string lowerSearchText = searchText.ToLower();
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                var matches = new List<object>();
                int charPosition = 0;

                for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
                {
                    string line = lines[lineNumber];
                    int lineStartPosition = charPosition;
                    int columnPosition = 0;

                    while ((columnPosition = line.ToLower().IndexOf(lowerSearchText, columnPosition)) != -1)
                    {
                        matches.Add(new
                        {
                            line = lineNumber + 1,
                            column = columnPosition + 1,
                            matchText = line.Substring(columnPosition, searchText.Length)
                        });
                        columnPosition += searchText.Length;
                    }

                    charPosition += line.Length + 1; // +1 for line ending
                }

                if (matches.Count == 0)
                {
                    PostWebMessage(new PwshResult(requestId)
                    {
                        Type = MSG_FILE_TEXT_SEARCH,
                        Status = 0,
                        Output = JsonSerializer.Serialize(new { found = false, matches = new object[] { } }, JsonOptions)
                    });
                }
                else
                {
                    PostWebMessage(new PwshResult(requestId)
                    {
                        Type = MSG_FILE_TEXT_SEARCH,
                        Status = 0,
                        Output = JsonSerializer.Serialize(new { found = true, matchCount = matches.Count, matches = matches }, JsonOptions)
                    });
                }
            }
            catch (Exception ex)
            {
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_FILE_TEXT_SEARCH,
                    Status = 1,
                    Output = $"Error searching text in file: {ex.Message}"
                });
            }
        }

        private async Task HandleSystemInfo( string requestId)
        {
            try
            {
                var sysInfo = SystemMonitorController.GetFullMetricsJson();
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_SYS_INFO,
                    Status = 0,
                    Output = sysInfo
                });
            }
            catch (Exception ex)
            {
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_SYS_INFO,
                    Status = 1,
                    Output = $"Error fetching system info: {ex.Message}"
                });
            }
        }

        private async Task HandleRawToMarkdownAsync(JsonElement message, string requestId)
        {
            try
            {
                string text = message.GetProperty("text").GetString() ?? string.Empty;
                LlmContentController contentController = new LlmContentController();
                string markdown = await contentController.ProcessToMarkdown(Encoding.UTF8.GetBytes(text));
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_RAW_TO_MD,
                    Status = 0,
                    Output = markdown
                });
            }
            catch (Exception ex)
            {
                PostWebMessage(new PwshResult(requestId)
                {
                    Type = MSG_RAW_TO_MD,
                    Status = 1,
                    Output = $"Error converting text to markdown: {ex.Message}"
                });
            }
        }

        private PwshProcess? GetProcessByRequestId(string requestId)
        {
            lock (_processListLock)
            {
                return processList.FirstOrDefault(p => p.RequestId == requestId && p.IsRunning);
            }
        }

        private void PostWebMessage<T>(T message) where T : class
        {
            try
            {
                webView?.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(message, JsonOptions));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error posting web message: {ex.Message}");
            }
        }

        private void PostErrorMessage(string message)
        {
            try
            {
                webView?.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(
                    new { type = MSG_TYPE_ERROR, message = message },
                    JsonOptions));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error posting error message: {ex.Message}");
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
            const int pollingIntervalMs = 500;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    List<PwshProcess> asyncProcesses;
                    lock (_processListLock)
                    {
                        asyncProcesses = processList.Where(p => p.AsyncOutput && p.IsRunning).ToList();
                    }

                    foreach (var process in asyncProcesses)
                    {
                        try
                        {
                            string output = process.GetIncrementalOutput();
                            if (!string.IsNullOrEmpty(output))
                            {
                                PostWebMessage(new PwshResult(process.RequestId)
                                {
                                    Type = MSG_PWSH_ASYNC_OUTPUT,
                                    Status = 0,
                                    Output = output,
                                    IsRunning = process.IsRunning
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error getting async output from process {process.RequestId}: {ex.Message}");
                        }
                    }

                    // Remove processos que não estão mais rodando e não têm keepOpen
                    lock (_processListLock)
                    {
                        var processesToRemove = processList.Where(p => !p.IsRunning && !p.Keep).ToList();
                        foreach (var process in processesToRemove)
                        {
                            var items = processList.ToList();
                            items.Remove(process);
                            processList = new ConcurrentBag<PwshProcess>(items);
                        }
                    }

                    await Task.Delay(pollingIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Esperado quando a task é cancelada
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unexpected error in async output polling: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                _asyncOutputCts?.Cancel();

                if (_asyncOutputTask != null && !_asyncOutputTask.IsCompleted)
                {
                    if (!_asyncOutputTask.Wait(TimeSpan.FromSeconds(2)))
                    {
                        Debug.WriteLine("Async output task did not complete within timeout");
                    }
                }

                List<PwshProcess> processesToDispose;
                lock (_processListLock)
                {
                    processesToDispose = processList.ToList();
                }

                foreach (var process in processesToDispose)
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disposing process {process.RequestId}: {ex.Message}");
                    }
                }

                processList = new ConcurrentBag<PwshProcess>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnFormClosing: {ex.Message}");
            }
            finally
            {
                base.OnFormClosing(e);
            }
        }

        private void CoreWebView2OnDocumentTitleChanged(object? sender, object e)
        {
            try
            {
                if (webView?.CoreWebView2 == null)
                    return;

                var title = webView.CoreWebView2.DocumentTitle;
                if (!string.IsNullOrWhiteSpace(title))
                    Text = title;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating document title: {ex.Message}");
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
                if (webView?.CoreWebView2 == null)
                    return;

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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating title and favicon: {ex.Message}");
            }
        }
    }
}
