using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace WebWrap.Controllers
{
    public class TitleBarController
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void EnableDarkMode(IntPtr handle)
        {
            try
            {
                int darkMode = 1;
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
            }
            catch
            {
                // ignore if dark mode is not supported
            }
        }
        public static async Task<Icon?> TryLoadIconFromUrlAsync(string url, HttpClient httpClient)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            if (uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                var localPath = uri.LocalPath;
                if (!File.Exists(localPath))
                    return null;

                return TryLoadIconFromFile(localPath);
            }

            if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                return null;

            using var resp = await httpClient.GetAsync(uri);
            if (!resp.IsSuccessStatusCode)
                return null;

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            return TryLoadIconFromBytes(bytes);
        }

        private static Icon? TryLoadIconFromFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".ico")
                return new Icon(path);

            var bytes = File.ReadAllBytes(path);
            return TryLoadIconFromBytes(bytes);
        }

        private static Icon? TryLoadIconFromBytes(byte[] bytes)
        {
            // .ico direto
            if (bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1 && bytes[3] == 0)
            {
                using var ms = new MemoryStream(bytes);
                return new Icon(ms);
            }

            // tentar como imagem (png/jpg/svg não)
            using var imgStream = new MemoryStream(bytes);
            using var bmp = new Bitmap(imgStream);

            // Criar HICON a partir do bitmap
            var hIcon = bmp.GetHicon();
            try
            {
                using var temp = Icon.FromHandle(hIcon);
                return (Icon)temp.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
