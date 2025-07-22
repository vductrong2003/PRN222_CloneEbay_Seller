using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly CloneEbayDbContext _context;

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
            }

            return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        }

        public async Task<List<OrderTable>> GetOrdersBySellerIdAsync(int sellerId)
        {
            return await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.ShippingInfos)
                .Where(o => o.OrderItems.Any(oi => oi.Product != null && oi.Product.SellerId == sellerId))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<OrderTable?> GetOrderByIdAsync(int orderId)
        {
            return await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.ShippingInfos)
                .FirstOrDefaultAsync(o => o.Id == orderId);
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
                .Include(o => o.Disputes)
                .Include(o => o.ReturnRequests)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.OrderTables.FindAsync(orderId);
            if (order == null) return false;

            order.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ShipOrderAsync(int orderId, string trackingNumber, string carrier)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update order status
                var order = await _context.OrderTables.FindAsync(orderId);
                if (order == null) return false;

                order.Status = "Shipped";

                // Create or update shipping info
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
                        EstimatedArrival = DateTime.Now.AddDays(7) // Default 7 days
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

                // Only allow cancellation for pending or paid orders
                if (order.Status != "Pending" && order.Status != "Paid") return false;

                order.Status = "Cancelled";

                // If order was paid, create refund record
                if (order.Status == "Paid" && order.Payments.Any())
                {
                    var payment = order.Payments.First();
                    payment.Status = "Refunded";
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
            return await _context.OrderTables
                .Where(o => o.Status == "Delivered" || o.Status == "Shipped")
                .SumAsync(o => o.TotalPrice ?? 0);
        }

        public async Task<List<OrderTable>> GetRecentOrdersAsync(int count = 10)
        {
            return await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .ToListAsync();
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

                var payment = order.Payments.FirstOrDefault();
                if (payment == null) return false;

                // Create refund payment record
                var refundPayment = new Payment
                {
                    OrderId = orderId,
                    UserId = order.BuyerId,
                    Amount = -amount, // Negative amount for refund
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
    }
}