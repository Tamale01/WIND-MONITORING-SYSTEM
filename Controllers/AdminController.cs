// File: Controllers/AdminController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WindMonitoringSystem.Data;
using WindMonitoringSystem.Models;

namespace WindMonitoringSystem.Controllers
{
    /// <summary>
    /// Admin controller — restricted to users in the "Admin" role.
    /// Provides actions to generate sample data and clear simulated readings.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var total     = await _db.WindReadings.CountAsync();
            var simulated = await _db.WindReadings.CountAsync(r => r.IsSimulated);
            ViewBag.Total     = total;
            ViewBag.Simulated = simulated;
            ViewBag.Real      = total - simulated;
            return View();
        }

        /// <summary>Generates 100 random simulated readings for testing.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSamples()
        {
            var rng  = new Random();
            var now  = DateTime.UtcNow;
            var list = Enumerable.Range(0, 100).Select(_ => new WindReading
            {
                WindSpeed   = Math.Round((decimal)(rng.NextDouble() * 30), 2),
                Timestamp   = now.AddMinutes(-rng.Next(0, 7 * 24 * 60)),
                SensorId    = "GEN-001",
                IsSimulated = true
            }).ToList();

            await _db.WindReadings.AddRangeAsync(list);
            await _db.SaveChangesAsync();

            TempData["Message"] = "✅ 100 sample readings generated successfully.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Deletes all simulated readings, keeping real sensor data.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearSimulated()
        {
            var simulated = _db.WindReadings.Where(r => r.IsSimulated);
            _db.WindReadings.RemoveRange(simulated);
            await _db.SaveChangesAsync();

            TempData["Message"] = "🗑️ All simulated readings cleared.";
            return RedirectToAction(nameof(Index));
        }
    }
}
