using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IPerformanceService
    {
        Task<Dictionary<string, object>> GetPerformanceOverviewAsync(int days);
        Task<Dictionary<string, object>> GetRevenueChartDataAsync(int days);
        Task<List<OrderTable>> GetRecentSalesAsync(int count, int days);
        Task<Dictionary<string, int>> GetOrderStatisticsAsync(int days);
        Task<decimal> GetTotalRevenueAsync(int days);
    }
}