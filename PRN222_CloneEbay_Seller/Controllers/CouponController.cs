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

        // GET: /Coupon/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sellerId = 1; // Giả định ID người bán để test
            var coupon = await _context.Coupons
                .Include(c => c.Product) // Include Product để kiểm tra quyền sở hữu
                .FirstOrDefaultAsync(c => c.Id == id);

            if (coupon == null || coupon.Product.SellerId != sellerId)
            {
                // Không tìm thấy coupon hoặc coupon không thuộc về người bán này
                return NotFound();
            }

            // Chuẩn bị dropdown list sản phẩm, và chọn sẵn sản phẩm hiện tại của coupon
            ViewBag.Products = new SelectList(
                await _context.Products.Where(p => p.SellerId == sellerId).ToListAsync(),
                "Id", "Title", coupon.ProductId);

            return View(coupon);
        }

        // POST: /Coupon/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,DiscountPercent,StartDate,EndDate,MaxUsage,ProductId")] Coupon coupon)
        {
            if (id != coupon.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Security check: Đảm bảo người dùng không sửa productId thành của người khác
                    var product = await _context.Products.FindAsync(coupon.ProductId);
                    var sellerId = 1; // Giả định ID
                    if (product == null || product.SellerId != sellerId)
                    {
                        ModelState.AddModelError("ProductId", "Invalid product selected.");
                        // Nếu lỗi, phải load lại dropdown
                        ViewBag.Products = new SelectList(
                           await _context.Products.Where(p => p.SellerId == sellerId).ToListAsync(),
                           "Id", "Title", coupon.ProductId);
                        return View(coupon);
                    }

                    _context.Update(coupon);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Coupons.Any(e => e.Id == coupon.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Nếu model không hợp lệ, load lại dropdown và hiển thị lại form
            var currentSellerId = 1; // Giả định ID
            ViewBag.Products = new SelectList(
                await _context.Products.Where(p => p.SellerId == currentSellerId).ToListAsync(),
                "Id", "Title", coupon.ProductId);
            return View(coupon);
        }

        // ======================== KẾT THÚC PHẦN EDIT =======================
    }
}
