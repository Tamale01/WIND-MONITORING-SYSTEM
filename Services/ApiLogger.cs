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
            // Store log file inside the app root under logs/
            var logsDir = Path.Combine(env.ContentRootPath, "logs");
            Directory.CreateDirectory(logsDir);
            _logPath = Path.Combine(logsDir, "api_log.txt");
        }

        /// <summary>Logs an API call with timestamp, endpoint, and user.</summary>
        public void Log(string endpoint, string user)
        {
            var entry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC] ENDPOINT={endpoint} USER={user}{Environment.NewLine}";
            lock (_lock)
            {
                File.AppendAllText(_logPath, entry);
            }
        }
    }
}
