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
        
        Task<bool> ProcessReturnRequestAsync(int orderId, string reason, string action);
        Task<bool> ResolveDisputeAsync(int orderId, string resolution, string status);
        Task<List<OrderTable>> GetOrdersWithReturnsAsync();
        Task<List<OrderTable>> GetOrdersWithDisputesAsync();

        Task<List<OrderTable>> GetOrdersReadyForShippingAsync();
        Task<bool> GenerateShippingLabelAsync(int orderId, string carrier, decimal weight, string dimensions);
        Task<string> GetShippingLabelUrlAsync(int orderId);
        Task<List<string>> GenerateBulkShippingLabelsAsync(List<int> orderIds, string carrier);
        Task<decimal> CalculateShippingCostAsync(int orderId, string carrier, decimal weight, string dimensions);

        (bool IsValid, string ErrorMessage) ValidateStatusTransition(string currentStatus, string newStatus);
        List<string> GetAllowedStatusTransitions(string currentStatus);
        Task<bool> AdvanceOrderToNextStepAsync(int orderId);
        Task<Dictionary<string, bool>> GetAvailableActionsAsync(int orderId);
        Task<List<OrderTable>> GetOrdersBySellerAsync(int sellerId);
        Task<OrderTable> GetOrderDetailsByIdAsync(int orderId);
        Task<List<Review>> GetOrderProductReviewsByBuyerAsync(int orderId);
    }
}