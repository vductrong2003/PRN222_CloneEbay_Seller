using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IPerformanceService
    {
        // Performance overview and statistics
        Task<Dictionary<string, object>> GetPerformanceOverviewAsync(int sellerId, int days = 30);
        Task<Dictionary<string, object>> GetRevenueDataAsync(int sellerId, int days = 30);
        Task<Dictionary<string, object>> GetOrderStatisticsAsync(int sellerId, int days = 30);
        
        Task<List<Product>> GetTopProductsAsync(int sellerId, int days = 30);
        Task<Dictionary<string, object>> GetSalesByCategoryAsync(int sellerId, int days = 30);
        
        Task<decimal> GetTotalRevenueAsync(int sellerId, int days = 30);
        Task<int> GetTotalOrdersAsync(int sellerId, int days = 30);
        Task<int> GetTotalProductsSoldAsync(int sellerId, int days = 30);
        Task<int> GetActiveProductsAsync(int sellerId);
        
        Task<decimal> GetRevenueGrowthAsync(int sellerId, int days = 30);
        Task<decimal> GetAverageOrderValueAsync(int sellerId, int days = 30);
        Task<decimal> GetConversionRateAsync(int sellerId, int days = 30);
        
        Task<Dictionary<string, object>> GetSalesChartDataAsync(int sellerId, int days = 30);
        Task<Dictionary<string, object>> GetCategoryChartDataAsync(int sellerId, int days = 30);
        Task<List<OrderTable>> GetRecentSalesAsync(int sellerId, int count = 10, int days = 30);
    }
}