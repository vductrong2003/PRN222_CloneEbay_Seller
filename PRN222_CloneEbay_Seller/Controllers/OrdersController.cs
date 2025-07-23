using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
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

        // POST: Orders/Cancel
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

        // POST: Orders/UpdateStatus - Updated with validation logic
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

        // POST: Orders/AdvanceToNextStep - New method for step-by-step progression
        [HttpPost]
        public async Task<IActionResult> AdvanceToNextStep(int orderId)
        {
            try
            {
                var result = await _orderService.AdvanceOrderToNextStepAsync(orderId);

                if (result)
                {
                    TempData["Success"] = "Order advanced to next step successfully.";
                }
                else
                {
                    TempData["Error"] = "Cannot advance order. Order may already be at final status.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to advance order: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // GET: Orders/GetAvailableActions/{id} - API endpoint for available actions
        [HttpGet]
        public async Task<IActionResult> GetAvailableActions(int id)
        {
            try
            {
                var availableActions = await _orderService.GetAvailableActionsAsync(id);
                return Json(availableActions);
            }
            catch (Exception)
            {
                return Json(new Dictionary<string, bool>());
            }
        }

        [HttpGet]
        public IActionResult ValidateStatusChange(string currentStatus, string newStatus)
        {
            try
            {
                var validation = _orderService.ValidateStatusTransition(currentStatus, newStatus);
                return Json(new
                {
                    isValid = validation.IsValid,
                    errorMessage = validation.ErrorMessage,
                    allowedTransitions = _orderService.GetAllowedStatusTransitions(currentStatus)
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    isValid = false,
                    errorMessage = ex.Message,
                    allowedTransitions = new List<string>()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Recent(int count = 10)
        {
            var orders = await _orderService.GetRecentOrdersAsync(count);
            return Json(orders);
        }

        // GET: Orders/Statistics - API endpoint for statistics
        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            var statistics = await _orderService.GetOrderStatisticsAsync();
            var totalRevenue = await _orderService.GetTotalRevenueAsync();

            return Json(new
            {
                Statistics = statistics,
                TotalRevenue = totalRevenue
            });
        }

        // GET: Orders/Search - Search orders by various criteria
        public async Task<IActionResult> Search(string query, string status = "all", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var orders = status == "all"
                ? await _orderService.GetAllOrdersAsync()
                : await _orderService.GetOrdersByStatusAsync(status);

            // Filter by date range
            if (fromDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= fromDate.Value).ToList();
            }
            if (toDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= toDate.Value).ToList();
            }

            // Filter by search query (order ID, buyer name, product name)
            if (!string.IsNullOrEmpty(query))
            {
                orders = orders.Where(o =>
                    o.Id.ToString().Contains(query) ||
                    (o.Buyer?.Username?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (o.Buyer?.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    o.OrderItems.Any(oi => oi.Product?.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            ViewBag.CurrentStatus = status;
            ViewBag.Query = query;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;

            return View("Index", orders);
        }

        // GET: Orders/Returns
        public async Task<IActionResult> Returns()
        {
            var orders = await _orderService.GetOrdersWithReturnsAsync();
            ViewBag.CurrentStatus = "returns";
            ViewBag.Statistics = await _orderService.GetOrderStatisticsAsync();
            ViewBag.TotalRevenue = await _orderService.GetTotalRevenueAsync();
            return View("Index", orders);
        }

        // GET: Orders/Disputes
        public async Task<IActionResult> Disputes()
        {
            var orders = await _orderService.GetOrdersWithDisputesAsync();
            ViewBag.CurrentStatus = "disputes";
            ViewBag.Statistics = await _orderService.GetOrderStatisticsAsync();
            ViewBag.TotalRevenue = await _orderService.GetTotalRevenueAsync();
            return View("Index", orders);
        }

        // GET: Orders/ShippingLabels
        public async Task<IActionResult> ShippingLabels()
        {
            var orders = await _orderService.GetOrdersReadyForShippingAsync();
            ViewBag.CurrentStatus = "shipping-labels";
            ViewBag.Statistics = await _orderService.GetOrderStatisticsAsync();
            ViewBag.TotalRevenue = await _orderService.GetTotalRevenueAsync();
            return View("Index", orders);
        }

        // POST: Orders/GenerateLabel
        [HttpPost]
        public async Task<IActionResult> GenerateLabel(int orderId, string carrier, decimal weight, string dimensions)
        {
            if (string.IsNullOrEmpty(carrier))
            {
                TempData["Error"] = "Please select a carrier.";
                return RedirectToAction("Details", new { id = orderId });
            }

            var success = await _orderService.GenerateShippingLabelAsync(orderId, carrier, weight, dimensions);

            if (success)
            {
                TempData["Success"] = "Shipping label generated successfully.";
                var labelUrl = await _orderService.GetShippingLabelUrlAsync(orderId);
                TempData["LabelUrl"] = labelUrl;
            }
            else
            {
                TempData["Error"] = "Failed to generate shipping label. Please try again.";
            }

            return RedirectToAction("Details", new { id = orderId });
        }

        // POST: Orders/GenerateBulkLabels
        [HttpPost]
        public async Task<IActionResult> GenerateBulkLabels([FromBody] BulkLabelRequest request)
        {
            if (request.OrderIds == null || !request.OrderIds.Any())
            {
                return Json(new { success = false, message = "No orders selected." });
            }

            if (string.IsNullOrEmpty(request.Carrier))
            {
                return Json(new { success = false, message = "Please select a carrier." });
            }

            var labelUrls = await _orderService.GenerateBulkShippingLabelsAsync(request.OrderIds, request.Carrier);

            return Json(new
            {
                success = true,
                message = $"Generated {labelUrls.Count} shipping labels successfully.",
                labelUrls = labelUrls
            });
        }

        // GET: Orders/ShippingLabel/{id}
        public async Task<IActionResult> ShippingLabel(int id, string tracking)
        {
            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            ViewBag.TrackingNumber = tracking;
            return View(order);
        }

        // GET: Orders/CalculateShipping
        [HttpGet]
        public async Task<IActionResult> CalculateShipping(int orderId, string carrier, decimal weight, string dimensions)
        {
            var cost = await _orderService.CalculateShippingCostAsync(orderId, carrier, weight, dimensions);
            return Json(new { cost = cost });
        }

        // API endpoints for statistics
        [HttpGet]
        public async Task<IActionResult> GetStatistics()
        {
            var statistics = await _orderService.GetOrderStatisticsAsync();
            var totalRevenue = await _orderService.GetTotalRevenueAsync();

            return Json(new { statistics, totalRevenue });
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentOrders(int count = 5)
        {
            var orders = await _orderService.GetRecentOrdersAsync(count);
            return Json(orders);
        }
    }

    // Helper class for bulk label generation
    public class BulkLabelRequest
    {
        public List<int> OrderIds { get; set; } = new List<int>();
        public string Carrier { get; set; } = string.Empty;
    }
}