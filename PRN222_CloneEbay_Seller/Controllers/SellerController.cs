using Microsoft.AspNetCore.Mvc;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class SellerController : Controller
    {
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
