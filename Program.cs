using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace WebWrap
{
    internal static class Program
    {
        private static IHost? _host;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Start the HTTP server with dynamic port and wait for it to be ready
            StartWebServerAsync().Wait();

            try
            {
                Application.Run(new Main());
            }
            finally
            {
                // Clean up: ensure server is stopped and port is freed
                StopWebServer().Wait();
            }
        }

        public static string? ServerUrl { get; private set; }

        private static async Task StartWebServerAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Starting web server...");

                var builder = Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseKestrel(options =>
                        {
                            // Use explicit IP addresses for dynamic port binding
                            options.Listen(IPAddress.Loopback, 0);
                            options.Listen(IPAddress.IPv6Loopback, 0);
                        })
                        .Configure(app =>
                        {
                            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                            app.UseDefaultFiles(new DefaultFilesOptions
                            {
                                DefaultFileNames = new List<string> { "index.html" }
                            });

                            app.UseStaticFiles(new StaticFileOptions
                            {
                                FileProvider = new PhysicalFileProvider(baseDir),
                                RequestPath = ""
                            });

                            // Fallback for SPA routing
                            app.Run(async context =>
                            {
                                var indexPath = Path.Combine(baseDir, "index.html");
                                if (File.Exists(indexPath))
                                {
                                    context.Response.ContentType = "text/html";
                                    await context.Response.SendFileAsync(indexPath);
                                }
                                else
                                {
                                    context.Response.StatusCode = 404;
                                    await context.Response.WriteAsync("index.html not found");
                                }
                            });
                        });
                    });

                _host = builder.Build();
                await _host.StartAsync();

                // Try to get the server address from the host properties
                if (_host.Services.GetService<IServer>() is IServer server)
                {
                    var addressFeature = server.Features.Get<IServerAddressesFeature>();
                    if (addressFeature?.Addresses.FirstOrDefault() is string address)
                    {
                        ServerUrl = address;
                        System.Diagnostics.Debug.WriteLine($"✓ Web server started successfully at: {ServerUrl}");
                        return;
                    }
                }

                // Fallback: construct URL from localhost
                ServerUrl = "http://localhost:5000";
                System.Diagnostics.Debug.WriteLine($"⚠ Using fallback server URL: {ServerUrl}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Failed to start web server: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                ServerUrl = null;
            }
        }

        private static async Task StopWebServer()
        {
            if (_host != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("Stopping web server...");
                    await _host.StopAsync(TimeSpan.FromSeconds(5));
                    System.Diagnostics.Debug.WriteLine("Web server stopped successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping web server: {ex.Message}");
                }
                finally
                {
                    _host.Dispose();
                    _host = null;
                    System.Diagnostics.Debug.WriteLine("Web server resources released. Port is now free.");
                }
            }
        }
    }
}