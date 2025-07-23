using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class OverviewService : IOverviewService
    {
        private readonly CloneEbayDbContext _context;

        public OverviewService(CloneEbayDbContext context)
        {
            _context = context;
        }

        public async Task<OverviewDashboardViewModel> GetDashboardDataAsync(int sellerId)
        {
            var dashboard = new OverviewDashboardViewModel
            {
                ShopOverview = await GetShopOverviewAsync(sellerId),
                ShopStats = await GetShopStatsAsync(sellerId),
                PerformanceMetrics = await GetPerformanceMetricsAsync(sellerId),
                TopProducts = await GetTopProductsAsync(sellerId),
                RecentActivities = await GetRecentActivitiesAsync(sellerId)
            };

            return dashboard;
        }

        public async Task<ShopOverviewViewModel> GetShopOverviewAsync(int sellerId)
        {
            var store = await _context.Stores
                .Include(s => s.Seller)
                .FirstOrDefaultAsync(s => s.SellerId == sellerId);

            if (store == null)
            {
                return new ShopOverviewViewModel();
            }

            var totalReviews = await _context.Reviews
                .Where(r => r.Product.SellerId == sellerId)
                .CountAsync();

            var averageRating = totalReviews > 0 ? 
                await _context.Reviews
                    .Where(r => r.Product.SellerId == sellerId)
                    .AverageAsync(r => (double)r.Rating) : 0;

            return new ShopOverviewViewModel
            {
                ShopName = store.StoreName ?? "Cửa hàng",
                ShopDescription = store.Description ?? "Chào mừng đến với cửa hàng của chúng tôi!",
                JoinDate = DateTime.Now, // Store doesn't have CreatedDate, using current date as fallback
                TotalFollowers = 0, // No followers feature in current models
                TotalReviews = totalReviews,
                AverageRating = averageRating
            };
        }

        public async Task<ShopStatsViewModel> GetShopStatsAsync(int sellerId)
        {
            var totalProducts = await _context.Products
                .Where(p => p.SellerId == sellerId)
                .CountAsync();

            var activeListings = await _context.Products
                .Where(p => p.SellerId == sellerId && p.Status == "Active")
                .CountAsync();

            var totalOrders = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            var totalSales = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.Status == "Completed")
                .SumAsync(oi => oi.Quantity ?? 0);

            var totalRevenue = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.Status == "Completed")
                .SumAsync(oi => (oi.UnitPrice ?? 0) * (oi.Quantity ?? 0));

            var pendingOrders = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId && 
                           (oi.Order.Status == "Pending" || oi.Order.Status == "Processing"))
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            return new ShopStatsViewModel
            {
                TotalProducts = totalProducts,
                ActiveListings = activeListings,
                TotalSales = totalSales,
                TotalRevenue = totalRevenue,
                PendingOrders = pendingOrders,
                TotalOrders = totalOrders
            };
        }

        public async Task<PerformanceMetricsViewModel> GetPerformanceMetricsAsync(int sellerId)
        {
            var totalOrders = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId)
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            if (totalOrders == 0)
            {
                return new PerformanceMetricsViewModel();
            }

            var completedOrders = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.Status == "Completed")
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            var refundedOrders = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.Status == "Refunded")
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            var cancelledOrders = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.Status == "Cancelled")
                .Select(oi => oi.OrderId)
                .Distinct()
                .CountAsync();

            // Since Product doesn't have ViewCount, we'll use 0 for now
            var totalViews = 0;

            var averageOrderValue = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.Status == "Completed")
                .GroupBy(oi => oi.OrderId)
                .Select(g => g.Sum(oi => (oi.UnitPrice ?? 0) * (oi.Quantity ?? 0)))
                .DefaultIfEmpty(0)
                .AverageAsync();

            var returnRequests = await _context.ReturnRequests
                .Where(rr => rr.Order.OrderItems.Any(oi => oi.Product.SellerId == sellerId))
                .CountAsync();

            var activeDisputes = await _context.Disputes
                .Where(d => d.Order.OrderItems.Any(oi => oi.Product.SellerId == sellerId) && d.Status == "Open")
                .CountAsync();

            return new PerformanceMetricsViewModel
            {
                SuccessfulOrdersPercentage = totalOrders > 0 ? (decimal)completedOrders / totalOrders * 100 : 0,
                RefundedOrdersPercentage = totalOrders > 0 ? (decimal)refundedOrders / totalOrders * 100 : 0,
                CancelledOrdersPercentage = totalOrders > 0 ? (decimal)cancelledOrders / totalOrders * 100 : 0,
                TotalViews = totalViews,
                TotalWatchers = 0, // No watchers feature in current models
                ConversionRate = totalViews > 0 ? (decimal)completedOrders / totalViews * 100 : 0,
                AverageOrderValue = averageOrderValue,
                ReturnRequests = returnRequests,
                ActiveDisputes = activeDisputes
            };
        }

        public async Task<List<TopProductViewModel>> GetTopProductsAsync(int sellerId, int count = 5)
        {
            var topProducts = await _context.Products
                .Where(p => p.SellerId == sellerId)
                .Select(p => new TopProductViewModel
                {
                    ProductId = p.Id,
                    ProductName = p.Title ?? "Sản phẩm",
                    CategoryName = p.Category.Name ?? "Danh mục",
                    Price = p.Price ?? 0,
                    InventoryCount = p.Inventories.Sum(i => i.Quantity ?? 0),
                    TotalSales = _context.OrderItems
                        .Where(oi => oi.ProductId == p.Id && oi.Order.Status == "Completed")
                        .Sum(oi => oi.Quantity ?? 0),
                    Revenue = _context.OrderItems
                        .Where(oi => oi.ProductId == p.Id && oi.Order.Status == "Completed")
                        .Sum(oi => (oi.UnitPrice ?? 0) * (oi.Quantity ?? 0)),
                    TotalViews = 0 // Product doesn't have ViewCount
                })
                .OrderByDescending(p => p.TotalSales)
                .ThenByDescending(p => p.Revenue)
                .Take(count)
                .ToListAsync();

            return topProducts;
        }

        public async Task<List<RecentActivityViewModel>> GetRecentActivitiesAsync(int sellerId, int count = 10)
        {
            var activities = new List<RecentActivityViewModel>();

            // Recent sales
            var recentSales = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.Status == "Completed")
                .OrderByDescending(oi => oi.Order.OrderDate)
                .Take(count / 2)
                .Select(oi => new RecentActivityViewModel
                {
                    ActivityType = "sale",
                    Title = "Đã bán sản phẩm",
                    Description = $"{oi.Product.Title} - Số lượng: {oi.Quantity}",
                    Timestamp = oi.Order.OrderDate ?? DateTime.Now,
                    RelatedEntityId = oi.OrderId.ToString(),
                    Icon = "💰"
                })
                .ToListAsync();

            activities.AddRange(recentSales);

            // Recent orders
            var recentOrders = await _context.OrderItems
                .Where(oi => oi.Product.SellerId == sellerId && oi.Order.Status == "Pending")
                .OrderByDescending(oi => oi.Order.OrderDate)
                .Take(count / 2)
                .Select(oi => new RecentActivityViewModel
                {
                    ActivityType = "order",
                    Title = "Đơn hàng mới",
                    Description = $"Đơn hàng #{oi.OrderId} - {oi.Product.Title}",
                    Timestamp = oi.Order.OrderDate ?? DateTime.Now,
                    RelatedEntityId = oi.OrderId.ToString(),
                    Icon = "📦"
                })
                .ToListAsync();

            activities.AddRange(recentOrders);

            return activities
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToList();
        }
    }
}