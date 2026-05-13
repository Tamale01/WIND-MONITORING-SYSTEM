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
    }
}
