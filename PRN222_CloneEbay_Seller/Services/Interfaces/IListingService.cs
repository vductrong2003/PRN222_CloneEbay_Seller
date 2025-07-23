using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IListingService
    {
        // Product listing management
        Task<List<Product>> GetAllProductsAsync(int sellerId);
        Task<List<Product>> GetProductsByStatusAsync(int sellerId, string status);
        Task<Product?> GetProductDetailsAsync(int productId);
        Task<Dictionary<string, int>> GetProductStatisticsAsync(int sellerId);
        
        // Product CRUD operations
        Task<bool> CreateProductAsync(Product product);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int productId);
        Task<bool> UpdateProductStatusAsync(int productId, string status);
        
        // Inventory management
        Task<List<Inventory>> GetInventoryByProductAsync(int productId);
        Task<bool> UpdateInventoryAsync(int productId, int quantity);
        
        // Category and search
        Task<List<Category>> GetAllCategoriesAsync();
        Task<List<Product>> SearchProductsAsync(int sellerId, string query, int? categoryId = null);
        
        // Product performance
        Task<Dictionary<string, object>> GetProductPerformanceAsync(int productId, int days = 30);
        Task<List<Product>> GetTopPerformingProductsAsync(int sellerId, int count = 10);
        
        // Bulk operations
        Task<bool> BulkUpdateStatusAsync(List<int> productIds, string status);
        Task<bool> BulkDeleteAsync(List<int> productIds);
        
        // Draft and scheduling
        Task<List<Product>> GetDraftProductsAsync(int sellerId);
        Task<List<Product>> GetScheduledProductsAsync(int sellerId);
        Task<bool> ScheduleProductAsync(int productId, DateTime publishDate);
        Task<List<Product>> GetDraftsBySellerIdAsync(int sellerId);
    }
}
