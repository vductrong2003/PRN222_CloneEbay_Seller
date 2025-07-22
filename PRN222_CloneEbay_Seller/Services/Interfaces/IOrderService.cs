using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IOrderService
    {
        Task<List<OrderTable>> GetAllOrdersAsync();
        Task<List<OrderTable>> GetOrdersByStatusAsync(string status);
        Task<List<OrderTable>> GetOrdersBySellerIdAsync(int sellerId);
        Task<OrderTable?> GetOrderByIdAsync(int orderId);
        Task<OrderTable?> GetOrderDetailsAsync(int orderId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<bool> ShipOrderAsync(int orderId, string trackingNumber, string carrier);
        Task<bool> CancelOrderAsync(int orderId, string reason);
        Task<Dictionary<string, int>> GetOrderStatisticsAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<List<OrderTable>> GetRecentOrdersAsync(int count = 10);
        Task<bool> ProcessRefundAsync(int orderId, decimal amount, string reason);
    }
}