using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly CloneEbayDbContext _context;

        // Định nghĩa quy tắc chuyển đổi status hợp lệ
        private readonly Dictionary<string, List<string>> _allowedStatusTransitions = new()
        {
            ["Pending"] = new List<string> { "Paid", "Cancelled", "Shipped" },
            ["Paid"] = new List<string> { "Processing", "Cancelled", "Refunded", "Shipped" },
            ["Processing"] = new List<string> { "Shipped", "Cancelled", "Refunded" },
            ["Shipped"] = new List<string> { "Delivered", "Returned" },
            ["Delivered"] = new List<string> { "Refunded" },
            ["Cancelled"] = new List<string>(), 
            ["Refunded"] = new List<string>(), 
            ["Returned"] = new List<string> { "Refunded" } 
        };

        public OrderService(CloneEbayDbContext context)
        {
            _context = context;
        }

        // Updated to filter by seller ID
        public async Task<List<OrderTable>> GetAllOrdersAsync(int? sellerId = null)
        {
            var orders = await _context.OrderTables
    .Include(o => o.Buyer)
    .Include(o => o.Address)
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
    .Include(o => o.Payments)
    .Include(o => o.ShippingInfos)
    .ToListAsync();

            if (sellerId.HasValue)
            {
                orders = orders
                    .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == sellerId.Value))
                    .ToList();
            }

            return orders.OrderByDescending(o => o.OrderDate).ToList();

        }

        public async Task<List<OrderTable>> GetOrdersByStatusAsync(string status, int? sellerId = null)
        {
            var query = _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.ShippingInfos)
                .Where(o => o.Status == status);

            // Filter by seller if provided
            if (sellerId.HasValue)
            {
                query = query.Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == sellerId.Value));
            }

            return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<OrderTable?> GetOrderDetailsAsync(int orderId, int? sellerId = null)
        {
            var query = _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.ShippingInfos)
                .Where(o => o.Id == orderId);

            // Filter by seller if provided
            if (sellerId.HasValue)
            {
                query = query.Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == sellerId.Value));
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, int? sellerId = null)
        {
            var order = await GetOrderDetailsAsync(orderId, sellerId);
            if (order == null) 
            {
                return false;
            }

            // Validate status transition
            var validationResult = ValidateStatusTransition(order.Status ?? "Pending", newStatus);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }

            order.Status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ShipOrderAsync(int orderId, string trackingNumber, string carrier, int? sellerId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await GetOrderDetailsAsync(orderId, sellerId);
                if (order == null) return false;

                var currentStatus = order.Status ?? "Pending";
                var allowedStatuses = new[] { "Pending", "Paid", "Processing" };
                
                if (!allowedStatuses.Contains(currentStatus))
                {
                    throw new InvalidOperationException($"Cannot ship order with status '{currentStatus}'. Order must be Pending, Paid or Processing.");
                }

                var validationResult = ValidateStatusTransition(currentStatus, "Shipped");
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(validationResult.ErrorMessage);
                }

                order.Status = "Shipped";

                // Add or update shipping information
                var existingShipping = order.ShippingInfos.FirstOrDefault();
                if (existingShipping != null)
                {
                    existingShipping.TrackingNumber = trackingNumber;
                    existingShipping.Carrier = carrier;
                    existingShipping.Status = "Shipped";
                    existingShipping.EstimatedArrival = DateTime.Now.AddDays(7);
                }
                else
                {
                    var shippingInfo = new ShippingInfo
                    {
                        OrderId = orderId,
                        TrackingNumber = trackingNumber,
                        Carrier = carrier,
                        Status = "Shipped",
                        EstimatedArrival = DateTime.Now.AddDays(7)
                    };
                    _context.ShippingInfos.Add(shippingInfo);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, string reason, int? sellerId = null)
        {
            var order = await GetOrderDetailsAsync(orderId, sellerId);
            if (order == null) return false;

            var currentStatus = order.Status ?? "Pending";
            var validationResult = ValidateStatusTransition(currentStatus, "Cancelled");
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }

            order.Status = "Cancelled";
            // Note: In a real application, you might want to add a cancellation reason field
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ProcessRefundAsync(int orderId, decimal amount, string reason, int? sellerId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await GetOrderDetailsAsync(orderId, sellerId);
                if (order == null) return false;

                var currentStatus = order.Status ?? "Pending";
                var validationResult = ValidateStatusTransition(currentStatus, "Refunded");
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(validationResult.ErrorMessage);
                }

                order.Status = "Refunded";

                // Create refund payment record
                var refundPayment = new Payment
                {
                    OrderId = orderId,
                    UserId = order.BuyerId,
                    Amount = -amount, // Negative amount for refund
                    Method = "Refund",
                    Status = "Completed",
                    PaidAt = DateTime.Now
                };
                _context.Payments.Add(refundPayment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetOrderStatisticsAsync(int? sellerId = null)
        {
            var query = _context.OrderTables.AsQueryable();

            // Filter by seller if provided
            if (sellerId.HasValue)
            {
                query = query.Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == sellerId.Value));
            }

            var statistics = new Dictionary<string, int>
            {
                ["Total"] = await query.CountAsync(),
                ["Pending"] = await query.CountAsync(o => o.Status == "Pending"),
                ["Paid"] = await query.CountAsync(o => o.Status == "Paid"),
                ["Processing"] = await query.CountAsync(o => o.Status == "Processing"),
                ["Shipped"] = await query.CountAsync(o => o.Status == "Shipped"),
                ["Delivered"] = await query.CountAsync(o => o.Status == "Delivered"),
                ["Cancelled"] = await query.CountAsync(o => o.Status == "Cancelled"),
                ["Refunded"] = await query.CountAsync(o => o.Status == "Refunded"),
                ["Returned"] = await query.CountAsync(o => o.Status == "Returned")
            };

            return statistics;
        }

        public async Task<decimal> GetTotalRevenueAsync(int? sellerId = null)
        {
            var query = _context.OrderTables
                .Where(o => o.Status == "Delivered" || o.Status == "Paid" || o.Status == "Shipped");

            // Filter by seller if provided
            if (sellerId.HasValue)
            {
                query = query.Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == sellerId.Value));
            }

            return await query.SumAsync(o => o.TotalPrice ?? 0);
        }

        public async Task<List<Review>> GetOrderProductReviewsByBuyerAsync(int orderId, int? sellerId = null)
        {
            var order = await GetOrderDetailsAsync(orderId, sellerId);

            if (order == null)
                return new List<Review>();

            var reviews = new List<Review>();
            foreach (var orderItem in order.OrderItems)
            {
                if (orderItem.Product != null)
                {
                    var productReviews = await _context.Reviews
                        .Include(r => r.Reviewer)
                        .Where(r => r.ProductId == orderItem.Product.Id)
                        .ToListAsync();
                    reviews.AddRange(productReviews);
                }
            }

            return reviews;
        }

        public (bool IsValid, string ErrorMessage) ValidateStatusTransition(string currentStatus, string newStatus)
        {
            if (!_allowedStatusTransitions.ContainsKey(currentStatus))
            {
                return (false, $"Invalid current status: {currentStatus}");
            }

            if (!_allowedStatusTransitions[currentStatus].Contains(newStatus))
            {
                var allowedStatuses = string.Join(", ", _allowedStatusTransitions[currentStatus]);
                return (false, $"Cannot change from '{currentStatus}' to '{newStatus}'. Allowed transitions: {allowedStatuses}");
            }

            return (true, string.Empty);
        }

        public List<string> GetAllowedStatusTransitions(string currentStatus)
        {
            if (_allowedStatusTransitions.ContainsKey(currentStatus))
            {
                return _allowedStatusTransitions[currentStatus];
            }
            return new List<string>();
        }
    }
}