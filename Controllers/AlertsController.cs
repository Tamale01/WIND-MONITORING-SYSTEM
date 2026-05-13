// File: Controllers/AlertsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WindMonitoringSystem.Data;
using WindMonitoringSystem.Models;

namespace WindMonitoringSystem.Controllers
{
    [Authorize]
    public class AlertsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public AlertsController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var alerts = await _db.AlertThresholds
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(alerts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(decimal speedThreshold, NotificationType notificationMethod)
        {
            if (speedThreshold < 0 || speedThreshold > 200)
            {
                TempData["Error"] = "Threshold must be between 0 and 200 m/s.";
                return RedirectToAction(nameof(Index));
            }

            var alert = new AlertThreshold
            {
                UserId = _userManager.GetUserId(User)!,
                SpeedThreshold = speedThreshold,
                NotificationMethod = notificationMethod,
                IsActive = true
            };

            _db.AlertThresholds.Add(alert);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Alert created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var userId = _userManager.GetUserId(User);
            var alert = await _db.AlertThresholds.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            
            if (alert != null)
            {
                alert.IsActive = !alert.IsActive;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var alert = await _db.AlertThresholds.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (alert != null)
            {
                _db.AlertThresholds.Remove(alert);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
