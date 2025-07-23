using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class PerformanceController : Controller
    {
        private readonly IPerformanceService _performanceService;
        private readonly IAccountService _accountService;

        public PerformanceController(IPerformanceService performanceService, IAccountService accountService)
        {
            _performanceService = performanceService;
            _accountService = accountService;
        }

        private async Task<bool> CheckSellerAccessAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return false;
            
            return await _accountService.CanAccessSellerFeaturesAsync(userId.Value);
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        public async Task<IActionResult> Index(int days = 30)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to access performance data.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var overview = await _performanceService.GetPerformanceOverviewAsync(days, userId);
                var revenueChartData = await _performanceService.GetRevenueChartDataAsync(days, userId);
                var recentSales = await _performanceService.GetRecentSalesAsync(10, days, userId);
                var orderStats = await _performanceService.GetOrderStatisticsAsync(days, userId);
                
                ViewBag.Overview = overview;
                ViewBag.RevenueChartData = revenueChartData;
                ViewBag.OrderStats = orderStats;
                ViewBag.SelectedDays = days;
                
                return View(recentSales);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading performance data.";
                return View(new List<object>());
            }
        }
    }
}