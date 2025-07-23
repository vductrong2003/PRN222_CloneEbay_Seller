using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IOverviewService
    {
        Task<OverviewDashboardViewModel> GetDashboardDataAsync(int sellerId);
        Task<ShopOverviewViewModel> GetShopOverviewAsync(int sellerId);
        Task<ShopStatsViewModel> GetShopStatsAsync(int sellerId);
        Task<PerformanceMetricsViewModel> GetPerformanceMetricsAsync(int sellerId);
        Task<List<TopProductViewModel>> GetTopProductsAsync(int sellerId, int count = 5);
        Task<List<RecentActivityViewModel>> GetRecentActivitiesAsync(int sellerId, int count = 10);
    }
}