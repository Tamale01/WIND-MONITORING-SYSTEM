// File: Services/BackgroundReadingService.cs
using Microsoft.EntityFrameworkCore;
using WindMonitoringSystem.Data;
using WindMonitoringSystem.Models;

namespace WindMonitoringSystem.Services
{
    /// <summary>
    /// Background service that polls the simulated sensor every 10 seconds
    /// and persists each reading to the database.
    /// Implements IHostedService via BackgroundService base class.
    /// </summary>
    public class BackgroundReadingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISensorSimulator _simulator;
        private readonly ILogger<BackgroundReadingService> _logger;

        // How often to capture a new reading (10 seconds)
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

        public BackgroundReadingService(
            IServiceScopeFactory scopeFactory,
            ISensorSimulator simulator,
            ILogger<BackgroundReadingService> logger)
        {
            _scopeFactory = scopeFactory;
            _simulator    = simulator;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackgroundReadingService started. Interval: {Interval}s", _interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Create a new DI scope so we can resolve the scoped DbContext
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var reading = new WindReading
                    {
                        WindSpeed   = _simulator.GetCurrentWindSpeed(),
                        Timestamp   = DateTime.UtcNow,
                        SensorId    = "SIM-001",
                        IsSimulated = true
                    };

                    db.WindReadings.Add(reading);
                    await db.SaveChangesAsync(stoppingToken);

                    // ── Check Alerts ───────────────────────────────────────────
                    await CheckAlertsAsync(scope.ServiceProvider, reading.WindSpeed, stoppingToken);

                    _logger.LogDebug("Saved simulated reading: {Speed} m/s at {Time}", reading.WindSpeed, reading.Timestamp);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error saving simulated wind reading.");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("BackgroundReadingService stopped.");
        }

        private async Task CheckAlertsAsync(IServiceProvider sp, decimal currentSpeed, CancellationToken ct)
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var notifier = sp.GetRequiredService<INotificationService>();

            // Find active alerts where threshold is exceeded
            // and hasn't been triggered in the last 15 minutes to avoid spam
            var quietPeriod = DateTime.UtcNow.AddMinutes(-15);
            
            var alerts = await db.AlertThresholds
                .Where(a => a.IsActive && currentSpeed >= a.SpeedThreshold)
                .Where(a => a.LastTriggeredAt == null || a.LastTriggeredAt <= quietPeriod)
                .ToListAsync(ct);

            foreach (var alert in alerts)
            {
                var msg = $"⚠️ ALERT: High wind speed detected! Current: {currentSpeed:F2} m/s. (Threshold: {alert.SpeedThreshold} m/s)";
                await notifier.SendAlertAsync(alert.UserId, msg, alert.NotificationMethod);

                alert.LastTriggeredAt = DateTime.UtcNow;
            }

            if (alerts.Any())
            {
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
