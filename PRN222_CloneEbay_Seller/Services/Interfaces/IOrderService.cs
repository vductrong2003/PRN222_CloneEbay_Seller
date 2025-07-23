using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IOrderService
    {
        // Core Order Operations
        Task<List<OrderTable>> GetAllOrdersAsync();
        Task<List<OrderTable>> GetOrdersByStatusAsync(string status);
        Task<OrderTable?> GetOrderDetailsAsync(int orderId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        
        // Order Actions
        Task<bool> ShipOrderAsync(int orderId, string trackingNumber, string carrier);
        Task<bool> CancelOrderAsync(int orderId, string reason);
        Task<bool> ProcessRefundAsync(int orderId, decimal amount, string reason);
        
        // Statistics
        Task<Dictionary<string, int>> GetOrderStatisticsAsync();
        Task<decimal> GetTotalRevenueAsync();
        
        // Reviews
        Task<List<Review>> GetOrderProductReviewsByBuyerAsync(int orderId);
        
        // Status Validation
        (bool IsValid, string ErrorMessage) ValidateStatusTransition(string currentStatus, string newStatus);
        List<string> GetAllowedStatusTransitions(string currentStatus);
    }
}