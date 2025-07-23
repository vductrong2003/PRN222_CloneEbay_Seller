using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class ProductController : Controller
    {
        private readonly CloneEbayDbContext _context;

        public ProductController(CloneEbayDbContext context)
        {
            _context = context;
        }

        // GET: Product (Hiển thị danh sách sản phẩm)
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách sản phẩm và bao gồm thông tin về Category và Seller
            var products = _context.Products
                                   .Include(p => p.Category)
                                   .Include(p => p.Seller);
            return View(await products.ToListAsync());
        }

        // GET: Product/Details/5 (Hiển thị chi tiết sản phẩm)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Seller)
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Product/Create (Hiển thị form tạo mới)
        public IActionResult Create()
        {
            // Gửi danh sách Category và User (Seller) để hiển thị trong dropdown list
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            ViewData["SellerId"] = new SelectList(_context.Users.Where(u => u.Role == "Seller"), "Id", "Username");
            return View();
        }

        // POST: Product/Create (Xử lý khi submit form tạo mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Price,Images,CategoryId,SellerId,IsAuction,AuctionEndTime")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["SellerId"] = new SelectList(_context.Users.Where(u => u.Role == "Seller"), "Id", "Username", product.SellerId);
            return View(product);
        }

        // GET: Product/Edit/5 (Hiển thị form chỉnh sửa)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["SellerId"] = new SelectList(_context.Users.Where(u => u.Role == "Seller"), "Id", "Username", product.SellerId);
            return View(product);
        }

        // POST: Product/Edit/5 (Xử lý khi submit form chỉnh sửa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Price,Images,CategoryId,SellerId,IsAuction,AuctionEndTime")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewData["SellerId"] = new SelectList(_context.Users.Where(u => u.Role == "Seller"), "Id", "Username", product.SellerId);
            return View(product);
        }

        // GET: Product/Delete/5 (Hiển thị trang xác nhận xóa)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                                        .Include(p => p.Category)
                                        .Include(p => p.Seller)
                                        .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Product/Delete/5 (Xử lý xóa)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}