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

        public async Task<Dictionary<string, object>> GetPerformanceOverviewAsync(int sellerId, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            var totalRevenue = await GetTotalRevenueAsync(sellerId, days);
            var totalOrders = await GetTotalOrdersAsync(sellerId, days);
            var totalProductsSold = await GetTotalProductsSoldAsync(sellerId, days);
            var activeProducts = await GetActiveProductsAsync(sellerId);
            var revenueGrowth = await GetRevenueGrowthAsync(sellerId, days);
            var averageOrderValue = await GetAverageOrderValueAsync(sellerId, days);
            var conversionRate = await GetConversionRateAsync(sellerId, days);

            return new Dictionary<string, object>
            {
                ["TotalRevenue"] = totalRevenue,
                ["TotalOrders"] = totalOrders,
                ["TotalProductsSold"] = totalProductsSold,
                ["ActiveProducts"] = activeProducts,
                ["RevenueGrowth"] = Math.Round(revenueGrowth, 2),
                ["AverageOrderValue"] = Math.Round(averageOrderValue, 2),
                ["ConversionRate"] = Math.Round(conversionRate, 2),
                ["Period"] = days
            };
        }

        public async Task<Dictionary<string, object>> GetRevenueDataAsync(int sellerId, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            var revenueData = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.SellerId == sellerId && 
                           oi.Order.OrderDate >= startDate)
                .GroupBy(oi => oi.Order.OrderDate.Value.Date)
                .Select(g => new {
                    Date = g.Key,
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var totalRevenue = revenueData.Sum(r => r.Revenue);
            var averageDaily = revenueData.Any() ? totalRevenue / revenueData.Count : 0;

            return new Dictionary<string, object>
            {
                ["Labels"] = revenueData.Select(r => r.Date.ToString("MM/dd")).ToList(),
                ["Data"] = revenueData.Select(r => r.Revenue).ToList(),
                ["TotalRevenue"] = totalRevenue,
                ["AverageDaily"] = Math.Round((decimal)averageDaily, 2),
                ["GrowthRate"] = await GetRevenueGrowthAsync(sellerId, days)
            };
        }

        public async Task<Dictionary<string, object>> GetOrderStatisticsAsync(int sellerId, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            var orderStats = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.SellerId == sellerId && 
                           oi.Order.OrderDate >= startDate)
                .GroupBy(oi => oi.Order.OrderDate.Value.Date)
                .Select(g => new {
                    Date = g.Key,
                    Orders = g.Select(oi => oi.OrderId).Distinct().Count(),
                    Items = g.Sum(oi => oi.Quantity)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var totalOrders = orderStats.Sum(o => o.Orders);
            var totalItems = orderStats.Sum(o => o.Items);
            var averageOrdersPerDay = orderStats.Any() ? (decimal)totalOrders / orderStats.Count : 0;

            return new Dictionary<string, object>
            {
                ["Labels"] = orderStats.Select(o => o.Date.ToString("MM/dd")).ToList(),
                ["OrdersData"] = orderStats.Select(o => o.Orders).ToList(),
                ["ItemsData"] = orderStats.Select(o => o.Items).ToList(),
                ["TotalOrders"] = totalOrders,
                ["TotalItems"] = totalItems,
                ["AverageOrdersPerDay"] = Math.Round(averageOrdersPerDay, 2)
            };
        }

        public async Task<List<Product>> GetTopProductsAsync(int sellerId, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            var topProductIds = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.SellerId == sellerId && 
                           oi.Order.OrderDate >= startDate)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new {
                    ProductId = g.Key,
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(10)
                .Select(p => p.ProductId)
                .ToListAsync();

            return await _context.Products
                .Where(p => topProductIds.Contains(p.Id))
                .ToListAsync();
        }

        public async Task<Dictionary<string, object>> GetSalesByCategoryAsync(int sellerId, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            var categoryStats = await _context.OrderItems
                .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.SellerId == sellerId && 
                           oi.Order.OrderDate >= startDate)
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new {
                    CategoryName = g.Key ?? "Uncategorized",
                    TotalSold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                    ProductCount = g.Select(oi => oi.ProductId).Distinct().Count(),
                    AveragePrice = g.Average(oi => oi.UnitPrice)
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["Labels"] = categoryStats.Select(c => c.CategoryName).ToList(),
                ["RevenueData"] = categoryStats.Select(c => c.TotalRevenue).ToList(),
                ["SoldData"] = categoryStats.Select(c => c.TotalSold).ToList(),
                ["Details"] = categoryStats
            };
        }

        public async Task<decimal> GetTotalRevenueAsync(int sellerId, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            var revenue = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.SellerId == sellerId && 
                           oi.Order.OrderDate >= startDate)
                .SumAsync(oi => oi.Quantity * oi.UnitPrice);

            return revenue ?? 0;
        }

        public async Task<int> GetTotalOrdersAsync(int sellerId, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            var orders = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.SellerId == sellerId && 
                           oi.Order.OrderDate >= startDate)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            return orders;
        }

        public async Task<int> GetTotalProductsSoldAsync(int sellerId, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            var productsSold = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.SellerId == sellerId && 
                           oi.Order.OrderDate >= startDate)
                .SumAsync(oi => oi.Quantity);

            return productsSold ?? 0;
        }

        public async Task<int> GetActiveProductsAsync(int sellerId)
        {
            return await _context.Products
                .CountAsync(p => p.SellerId == sellerId);
        }

        public async Task<decimal> GetRevenueGrowthAsync(int sellerId, int days = 30)
        {
            var currentRevenue = await GetTotalRevenueAsync(sellerId, days);
            var previousStart = DateTime.Now.AddDays(-days * 2);
            var previousEnd = DateTime.Now.AddDays(-days);
            
            var previousRevenue = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.SellerId == sellerId && 
                           oi.Order.OrderDate >= previousStart &&
                           oi.Order.OrderDate < previousEnd)
                .SumAsync(oi => oi.Quantity * oi.UnitPrice);

            if (previousRevenue == null || previousRevenue == 0)
                return currentRevenue > 0 ? 100 : 0;

            return ((currentRevenue - previousRevenue.Value) / previousRevenue.Value) * 100;
        }

        public async Task<decimal> GetAverageOrderValueAsync(int sellerId, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            var orderValues = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.SellerId == sellerId && 
                           oi.Order.OrderDate >= startDate)
                .GroupBy(oi => oi.OrderId)
                .Select(g => g.Sum(oi => oi.Quantity * oi.UnitPrice))
                .ToListAsync();

            return (decimal)(orderValues.Any() ? orderValues.Average() : 0);
        }

        public async Task<decimal> GetConversionRateAsync(int sellerId, int days = 30)
        {
            // Simplified calculation - would need actual visitor/view data for accurate conversion rate
            var totalOrders = await GetTotalOrdersAsync(sellerId, days);
            var activeProducts = await GetActiveProductsAsync(sellerId);
            
            if (activeProducts == 0) return 0;
            
            // Estimate conversion rate based on orders per product
            return (decimal)totalOrders / activeProducts * 10; // Simplified calculation
        }

        public async Task<Dictionary<string, object>> GetSalesChartDataAsync(int sellerId, int days = 30)
        {
            return await GetRevenueDataAsync(sellerId, days);
        }

        public async Task<Dictionary<string, object>> GetCategoryChartDataAsync(int sellerId, int days = 30)
        {
            return await GetSalesByCategoryAsync(sellerId, days);
        }

        public async Task<List<OrderTable>> GetRecentSalesAsync(int sellerId, int count = 10, int days = 30)
        {
            var recentOrderIds = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.SellerId == sellerId)
                .Select(oi => oi.OrderId)
                .Distinct()
                .OrderByDescending(id => id)
                .Take(count)
                .ToListAsync();

            return await _context.OrderTables
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => recentOrderIds.Contains(o.Id))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}