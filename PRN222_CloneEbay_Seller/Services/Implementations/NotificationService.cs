using Microsoft.AspNetCore.SignalR;
using PRN222_CloneEbay_Seller.Hubs;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly CloneEbayDbContext _context;

        public NotificationService(IHubContext<NotificationHub> hubContext, CloneEbayDbContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }

        public async Task SendToAllUsersAsync(string message, string type = "info")
        {
            // Gửi thông báo đến tất cả User (không phải Seller)
            var users = _context.Users.Where(u => u.Role == "User").Select(u => u.Id).ToList();
            
            foreach (var userId in users)
            {
                await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", new
                {
                    message = message,
                    type = type,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    shouldReload = true
                });
            }
        }

        public async Task SendToUserAsync(int userId, string message, string type = "info")
        {
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", new
            {
                message = message,
                type = type,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                shouldReload = true
            });
        }

        // Listing notifications
        public async Task NotifyListingAddedAsync(Product listing)
        {
            var message = $"🆕 New listing available: {listing.Title} - ${listing.Price:N2}";
            await SendToAllUsersAsync(message, "success");
        }

        public async Task NotifyListingUpdatedAsync(Product listing)
        {
            var message = $"📝 Listing updated: {listing.Title} - Check out the latest changes!";
            await SendToAllUsersAsync(message, "info");
        }

        public async Task NotifyListingDeletedAsync(string listingTitle)
        {
            var message = $"❌ Listing no longer available: {listingTitle}";
            await SendToAllUsersAsync(message, "warning");
        }

        // Seller notifications for same seller on multiple devices
        public async Task NotifySellerAsync(int sellerId, string message, string type = "info")
        {
            await _hubContext.Clients.Group($"Seller_{sellerId}").SendAsync("ReceiveNotification", new
            {
                message = message,
                type = type,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                shouldReload = true
            });
        }

        public async Task NotifySellerListingChangedAsync(int sellerId, string action, string listingTitle)
        {
            var message = $"📋 Your listing '{listingTitle}' has been {action}";
            await NotifySellerAsync(sellerId, message, "info");
        }

        public async Task NotifySellerOrderChangedAsync(int sellerId, string action, int orderId)
        {
            var message = $"📦 Order #{orderId} has been {action}";
            await NotifySellerAsync(sellerId, message, "info");
        }

        public async Task NotifyOrderStatusChangedAsync(OrderTable order)
        {
            if (order.BuyerId.HasValue)
            {
                var message = $"📦 Order #{order.Id} status updated to: {order.Status}";
                await SendToUserAsync(order.BuyerId.Value, message, "info");
            }
        }
    }
}
