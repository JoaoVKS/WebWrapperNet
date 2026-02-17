using System;
using System.Diagnostics;
using System.Text;
using WebWrap.Models;

namespace WebWrap.Controllers
{
    public class PwshProcess : BaseModel, IDisposable
    {
        private Process _psProcess;
        private StreamWriter _inputWriter;
        private readonly StringBuilder _outputLog;
        private readonly object _outputLock = new object();
        private bool _disposed = false;

        public string Name { get; set; }
        public int PID { get; set; }
        public bool Keep { get; set; }
        public bool AsyncOutput { get; set; }
        public List<string> InputHistory { get; set; }
        
        public bool IsRunning
        {
            get
            {
                if (_disposed)
                    return false;

                try
                {
                    return !_psProcess.HasExited;
                }
                catch
                {
                    return false;
                }
            }
        }

        public PwshProcess(string RequestId, string Name, bool Keep, bool asyncOutput = false)
        {
            this.RequestId = RequestId;
            this.Name = Name;
            this.Keep = Keep;
            this.AsyncOutput = asyncOutput;
            InputHistory = new List<string>();
            _outputLog = new StringBuilder();

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = this.Keep ? "-NoExit -Command -" : "-Command -",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                //false for debugging, true for production to avoid console window
                CreateNoWindow = false,
                StandardOutputEncoding = Encoding.UTF8
            };

            _psProcess = new Process { StartInfo = startInfo };

            try
            {
                _psProcess.OutputDataReceived += (s, e) => 
                { 
                    if (e.Data != null)
                    {
                        lock (_outputLock)
                        {
                            _outputLog.AppendLine(e.Data);
                        }
                    }
                };

                _psProcess.ErrorDataReceived += (s, e) => 
                { 
                    if (e.Data != null)
                    {
                        lock (_outputLock)
                        {
                            _outputLog.AppendLine($"ERROR: {e.Data}");
                        }
                    }
                };

                _psProcess.Start();
                PID = _psProcess.Id;
                _psProcess.BeginOutputReadLine();
                _psProcess.BeginErrorReadLine();

                _inputWriter = _psProcess.StandardInput;
            }
            catch
            {
                _psProcess?.Dispose();
                throw;
            }
        }

        public void ExecuteCommand(string command)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PwshProcess));

            if (!IsRunning)
                throw new InvalidOperationException("Process is not running.");

            if (_inputWriter == null)
                throw new InvalidOperationException("Input writer is not available.");

            try
            {
                _inputWriter.WriteLine(command);
                InputHistory.Add(command);
                _inputWriter.Flush();
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("Failed to execute command.", ex);
            }
        }

        public string GetAllOutput()
        {
            if (_disposed)
                return string.Empty;

            lock (_outputLock)
            {
                return _outputLog.ToString();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    _inputWriter?.Close();
                    _inputWriter?.Dispose();
                }
                catch
                {
                    // Ignore errors when closing input
                }

                if (_psProcess != null && !_psProcess.HasExited)
                {
                    try
                    {
                        _psProcess.Kill();
                        _psProcess.WaitForExit(TimeSpan.FromSeconds(2));
                    }
                    catch
                    {
                        // Ignore errors
                    }
                }

                _psProcess?.Dispose();
            }

            _disposed = true;
        }
    }
}
