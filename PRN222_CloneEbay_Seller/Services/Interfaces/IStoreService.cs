using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IStoreService
    {
        // Get stores by seller
        Task<List<Store>> GetStoresBySellerIdAsync(int sellerId);
        Task<Store?> GetStoreBySellerIdAsync(int sellerId); // Get single store by seller ID
        
        // Store CRUD operations
        Task<Store?> GetStoreByIdAsync(int storeId);
        Task<Store> CreateStoreAsync(Store store);
        Task<bool> UpdateStoreAsync(int storeId, Store store);
        Task<bool> DeleteStoreAsync(int storeId);
        
        // Store validation
        Task<bool> IsStoreNameUniqueAsync(string storeName, int? excludeStoreId = null);
        Task<bool> IsStoreOwnerAsync(int storeId, int sellerId);
        
        // Store statistics
        Task<Dictionary<string, object>> GetStoreStatisticsAsync(int storeId);
        Task<List<Product>> GetStoreProductsAsync(int storeId);
        
        // Store settings
        Task<bool> SetDefaultStoreAsync(int storeId, int sellerId);
        Task<Store?> GetDefaultStoreAsync(int sellerId);
        
        // Performance methods
        Task<Dictionary<string, object>> GetStorePerformanceAsync(int storeId, int days = 30);
        Task<List<dynamic>> GetSalesDataAsync(int storeId, int days = 30);
        Task<List<dynamic>> GetProductPerformanceAsync(int storeId, int days = 30);
    }
}