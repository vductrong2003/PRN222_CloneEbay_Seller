using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace PRN222_CloneEbay_Seller.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        public async Task LeaveUserGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        }

        public async Task JoinSellerGroup(string sellerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Seller_{sellerId}");
        }

        public async Task LeaveSellerGroup(string sellerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Seller_{sellerId}");
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("UserId")?.Value;
            var userRole = Context.User?.FindFirst("Role")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                if (userRole == "User")
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                }
                else if (userRole == "Seller")
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Seller_{userId}");
                }
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.User?.FindFirst("UserId")?.Value;
            var userRole = Context.User?.FindFirst("Role")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                if (userRole == "User")
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                }
                else if (userRole == "Seller")
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Seller_{userId}");
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
