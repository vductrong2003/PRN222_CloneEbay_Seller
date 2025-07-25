using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;
using System.Collections.Generic; // Thêm using
using System.Linq; // Thêm using
using System.Threading.Tasks;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly CloneEbayDbContext _context;

        public PaymentController(IPaymentService paymentService, CloneEbayDbContext context)
        {
            _paymentService = paymentService;
            _context = context;
        }
        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        // Action cho trang danh sách Lịch Sử Giao Dịch
        public async Task<IActionResult> Index()
        {
            var sellerId = GetCurrentUserId(); // Giả định ID người bán để test
            var transactions = await _paymentService.GetPaymentTransactionsForSellerAsync(sellerId);

            // --- Nâng cấp: Tính toán các số liệu thống kê ---
            decimal totalSales = transactions.Where(t => t.Status?.ToLower() == "completed").Sum(t => t.Amount) ?? 0;
            decimal totalRefunds = transactions.Where(t => t.Status?.ToLower() == "refunded").Sum(t => t.Amount) ?? 0;
            decimal netIncome = totalSales - totalRefunds;

            ViewBag.TotalSales = totalSales;
            ViewBag.TotalRefunds = totalRefunds;
            ViewBag.NetIncome = netIncome;
            ViewBag.TotalTransactions = transactions.Count;
            // ----------------------------------------------------

            return View(transactions);
        }

        // Action để xem chi tiết một giao dịch
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o.Buyer) // Lấy thông tin người mua
                .Include(p => p.Order)
                    .ThenInclude(o => o.Address) // Lấy thông tin địa chỉ
                .FirstOrDefaultAsync(p => p.Id == id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
    }
}