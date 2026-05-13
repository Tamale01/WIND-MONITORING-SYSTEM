// File: Controllers/ChartsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WindMonitoringSystem.Controllers
{
    /// <summary>
    /// Charts controller — requires authenticated user.
    /// Serves the Chart.js wind history view; data is fetched via the API.
    /// </summary>
    [Authorize]
    public class ChartsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
