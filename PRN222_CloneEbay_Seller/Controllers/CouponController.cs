using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class CouponController : Controller
    {
        private readonly CloneEbayDbContext _context;

        public CouponController(CloneEbayDbContext context)
        {
            _context = context;
        }

        // GET: /Coupon (Danh sách coupons)
        public async Task<IActionResult> Index()
        {
            var sellerId = 1; // Giả định ID người bán để test
            var coupons = await _context.Coupons
                .Include(c => c.Product) // Include Product để hiển thị tên
                .Where(c => c.Product.SellerId == sellerId)
                .ToListAsync();
            return View(coupons);
        }

        // GET: /Coupon/Create
        public async Task<IActionResult> Create()
        {
            var sellerId = 1; // Giả định ID
            ViewBag.Products = new SelectList(
                await _context.Products.Where(p => p.SellerId == sellerId).ToListAsync(),
                "Id", "Title");
            return View();
        }

        // POST: /Coupon/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,DiscountPercent,StartDate,EndDate,MaxUsage,ProductId")] Coupon coupon)
        {
            if (ModelState.IsValid)
            {
                _context.Add(coupon);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            var sellerId = 1; // Giả định ID
            ViewBag.Products = new SelectList(
                await _context.Products.Where(p => p.SellerId == sellerId).ToListAsync(),
                "Id", "Title", coupon.ProductId);
            return View(coupon);
        }

        // ... Các action Edit, Delete tương tự ...
    }
}