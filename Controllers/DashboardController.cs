// File: Controllers/DashboardController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WindMonitoringSystem.Data;

namespace WindMonitoringSystem.Controllers
{
    /// <summary>
    /// Dashboard controller — requires authenticated user.
    /// Shows live wind speed and last 10 readings.
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>Main dashboard view with last 10 readings loaded server-side.</summary>
        public async Task<IActionResult> Index()
        {
            // Fetch latest 10 readings for the initial table render
            var latest = await _db.WindReadings
                .OrderByDescending(r => r.Timestamp)
                .Take(10)
                .ToListAsync();

            return View(latest);
        }
    }
}
