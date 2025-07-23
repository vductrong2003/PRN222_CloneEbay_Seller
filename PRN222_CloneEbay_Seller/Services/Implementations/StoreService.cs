using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class StoreService : IStoreService
    {
        private readonly CloneEbayDbContext _context;

        public StoreService(CloneEbayDbContext context)
        {
            _context = context;
        }

        public async Task<List<Store>> GetStoresBySellerIdAsync(int sellerId)
        {
            return await _context.Stores
                .Include(s => s.Seller)
                .Where(s => s.SellerId == sellerId)
                .OrderBy(s => s.StoreName)
                .ToListAsync();
        }

        public async Task<Store?> GetStoreByIdAsync(int storeId)
        {
            return await _context.Stores
                .Include(s => s.Seller)
                .FirstOrDefaultAsync(s => s.Id == storeId);
        }

        public async Task<Store> CreateStoreAsync(Store store)
        {
            _context.Stores.Add(store);
            await _context.SaveChangesAsync();
            return store;
        }

        public async Task<bool> UpdateStoreAsync(int storeId, Store store)
        {
            var existingStore = await _context.Stores.FindAsync(storeId);
            if (existingStore == null) return false;

            existingStore.StoreName = store.StoreName;
            existingStore.Description = store.Description;
            existingStore.BannerImageUrl = store.BannerImageUrl;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteStoreAsync(int storeId)
        {
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null) return false;

            // Get store's seller ID to check for products
            var sellerId = store.SellerId;
            
            // Check if seller has products (since products are linked to seller, not store)
            var hasProducts = await _context.Products.AnyAsync(p => p.SellerId == sellerId);
            if (hasProducts)
            {
                throw new InvalidOperationException("Cannot delete store. Seller still has products. Please remove all products first.");
            }

            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsStoreNameUniqueAsync(string storeName, int? excludeStoreId = null)
        {
            var query = _context.Stores.Where(s => s.StoreName == storeName);
            
            if (excludeStoreId.HasValue)
            {
                query = query.Where(s => s.Id != excludeStoreId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsStoreOwnerAsync(int storeId, int sellerId)
        {
            return await _context.Stores.AnyAsync(s => s.Id == storeId && s.SellerId == sellerId);
        }

        public async Task<Dictionary<string, object>> GetStoreStatisticsAsync(int storeId)
        {
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null) return new Dictionary<string, object>();

            var sellerId = store.SellerId;

            var products = await _context.Products
                .Where(p => p.SellerId == sellerId)
                .ToListAsync();

            var orders = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Product!.SellerId == sellerId)
                .ToListAsync();

            var totalRevenue = orders
                .Where(oi => oi.Order!.Status == "Delivered")
                .Sum(oi => (oi.UnitPrice ?? 0) * oi.Quantity);

            var reviews = await _context.Reviews
                .Where(r => r.Product!.SellerId == sellerId)
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating ?? 0) : 0;

            return new Dictionary<string, object>
            {
                ["TotalProducts"] = products.Count,
                ["ActiveProducts"] = products.Count(p => p.IsAuction == false || p.AuctionEndTime > DateTime.Now),
                ["TotalOrders"] = orders.Select(oi => oi.OrderId).Distinct().Count(),
                ["TotalRevenue"] = totalRevenue,
                ["TotalReviews"] = reviews.Count,
                ["AverageRating"] = Math.Round(averageRating, 1),
                ["PendingOrders"] = orders.Count(oi => oi.Order!.Status == "Pending"),
                ["ShippedOrders"] = orders.Count(oi => oi.Order!.Status == "Shipped")
            };
        }

        public async Task<List<Product>> GetStoreProductsAsync(int storeId)
        {
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null) return new List<Product>();

            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.SellerId == store.SellerId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<bool> SetDefaultStoreAsync(int storeId, int sellerId)
        {
            // For future implementation - could add IsDefault field to Store model
            return true;
        }

        public async Task<Store?> GetDefaultStoreAsync(int sellerId)
        {
            // Return the first store for now
            return await _context.Stores
                .FirstOrDefaultAsync(s => s.SellerId == sellerId);
        }

        public async Task<Dictionary<string, object>> GetStorePerformanceAsync(int storeId, int days = 30)
        {
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null) return new Dictionary<string, object>();

            var fromDate = DateTime.Now.AddDays(-days);
            var sellerId = store.SellerId;

            var orders = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Product!.SellerId == sellerId && oi.Order!.OrderDate >= fromDate)
                .ToListAsync();

            var totalSales = orders.Sum(oi => (oi.UnitPrice ?? 0) * oi.Quantity);
            var totalOrders = orders.Select(oi => oi.OrderId).Distinct().Count();
            var totalQuantity = orders.Sum(oi => oi.Quantity ?? 0);

            var dailySales = orders
                .GroupBy(oi => oi.Order!.OrderDate!.Value.Date)
                .Select(g => new { Date = g.Key, Sales = g.Sum(oi => (oi.UnitPrice ?? 0) * oi.Quantity) })
                .OrderBy(x => x.Date)
                .ToList();

            return new Dictionary<string, object>
            {
                ["TotalSales"] = totalSales,
                ["TotalOrders"] = totalOrders,
                ["TotalQuantity"] = totalQuantity,
                ["AverageDailySales"] = days > 0 ? totalSales / days : 0,
                ["DailySales"] = dailySales,
                ["Period"] = $"{days} days"
            };
        }

        public async Task<List<dynamic>> GetSalesDataAsync(int storeId, int days = 30)
        {
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null) return new List<dynamic>();

            var fromDate = DateTime.Now.AddDays(-days);
            var sellerId = store.SellerId;

            var salesData = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Product!.SellerId == sellerId && oi.Order!.OrderDate >= fromDate)
                .GroupBy(oi => oi.Order!.OrderDate!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    Sales = g.Sum(oi => (oi.UnitPrice ?? 0) * oi.Quantity),
                    Orders = g.Select(oi => oi.OrderId).Distinct().Count(),
                    Quantity = g.Sum(oi => oi.Quantity ?? 0)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return salesData.Cast<dynamic>().ToList();
        }

        public async Task<List<dynamic>> GetProductPerformanceAsync(int storeId, int days = 30)
        {
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null) return new List<dynamic>();

            var fromDate = DateTime.Now.AddDays(-days);
            var sellerId = store.SellerId;

            var productPerformance = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product!.SellerId == sellerId && oi.Order!.OrderDate >= fromDate)
                .GroupBy(oi => new { oi.ProductId, oi.Product!.Title })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Title,
                    TotalSales = g.Sum(oi => (oi.UnitPrice ?? 0) * oi.Quantity),
                    TotalQuantity = g.Sum(oi => oi.Quantity ?? 0),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalSales)
                .Take(10)
                .ToListAsync();

            return productPerformance.Cast<dynamic>().ToList();
        }
    }
}