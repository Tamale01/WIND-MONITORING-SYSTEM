// File: Services/ApiLogger.cs
namespace WindMonitoringSystem.Services
{
    /// <summary>
    /// Simple file-based API logger that appends timestamped entries to logs/api_log.txt.
    /// Thread-safe via locking — suitable for low-to-medium traffic.
    /// </summary>
    public class ApiLogger
    {
        private readonly string _logPath;
        private static readonly object _lock = new();

        public ApiLogger(IWebHostEnvironment env)
        {
            try
            {
                // Store log file inside the app root under logs/
                var logsDir = Path.Combine(env.ContentRootPath, "logs");
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }
                _logPath = Path.Combine(logsDir, "api_log.txt");
            }
            catch
            {
                _logPath = Path.GetTempFileName(); // Fallback
            }
        }

        /// <summary>Logs an API call with timestamp, endpoint, and user.</summary>
        public void Log(string endpoint, string user)
        {
            try
            {
                var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] ENDPOINT={endpoint} USER={user}{Environment.NewLine}";
                lock (_lock)
                {
                    File.AppendAllText(_logPath, entry);
                }
            }
            catch
            {
                // Silence logging errors to prevent app crashes in restricted environments
            }
        }
    }
}
