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

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetOrderDetailsAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Ship
        [HttpPost]
        public async Task<IActionResult> Ship(int orderId, string trackingNumber, string carrier)
        {
            if (string.IsNullOrEmpty(trackingNumber) || string.IsNullOrEmpty(carrier))
            {
                TempData["Error"] = "Tracking number and carrier are required.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            var result = await _orderService.ShipOrderAsync(orderId, trackingNumber, carrier);
            
            if (result)
            {
                TempData["Success"] = "Order has been marked as shipped successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to ship order. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Orders/Cancel
        [HttpPost]
        public async Task<IActionResult> Cancel(int orderId, string reason)
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

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Orders/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var result = await _orderService.UpdateOrderStatusAsync(orderId, status);
            
            if (result)
            {
                TempData["Success"] = "Order status updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update order status.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Orders/ProcessRefund
        [HttpPost]
        public async Task<IActionResult> ProcessRefund(int orderId, decimal amount, string reason)
        {
            if (amount <= 0)
            {
                TempData["Error"] = "Refund amount must be greater than zero.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            var result = await _orderService.ProcessRefundAsync(orderId, amount, reason ?? "Refund processed by seller");
            
            if (result)
            {
                TempData["Success"] = $"Refund of ${amount:F2} has been processed successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to process refund. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // GET: Orders/Recent - API endpoint for recent orders
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
            
            return Json(new { 
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
    }
}