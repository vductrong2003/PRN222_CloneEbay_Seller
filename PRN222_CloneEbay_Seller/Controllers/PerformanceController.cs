using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class PerformanceController : Controller
    {
        private readonly IPerformanceService _performanceService;

        public PerformanceController(IPerformanceService performanceService)
        {
            _performanceService = performanceService;
        }

        public async Task<IActionResult> Index(int days = 30)
        {
            var overview = await _performanceService.GetPerformanceOverviewAsync(days);
            var revenueChartData = await _performanceService.GetRevenueChartDataAsync(days);
            var recentSales = await _performanceService.GetRecentSalesAsync(10, days);
            var orderStats = await _performanceService.GetOrderStatisticsAsync(days);
            
            ViewBag.Overview = overview;
            ViewBag.RevenueChartData = revenueChartData;
            ViewBag.OrderStats = orderStats;
            ViewBag.SelectedDays = days;
            
            return View(recentSales);
        }
    }
}