using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IPerformanceService
    {
        Task<Dictionary<string, object>> GetPerformanceOverviewAsync(int days, int? sellerId = null);
        Task<Dictionary<string, object>> GetRevenueChartDataAsync(int days, int? sellerId = null);
        Task<List<OrderTable>> GetRecentSalesAsync(int count, int days, int? sellerId = null);
        Task<Dictionary<string, int>> GetOrderStatisticsAsync(int days, int? sellerId = null);
        Task<decimal> GetTotalRevenueAsync(int days, int? sellerId = null);
    }
}