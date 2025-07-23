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

        public async Task<List<OrderTable>> GetAllOrdersAsync()
        {
            return await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.ShippingInfos)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<OrderTable>> GetOrdersByStatusAsync(string status)
        {
            var query = _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.ShippingInfos)
                .AsQueryable();

            switch (status.ToLower())
            {
                case "awaiting-payment":
                case "pending":
                    query = query.Where(o => o.Status == "Pending");
                    break;
                case "awaiting-shipment":
                case "paid":
                    query = query.Where(o => o.Status == "Paid");
                    break;
                case "shipped":
                    query = query.Where(o => o.Status == "Shipped");
                    break;
                case "delivered":
                case "completed":
                    query = query.Where(o => o.Status == "Delivered");
                    break;
                case "cancelled":
                    query = query.Where(o => o.Status == "Cancelled");
                    break;
                case "refunded":
                    query = query.Where(o => o.Status == "Refunded");
                    break;
            }

            return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<OrderTable?> GetOrderDetailsAsync(int orderId)
        {
            return await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product!.Category)
                .Include(o => o.Payments)
                .Include(o => o.ShippingInfos)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await _context.OrderTables.FindAsync(orderId);
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

        public async Task<bool> ShipOrderAsync(int orderId, string trackingNumber, string carrier)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.OrderTables.FindAsync(orderId);
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

                var shippingInfo = await _context.ShippingInfos
                    .FirstOrDefaultAsync(s => s.OrderId == orderId);

                if (shippingInfo == null)
                {
                    shippingInfo = new ShippingInfo
                    {
                        OrderId = orderId,
                        TrackingNumber = trackingNumber,
                        Carrier = carrier,
                        Status = "Shipped",
                        EstimatedArrival = DateTime.Now.AddDays(7)
                    };
                    _context.ShippingInfos.Add(shippingInfo);
                }
                else
                {
                    shippingInfo.TrackingNumber = trackingNumber;
                    shippingInfo.Carrier = carrier;
                    shippingInfo.Status = "Shipped";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.OrderTables
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return false;

                var currentStatus = order.Status ?? "Pending";
                
                var validationResult = ValidateStatusTransition(currentStatus, "Cancelled");
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(validationResult.ErrorMessage);
                }

                order.Status = "Cancelled";

                if (currentStatus == "Paid" && order.Payments.Any())
                {
                    var payment = order.Payments.First();
                    payment.Status = "Cancelled";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> ProcessRefundAsync(int orderId, decimal amount, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.OrderTables
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return false;

                var currentStatus = order.Status ?? "Pending";
                var validationResult = ValidateStatusTransition(currentStatus, "Refunded");
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(validationResult.ErrorMessage);
                }

                var payment = order.Payments.FirstOrDefault();
                if (payment == null) return false;

                var refundPayment = new Payment
                {
                    OrderId = orderId,
                    UserId = order.BuyerId,
                    Amount = -amount,
                    Method = payment.Method,
                    Status = "Refunded",
                    PaidAt = DateTime.Now
                };

                _context.Payments.Add(refundPayment);
                order.Status = "Refunded";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<Dictionary<string, int>> GetOrderStatisticsAsync()
        {
            var orders = await _context.OrderTables.ToListAsync();
            
            return new Dictionary<string, int>
            {
                ["Total"] = orders.Count,
                ["Pending"] = orders.Count(o => o.Status == "Pending"),
                ["Paid"] = orders.Count(o => o.Status == "Paid"),
                ["Shipped"] = orders.Count(o => o.Status == "Shipped"),
                ["Delivered"] = orders.Count(o => o.Status == "Delivered"),
                ["Cancelled"] = orders.Count(o => o.Status == "Cancelled")
            };
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            // CHỈ TÍNH REVENUE TỪ ORDERS ĐÃ DELIVERED
            return await _context.OrderTables
                .Where(o => o.Status == "Delivered")
                .SumAsync(o => o.TotalPrice ?? 0);
        }

        public async Task<List<Review>> GetOrderProductReviewsByBuyerAsync(int orderId)
        {
            var order = await _context.OrderTables
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

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