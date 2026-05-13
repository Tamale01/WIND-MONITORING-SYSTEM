// File: Controllers/WindApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WindMonitoringSystem.Data;
using WindMonitoringSystem.Models;
using WindMonitoringSystem.Services;

namespace WindMonitoringSystem.Controllers
{
    /// <summary>
    /// REST API for wind readings.
    /// GET  /api/wind/latest        – latest reading (auth required)
    /// GET  /api/wind/history       – readings for last N hours (auth required)
    /// GET  /api/wind/status        – public health-check endpoint
    /// POST /api/wind               – submit a real sensor reading (auth or API key)
    /// </summary>
    [ApiController]
    [Route("api/wind")]
    public class WindApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ApiLogger _apiLogger;
        private readonly IConfiguration _config;

        public WindApiController(ApplicationDbContext db, ApiLogger apiLogger, IConfiguration config)
        {
            _db        = db;
            _apiLogger = apiLogger;
            _config    = config;
        }

        // ── GET /api/wind/status (public) ──────────────────────────────────────
        /// <summary>Public health-check — returns system status without auth.</summary>
        [HttpGet("status")]
        [AllowAnonymous]
        public IActionResult Status()
        {
            _apiLogger.Log("/api/wind/status", "anonymous");
            return Ok(new { status = "online", utcTime = DateTime.UtcNow });
        }

        // ── GET /api/wind/latest (auth required) ──────────────────────────────
        /// <summary>Returns the single most recent wind reading.</summary>
        [HttpGet("latest")]
        [Authorize]
        public async Task<IActionResult> Latest()
        {
            var user = User.Identity?.Name ?? "anonymous";
            _apiLogger.Log("/api/wind/latest", user);

            var reading = await _db.WindReadings
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            if (reading == null)
                return NotFound(new { message = "No readings available." });

            return Ok(new
            {
                reading.Id,
                reading.WindSpeed,
                reading.Timestamp,
                reading.SensorId,
                reading.IsSimulated
            });
        }

        // ── GET /api/wind/history?hours=24 (auth required) ────────────────────
        /// <summary>Returns readings from the last N hours (default 24, max 168).</summary>
        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> History([FromQuery] int hours = 24)
        {
            var user = User.Identity?.Name ?? "anonymous";
            _apiLogger.Log($"/api/wind/history?hours={hours}", user);

            // Cap at 7 days to prevent excessive queries
            hours = Math.Clamp(hours, 1, 168);
            var since = DateTime.UtcNow.AddHours(-hours);

            var readings = await _db.WindReadings
                .Where(r => r.Timestamp >= since)
                .OrderBy(r => r.Timestamp)
                .Select(r => new
                {
                    r.Id,
                    r.WindSpeed,
                    r.Timestamp,
                    r.SensorId,
                    r.IsSimulated
                })
                .ToListAsync();

            // Summary statistics for the selected period
            var stats = readings.Any()
                ? new
                {
                    count   = readings.Count,
                    average = Math.Round(readings.Average(r => (double)r.WindSpeed), 2),
                    min     = readings.Min(r => r.WindSpeed),
                    max     = readings.Max(r => r.WindSpeed)
                }
                : null;

            return Ok(new { readings, stats });
        }

        // ── POST /api/wind (auth or API key) ──────────────────────────────────
        /// <summary>
        /// Accepts a real sensor reading. Requires either a logged-in user
        /// or a valid X-Api-Key header matching the value in appsettings.json.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PostReading([FromBody] WindReadingDto dto)
        {
            // Allow either authenticated user OR valid API key
            var apiKey        = _config["ApiKey"];
            var providedKey   = Request.Headers["X-Api-Key"].FirstOrDefault();
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var hasValidKey     = !string.IsNullOrEmpty(apiKey) && providedKey == apiKey;

            if (!isAuthenticated && !hasValidKey)
                return Unauthorized(new { message = "Provide a valid X-Api-Key header or log in." });

            // Validate wind speed range
            if (dto.WindSpeed < 0 || dto.WindSpeed > 200)
                return BadRequest(new { message = "WindSpeed must be between 0 and 200 m/s." });

            var user = User.Identity?.Name ?? "api-key-user";
            _apiLogger.Log("POST /api/wind", user);

            var reading = new WindReading
            {
                WindSpeed   = dto.WindSpeed,
                Timestamp   = DateTime.UtcNow,
                SensorId    = dto.SensorId,
                IsSimulated = false   // Real hardware reading
            };

            _db.WindReadings.Add(reading);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Latest), new { id = reading.Id }, new
            {
                reading.Id,
                reading.WindSpeed,
                reading.Timestamp,
                reading.SensorId,
                reading.IsSimulated
            });
        }
    }

    /// <summary>DTO for POST /api/wind request body.</summary>
    public class WindReadingDto
    {
        public decimal WindSpeed { get; set; }
        public string? SensorId  { get; set; }
    }
}
