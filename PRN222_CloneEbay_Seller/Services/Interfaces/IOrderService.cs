using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IOrderService
    {
        // Core Order Operations - Updated to support seller filtering
        Task<List<OrderTable>> GetAllOrdersAsync(int? sellerId = null);
        Task<List<OrderTable>> GetOrdersByStatusAsync(string status, int? sellerId = null);
        Task<OrderTable?> GetOrderDetailsAsync(int orderId, int? sellerId = null);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status, int? sellerId = null);
        
        // Order Actions - Updated to support seller filtering
        Task<bool> ShipOrderAsync(int orderId, string trackingNumber, string carrier, int? sellerId = null);
        Task<bool> CancelOrderAsync(int orderId, string reason, int? sellerId = null);
        Task<bool> ProcessRefundAsync(int orderId, decimal amount, string reason, int? sellerId = null);
        
        // Statistics - Updated to support seller filtering
        Task<Dictionary<string, int>> GetOrderStatisticsAsync(int? sellerId = null);
        Task<decimal> GetTotalRevenueAsync(int? sellerId = null);
        
        // Reviews - Updated to support seller filtering
        Task<List<Review>> GetOrderProductReviewsByBuyerAsync(int orderId, int? sellerId = null);
        
        // Status Validation
        (bool IsValid, string ErrorMessage) ValidateStatusTransition(string currentStatus, string newStatus);
        List<string> GetAllowedStatusTransitions(string currentStatus);
    }
}