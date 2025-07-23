using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class OverviewController : Controller
    {
        private readonly IOverviewService _overviewService;

        public OverviewController(IOverviewService overviewService)
        {
            _overviewService = overviewService;
        }

        // GET: Overview/Index
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var dashboardData = await _overviewService.GetDashboardDataAsync(userId.Value);
                if (dashboardData == null)
                {
                    TempData["Error"] = "Unable to load overview data.";
                    return RedirectToAction("Index", "Home");
                }

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading the overview.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}