using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class PerformanceService : IPerformanceService
    {
        private readonly CloneEbayDbContext _context;

        public PerformanceService(CloneEbayDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<string, object>> GetPerformanceOverviewAsync(int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);

            var orderStats = await GetOrderStatisticsAsync(days);
            var totalRevenue = await GetTotalRevenueAsync(days);
            var totalOrders = orderStats.GetValueOrDefault("Total", 0);
            var deliveredOrders = orderStats.GetValueOrDefault("Delivered", 0);

            // Calculate growth (comparing with previous period)
            var previousRevenue = await GetTotalRevenueAsync(days * 2, days);
            var revenueGrowth = previousRevenue > 0 ? ((totalRevenue - previousRevenue) / previousRevenue) * 100 : 0;

            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            return new Dictionary<string, object>
            {
                ["TotalRevenue"] = totalRevenue,
                ["TotalOrders"] = totalOrders,
                ["DeliveredOrders"] = deliveredOrders,
                ["PendingOrders"] = orderStats.GetValueOrDefault("Pending", 0),
                ["RevenueGrowth"] = Math.Round(revenueGrowth, 2),
                ["AverageOrderValue"] = Math.Round(averageOrderValue, 2),
                ["Period"] = days
            };
        }

        public async Task<Dictionary<string, object>> GetRevenueChartDataAsync(int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);

            // Get daily revenue data (only from delivered orders)
            var revenueData = await _context.OrderTables
                .Where(o => o.OrderDate >= startDate && o.Status == "Delivered")
                .GroupBy(o => o.OrderDate!.Value.Date)
                .Select(g => new {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalPrice ?? 0)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Fill missing dates with 0 revenue
            var allDates = new List<DateTime>();
            for (int i = 0; i < days; i++)
            {
                allDates.Add(DateTime.Now.AddDays(-days + i + 1).Date);
            }

            var chartData = allDates.Select(date => new {
                Date = date,
                Revenue = revenueData.FirstOrDefault(r => r.Date == date)?.Revenue ?? 0
            }).ToList();

            return new Dictionary<string, object>
            {
                ["Labels"] = chartData.Select(r => r.Date.ToString("MM/dd")).ToList(),
                ["Data"] = chartData.Select(r => r.Revenue).ToList(),
                ["TotalRevenue"] = chartData.Sum(r => r.Revenue)
            };
        }

        public async Task<List<OrderTable>> GetRecentSalesAsync(int count = 10, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);

            return await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderDate >= startDate)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetOrderStatisticsAsync(int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            var orders = await _context.OrderTables
                .Where(o => o.OrderDate >= startDate)
                .ToListAsync();

            return new Dictionary<string, int>
            {
                ["Total"] = orders.Count,
                ["Pending"] = orders.Count(o => o.Status == "Pending"),
                ["Paid"] = orders.Count(o => o.Status == "Paid"),
                ["Shipped"] = orders.Count(o => o.Status == "Shipped"),
                ["Delivered"] = orders.Count(o => o.Status == "Delivered"),
                ["Cancelled"] = orders.Count(o => o.Status == "Cancelled"),
                ["Refunded"] = orders.Count(o => o.Status == "Refunded")
            };
        }

        public async Task<decimal> GetTotalRevenueAsync(int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);

            // Only count revenue from delivered orders
            return await _context.OrderTables
                .Where(o => o.OrderDate >= startDate && o.Status == "Delivered")
                .SumAsync(o => o.TotalPrice ?? 0);
        }

        // Helper method for growth calculation
        private async Task<decimal> GetTotalRevenueAsync(int totalDays, int skipDays)
        {
            var endDate = DateTime.Now.AddDays(-skipDays);
            var startDate = endDate.AddDays(-totalDays);

            return await _context.OrderTables
                .Where(o => o.OrderDate >= startDate && o.OrderDate < endDate && o.Status == "Delivered")
                .SumAsync(o => o.TotalPrice ?? 0);
        }
    }
}