using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    // Enum để định nghĩa các status hợp lệ và thứ tự chuyển đổi
    public enum OrderStatusFlow
    {
        Pending = 1,        // Chờ thanh toán
        Paid = 2,          // Đã thanh toán, chờ gửi hàng
        Processing = 3,     // Đang chuẩn bị hàng
        Shipped = 4,       // Đã gửi hàng
        Delivered = 5,     // Đã giao hàng thành công
        Cancelled = 99,    // Đã hủy (có thể từ Pending hoặc Paid)
        Refunded = 98,     // Đã hoàn tiền
        Returned = 97      // Đã trả hàng
    }

    public class OrderService : IOrderService
    {
        private readonly CloneEbayDbContext _context;

        // Định nghĩa quy tắc chuyển đổi status hợp lệ
        private readonly Dictionary<string, List<string>> _allowedStatusTransitions = new()
        {
            ["Pending"] = new List<string> { "Paid", "Cancelled" },
            ["Paid"] = new List<string> { "Processing", "Cancelled", "Refunded" },
            ["Processing"] = new List<string> { "Shipped", "Cancelled", "Refunded" },
            ["Shipped"] = new List<string> { "Delivered", "Returned" },
            ["Delivered"] = new List<string>(), // ✅ KHÓA - Không cho phép chuyển từ Delivered
            ["Cancelled"] = new List<string>(), // Không thể chuyển từ Cancelled
            ["Refunded"] = new List<string>(), // Không thể chuyển từ Refunded
            ["Returned"] = new List<string> { "Refunded" } // Có thể hoàn tiền sau khi trả hàng
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
                .Include(o => o.ReturnRequests)
                .Include(o => o.Disputes)
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
                case "returns":
                case "returned":
                    query = query.Where(o => o.ReturnRequests.Any(r => r.Status == "Approved" || r.Status == "Processing"));
                    break;
                case "disputes":
                case "dispute":
                    query = query.Where(o => o.Disputes.Any(d => d.Status == "Open" || d.Status == "Under Review"));
                    break;
                case "refunded":
                    query = query.Where(o => o.Status == "Refunded");
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

        // Method để validate việc chuyển đổi status
        public (bool IsValid, string ErrorMessage) ValidateStatusTransition(string currentStatus, string newStatus)
        {
            // Kiểm tra nếu status hiện tại không tồn tại trong quy tắc
            if (!_allowedStatusTransitions.ContainsKey(currentStatus))
            {
                return (false, $"Invalid current status: {currentStatus}");
            }

            // Kiểm tra nếu status mới không được phép chuyển từ status hiện tại
            if (!_allowedStatusTransitions[currentStatus].Contains(newStatus))
            {
                var allowedStatuses = string.Join(", ", _allowedStatusTransitions[currentStatus]);
                return (false, $"Cannot change from '{currentStatus}' to '{newStatus}'. Allowed transitions: {allowedStatuses}");
            }

            return (true, string.Empty);
        }

        // Method để lấy danh sách status có thể chuyển đến
        public List<string> GetAllowedStatusTransitions(string currentStatus)
        {
            if (_allowedStatusTransitions.ContainsKey(currentStatus))
            {
                return _allowedStatusTransitions[currentStatus];
            }
            return new List<string>();
        }

        // Cập nhật lại ShipOrderAsync để tuân theo quy tắc
        public async Task<bool> ShipOrderAsync(int orderId, string trackingNumber, string carrier)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.OrderTables.FindAsync(orderId);
                if (order == null) return false;

                // Kiểm tra status có thể ship không
                var currentStatus = order.Status ?? "Pending";
                var allowedStatuses = new[] { "Paid", "Processing" };
                
                if (!allowedStatuses.Contains(currentStatus))
                {
                    throw new InvalidOperationException($"Cannot ship order with status '{currentStatus}'. Order must be Paid or Processing.");
                }

                // Validate status transition
                var validationResult = ValidateStatusTransition(currentStatus, "Shipped");
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(validationResult.ErrorMessage);
                }

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

        // Cập nhật lại CancelOrderAsync để tuân theo quy tắc
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
                
                // Validate status transition to Cancelled
                var validationResult = ValidateStatusTransition(currentStatus, "Cancelled");
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(validationResult.ErrorMessage);
                }

                order.Status = "Cancelled";

                // If order was paid, create refund record
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

        // Method mới để chuyển status theo step-by-step
        public async Task<bool> AdvanceOrderToNextStepAsync(int orderId)
        {
            var order = await _context.OrderTables.FindAsync(orderId);
            if (order == null) return false;

            var currentStatus = order.Status ?? "Pending";
            var nextStatus = GetNextLogicalStatus(currentStatus);
            
            if (string.IsNullOrEmpty(nextStatus))
            {
                return false; // Không có step tiếp theo
            }

            return await UpdateOrderStatusAsync(orderId, nextStatus);
        }

        // Helper method để lấy status tiếp theo theo logic business
        private string GetNextLogicalStatus(string currentStatus)
        {
            return currentStatus switch
            {
                "Pending" => "Paid",
                "Paid" => "Processing",
                "Processing" => "Shipped",
                "Shipped" => "Delivered",
                _ => string.Empty // Không có step tiếp theo
            };
        }

        // Method để kiểm tra order có thể thực hiện action gì
        public async Task<Dictionary<string, bool>> GetAvailableActionsAsync(int orderId)
        {
            var order = await _context.OrderTables.FindAsync(orderId);
            if (order == null) return new Dictionary<string, bool>();

            var currentStatus = order.Status ?? "Pending";
            var allowedTransitions = GetAllowedStatusTransitions(currentStatus);

            // ✅ KHÓA TẤT CẢ ACTIONS KHI ĐÃ DELIVERED
            if (currentStatus == "Delivered")
            {
                return new Dictionary<string, bool>
                {
                    ["CanMarkAsPaid"] = false,
                    ["CanStartProcessing"] = false,
                    ["CanShip"] = false,
                    ["CanMarkAsDelivered"] = false,
                    ["CanCancel"] = false,
                    ["CanRefund"] = false,
                    ["CanReturn"] = false,
                    ["CanAdvanceToNext"] = false
                };
            }

            return new Dictionary<string, bool>
            {
                ["CanMarkAsPaid"] = currentStatus == "Pending",
                ["CanStartProcessing"] = currentStatus == "Paid",
                ["CanShip"] = currentStatus == "Paid" || currentStatus == "Processing",
                ["CanMarkAsDelivered"] = currentStatus == "Shipped",
                ["CanCancel"] = allowedTransitions.Contains("Cancelled"),
                ["CanRefund"] = allowedTransitions.Contains("Refunded"),
                ["CanReturn"] = allowedTransitions.Contains("Returned"),
                ["CanAdvanceToNext"] = !string.IsNullOrEmpty(GetNextLogicalStatus(currentStatus))
            };
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

        public async Task<bool> ProcessReturnRequestAsync(int orderId, string reason, string action)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.OrderTables
                    .Include(o => o.ReturnRequests)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return false;

                var returnRequest = order.ReturnRequests.FirstOrDefault();
                if (returnRequest != null)
                {
                    returnRequest.Status = action; // "Approved", "Rejected", "Processing"
                }

                if (action == "Approved")
                {
                    order.Status = "Returned";
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

        public async Task<bool> ResolveDisputeAsync(int orderId, string resolution, string status)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.OrderTables
                    .Include(o => o.Disputes)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return false;

                var dispute = order.Disputes.FirstOrDefault();
                if (dispute != null)
                {
                    dispute.Status = status; // "Resolved", "Closed", "Under Review"
                    dispute.Resolution = resolution;
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

        public async Task<List<OrderTable>> GetOrdersWithReturnsAsync()
        {
            return await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.ReturnRequests)
                .Where(o => o.ReturnRequests.Any())
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<OrderTable>> GetOrdersWithDisputesAsync()
        {
            return await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Disputes)
                .Where(o => o.Disputes.Any())
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        // Shipping Labels Implementation
        public async Task<List<OrderTable>> GetOrdersReadyForShippingAsync()
        {
            return await _context.OrderTables
                .Include(o => o.Buyer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .Include(o => o.ShippingInfos)
                .Where(o => o.Status == "Paid" && !o.ShippingInfos.Any(s => s.TrackingNumber != null))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<bool> GenerateShippingLabelAsync(int orderId, string carrier, decimal weight, string dimensions)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.OrderTables
                    .Include(o => o.Address)
                    .Include(o => o.ShippingInfos)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null || order.Status != "Paid") return false;

                // Generate tracking number (in real app, this would be from carrier API)
                string trackingNumber = GenerateTrackingNumber(carrier);
                
                // Create or update shipping info
                var shippingInfo = order.ShippingInfos.FirstOrDefault();
                if (shippingInfo == null)
                {
                    shippingInfo = new ShippingInfo
                    {
                        OrderId = orderId,
                        TrackingNumber = trackingNumber,
                        Carrier = carrier,
                        Status = "Label Created",
                        EstimatedArrival = DateTime.Now.AddDays(GetEstimatedDays(carrier))
                    };
                    _context.ShippingInfos.Add(shippingInfo);
                }
                else
                {
                    shippingInfo.TrackingNumber = trackingNumber;
                    shippingInfo.Carrier = carrier;
                    shippingInfo.Status = "Label Created";
                }

                // Update order status to indicate label is ready
                order.Status = "Label Created";

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

        public async Task<string> GetShippingLabelUrlAsync(int orderId)
        {
            var order = await _context.OrderTables
                .Include(o => o.ShippingInfos)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order?.ShippingInfos.Any() == true)
            {
                var trackingNumber = order.ShippingInfos.First().TrackingNumber;
                // In real app, this would generate/retrieve actual label URL from carrier API
                return $"/Orders/ShippingLabel/{orderId}?tracking={trackingNumber}";
            }

            return string.Empty;
        }

        public async Task<List<string>> GenerateBulkShippingLabelsAsync(List<int> orderIds, string carrier)
        {
            var labels = new List<string>();
            
            foreach (var orderId in orderIds)
            {
                var success = await GenerateShippingLabelAsync(orderId, carrier, 1.0m, "10x10x10");
                if (success)
                {
                    var labelUrl = await GetShippingLabelUrlAsync(orderId);
                    labels.Add(labelUrl);
                }
            }
            
            return labels;
        }

        public Task<decimal> CalculateShippingCostAsync(int orderId, string carrier, decimal weight, string dimensions)
        {
            // Basic shipping cost calculation based on weight and carrier
            decimal baseCost = 5.00m; // Base shipping cost
            decimal weightCost = weight * 0.5m; // $0.50 per unit weight
            
            // Carrier-specific multipliers
            decimal carrierMultiplier = carrier.ToLower() switch
            {
                "fedex" => 1.2m,
                "ups" => 1.1m,
                "usps" => 1.0m,
                "dhl" => 1.3m,
                _ => 1.0m
            };
            
            return Task.FromResult((baseCost + weightCost) * carrierMultiplier);
        }

        // Implementation of missing interface methods
        public async Task<List<OrderTable>> GetOrdersBySellerAsync(int sellerId)
        {
            return await GetOrdersBySellerIdAsync(sellerId);
        }

        public async Task<OrderTable> GetOrderDetailsByIdAsync(int orderId)
        {
            var order = await GetOrderDetailsAsync(orderId);
            return order ?? throw new InvalidOperationException($"Order with ID {orderId} not found");
        }

        public async Task<List<Review>> GetOrderProductReviewsByBuyerAsync(int orderId)
        {
            // Since Review doesn't have OrderId, we need to get reviews through the order's products
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

        // Helper methods
        private string GenerateTrackingNumber(string carrier)
        {
            var random = new Random();
            return carrier.ToUpper() switch
            {
                "USPS" => $"9405{random.Next(100000000, 999999999)}",
                "FEDEX" => $"FDX{random.Next(100000000, 999999999)}",
                "UPS" => $"1Z{random.Next(100000000, 999999999)}",
                "DHL" => $"DHL{random.Next(100000000, 999999999)}",
                _ => $"TRK{random.Next(100000000, 999999999)}"
            };
        }

        private int GetEstimatedDays(string carrier)
        {
            return carrier.ToLower() switch
            {
                "usps" => 3,
                "fedex" => 2,
                "ups" => 3,
                "dhl" => 1,
                _ => 5
            };
        }
    }
}