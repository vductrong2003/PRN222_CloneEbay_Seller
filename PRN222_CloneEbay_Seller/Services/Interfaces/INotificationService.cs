using Microsoft.AspNetCore.SignalR;
using PRN222_CloneEbay_Seller.Hubs;
using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendToAllUsersAsync(string message, string type = "info");
        Task SendToUserAsync(int userId, string message, string type = "info");
        
        // Listing notifications
        Task NotifyListingAddedAsync(Product listing);
        Task NotifyListingUpdatedAsync(Product listing);
        Task NotifyListingDeletedAsync(string listingTitle);
        
        // Order notifications
        Task NotifyOrderStatusChangedAsync(OrderTable order);
        
        // Seller notifications for multi-device sync
        Task NotifySellerAsync(int sellerId, string message, string type = "info");
        Task NotifySellerListingChangedAsync(int sellerId, string action, string listingTitle);
        Task NotifySellerOrderChangedAsync(int sellerId, string action, int orderId);
    }
}
