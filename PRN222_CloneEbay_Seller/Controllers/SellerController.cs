using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class SellerController : Controller
    {
        private readonly CloneEbayDbContext _context;

        public SellerController(CloneEbayDbContext context)
        {
            _context = context;
        }

        public IActionResult Overview()
        {
            return View();
        }

        public IActionResult Performance()
        {
            return View();
        }
        
        public IActionResult Store()
        {
            return View();
        }
    }
}
