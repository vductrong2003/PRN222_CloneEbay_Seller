using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Models;
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

        // GET: Performance
        public async Task<IActionResult> Index(int days = 30, int count = 15)
        {
            var sellerId = GetCurrentSellerId();
            
            var overview = await _performanceService.GetPerformanceOverviewAsync(sellerId, days);
            var revenueData = await _performanceService.GetRevenueDataAsync(sellerId, days);
            var recentSales = await _performanceService.GetRecentSalesAsync(sellerId, count, days);
            
            ViewBag.Overview = overview;
            ViewBag.RevenueData = revenueData;
            ViewBag.SelectedDays = days;
            ViewBag.SelectedCount = count;
            
            return View(recentSales);
        }

        // Helper method to get current seller ID
        private int GetCurrentSellerId()
        {
            // TODO: Get from session, authentication, or claims
            // For now, return a default value
            return 1;
        }

        // GET: Performance/Sales
        public async Task<IActionResult> Sales(int days = 30, int count = 20)
        {
            var sellerId = GetCurrentSellerId();
            
            var revenueData = await _performanceService.GetRevenueDataAsync(sellerId, days);
            var overview = await _performanceService.GetPerformanceOverviewAsync(sellerId, days);
            var recentSales = await _performanceService.GetRecentSalesAsync(sellerId, count, days);
            
            ViewBag.RevenueData = revenueData;
            ViewBag.Overview = overview;
            ViewBag.SelectedDays = days;
            ViewBag.SelectedCount = count;
            
            return View(recentSales);
        }

        // GET: Performance/GetRecentSales
        [HttpGet]
        public async Task<IActionResult> GetRecentSales(int days = 30, int count = 10)
        {
            var sellerId = GetCurrentSellerId();
            var sales = await _performanceService.GetRecentSalesAsync(sellerId, count, days);
            return Json(sales);
        }

        // GET: Performance/Search - Filter performance data với custom date range
        public async Task<IActionResult> Search(int days = 30, int count = 15, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var sellerId = GetCurrentSellerId();
            
            // Xử lý custom date range
            if (fromDate.HasValue && toDate.HasValue)
            {
                days = (int)(toDate.Value - fromDate.Value).TotalDays;
                if (days <= 0) days = 1; // Tối thiểu 1 ngày
            }

            var overview = await _performanceService.GetPerformanceOverviewAsync(sellerId, days);
            var revenueData = await _performanceService.GetRevenueDataAsync(sellerId, days);
            var recentSales = await _performanceService.GetRecentSalesAsync(sellerId, count, days);

            ViewBag.Overview = overview;
            ViewBag.RevenueData = revenueData;
            ViewBag.SelectedDays = days;
            ViewBag.SelectedCount = count;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View("Index", recentSales);
        }
    }
}