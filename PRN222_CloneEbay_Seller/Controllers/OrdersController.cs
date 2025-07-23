using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IAccountService _accountService;

        public OrdersController(IOrderService orderService, IAccountService accountService)
        {
            _orderService = orderService;
            _accountService = accountService;
        }

        private async Task<bool> CheckSellerAccessAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return false;
            
            return await _accountService.CanAccessSellerFeaturesAsync(userId.Value);
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string status = "all")
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to access orders.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var orders = status == "all"
                    ? await _orderService.GetAllOrdersAsync(userId)
                    : await _orderService.GetOrdersByStatusAsync(status, userId);

                var statistics = await _orderService.GetOrderStatisticsAsync(userId);
                var totalRevenue = await _orderService.GetTotalRevenueAsync(userId);

                ViewBag.CurrentStatus = status;
                ViewBag.Statistics = statistics;
                ViewBag.TotalRevenue = totalRevenue;

                return View(orders);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading orders.";
                return View(new List<PRN222_CloneEbay_Seller.Models.OrderTable>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to access order details.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var order = await _orderService.GetOrderDetailsAsync(id, userId);
                if (order == null)
                {
                    TempData["Error"] = "Order not found or you don't have permission to view it.";
                    return RedirectToAction("Index");
                }

                var buyerReviews = await _orderService.GetOrderProductReviewsByBuyerAsync(id, userId);
                ViewBag.BuyerReviews = buyerReviews;

                return View(order);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading order details.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Ship(int orderId, string trackingNumber, string carrier)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please log in to continue." });
            }

            if (!await CheckSellerAccessAsync())
            {
                return Json(new { success = false, message = "You need to be an approved seller to ship orders." });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(trackingNumber) || string.IsNullOrWhiteSpace(carrier))
                {
                    return Json(new { success = false, message = "Tracking number and carrier are required." });
                }

                var success = await _orderService.ShipOrderAsync(orderId, trackingNumber, carrier, userId);

                if (success)
                {
                    return Json(new { success = true, message = "Order shipped successfully!" });
                }

                return Json(new { success = false, message = "Failed to ship order or order not found." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch
            {
                return Json(new { success = false, message = "An error occurred while shipping the order." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int orderId, string reason)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please log in to continue." });
            }

            if (!await CheckSellerAccessAsync())
            {
                return Json(new { success = false, message = "You need to be an approved seller to cancel orders." });
            }

            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return Json(new { success = false, message = "Cancellation reason is required." });
                }

                var success = await _orderService.CancelOrderAsync(orderId, reason, userId);

                if (success)
                {
                    return Json(new { success = true, message = "Order cancelled successfully!" });
                }

                return Json(new { success = false, message = "Failed to cancel order or order not found." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch
            {
                return Json(new { success = false, message = "An error occurred while cancelling the order." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please log in to continue." });
            }

            if (!await CheckSellerAccessAsync())
            {
                return Json(new { success = false, message = "You need to be an approved seller to update order status." });
            }

            try
            {
                var success = await _orderService.UpdateOrderStatusAsync(orderId, status, userId);

                if (success)
                {
                    return Json(new { success = true, message = "Order status updated successfully!" });
                }

                return Json(new { success = false, message = "Failed to update order status or order not found." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch
            {
                return Json(new { success = false, message = "An error occurred while updating order status." });
            }
        }

        // POST: Orders/ProcessRefund
        [HttpPost]
        public async Task<IActionResult> ProcessRefund(int orderId, decimal? amount = null, string? reason = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please log in to continue." });
            }

            if (!await CheckSellerAccessAsync())
            {
                return Json(new { success = false, message = "You need to be an approved seller to process refunds." });
            }

            try
            {
                // Get order details to determine refund amount if not provided
                var order = await _orderService.GetOrderDetailsAsync(orderId, userId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found or you don't have permission to access it." });
                }

                var refundAmount = amount ?? order.TotalPrice ?? 0;
                var refundReason = reason ?? "Seller initiated refund";

                var success = await _orderService.ProcessRefundAsync(orderId, refundAmount, refundReason, userId);

                if (success)
                {
                    return Json(new { success = true, message = $"Refund of ${refundAmount:F2} processed successfully!" });
                }

                return Json(new { success = false, message = "Failed to process refund." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch
            {
                return Json(new { success = false, message = "An error occurred while processing the refund." });
            }
        }
    }
}