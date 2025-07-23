using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IDisputeService _disputeService;

        public OrdersController(IOrderService orderService, IDisputeService disputeService)
        {
            _orderService = orderService;
            _disputeService = disputeService;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string status = "all")
        {
            var orders = status == "all"
                ? await _orderService.GetAllOrdersAsync()
                : await _orderService.GetOrdersByStatusAsync(status);

            var statistics = await _orderService.GetOrderStatisticsAsync();
            var totalRevenue = await _orderService.GetTotalRevenueAsync();

            ViewBag.CurrentStatus = status;
            ViewBag.Statistics = statistics;
            ViewBag.TotalRevenue = totalRevenue;

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var buyerReviews = await _orderService.GetOrderProductReviewsByBuyerAsync(id);
            ViewBag.BuyerReviews = buyerReviews;

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Ship(int orderId, string trackingNumber, string carrier)
        {
            if (string.IsNullOrEmpty(trackingNumber) || string.IsNullOrEmpty(carrier))
            {
                TempData["Error"] = "Tracking number and carrier are required.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            try
            {
                var result = await _orderService.ShipOrderAsync(orderId, trackingNumber, carrier);

                if (result)
                {
                    TempData["Success"] = "Order has been marked as shipped successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to ship order. Please try again.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred while shipping the order.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int orderId, string reason)
        {
            try
            {
                var result = await _orderService.CancelOrderAsync(orderId, reason ?? "Cancelled by seller");

                if (result)
                {
                    TempData["Success"] = "Order has been cancelled successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to cancel order. Order may not be eligible for cancellation.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred while cancelling the order.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            try
            {
                var result = await _orderService.UpdateOrderStatusAsync(orderId, status);

                if (result)
                {
                    TempData["Success"] = $"Order status updated to '{status}' successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to update order status. Order not found.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred while updating order status.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Orders/ProcessRefund
        [HttpPost]
        public async Task<IActionResult> ProcessRefund(int orderId, decimal? amount = null, string reason = null)
        {
            try
            {
                var order = await _orderService.GetOrderDetailsAsync(orderId);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }

                // If no amount specified, refund full amount
                decimal refundAmount = amount ?? order.TotalPrice ?? 0;

                var result = await _orderService.ProcessRefundAsync(orderId, refundAmount, reason ?? "Refund processed by seller");

                if (result)
                {
                    TempData["Success"] = $"Refund of ${refundAmount:N2} has been processed successfully.";
                }
                else
                {
                    TempData["Error"] = "Failed to process refund. Please try again.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred while processing refund.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // GET: /Orders/Disputes
        public async Task<IActionResult> Disputes()
        {
            var sellerId = 1; // Giả định
            var disputes = await _disputeService.GetDisputesForSellerAsync(sellerId);
            return View(disputes);
        }

        // GET: /Orders/DisputeDetails/5
        public async Task<IActionResult> DisputeDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sellerId = 1; // Giả định
            var dispute = await _disputeService.GetDisputeDetailsAsync(id.Value, sellerId);

            if (dispute == null)
            {
                return NotFound();
            }

            return View(dispute);
        }

        // POST: /Orders/ResolveDispute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveDispute(int disputeId, string resolution)
        {
            if (string.IsNullOrWhiteSpace(resolution))
            {
                TempData["Error"] = "Resolution description cannot be empty.";
                return RedirectToAction("DisputeDetails", new { id = disputeId });
            }

            var sellerId = 1; // Giả định
            var success = await _disputeService.ResolveDisputeAsync(disputeId, resolution, sellerId);

            if (success)
            {
                TempData["Success"] = "Dispute has been successfully resolved.";
            }
            else
            {
                TempData["Error"] = "Failed to resolve dispute. It may not exist or you don't have permission.";
            }

            return RedirectToAction("DisputeDetails", new { id = disputeId });
        }
    }
}