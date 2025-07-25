using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class SellerController : Controller
    {
        private readonly CloneEbayDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IStoreService _storeService;

        public SellerController(CloneEbayDbContext context, IOrderService orderService, IStoreService storeService)
        {
            _context = context;
            _orderService = orderService;
            _storeService = storeService;
        }

        public IActionResult Overview()
        {
            return View();
        }

        public IActionResult Performance()
        {
            return View();
        }

        // GET: Seller/Store - Display all stores for current seller
        public async Task<IActionResult> Store()
        {
            int currentSellerId = 1;

            var stores = await _storeService.GetStoresBySellerIdAsync(currentSellerId);
            ViewBag.CurrentSellerId = currentSellerId;

            return View(stores);
        }

        // GET: Seller/Store/Details/5
        public async Task<IActionResult> StoreDetails(int id)
        {
            int currentSellerId = 1; // Get from authentication in real app

            var store = await _storeService.GetStoreByIdAsync(id);
            if (store == null || store.SellerId != currentSellerId)
            {
                return NotFound();
            }

            var statistics = await _storeService.GetStoreStatisticsAsync(id);
            var products = await _storeService.GetStoreProductsAsync(id);

            ViewBag.Statistics = statistics;
            ViewBag.Products = products;

            return View(store);
        }

        // GET: Seller/Store/Create
        public IActionResult CreateStore()
        {
            return View();
        }

        // POST: Seller/Store/Create
        [HttpPost]
        public async Task<IActionResult> CreateStore(Store store)
        {
            int currentSellerId = 1; // Get from authentication in real app

            if (!ModelState.IsValid)
            {
                return View(store);
            }

            // Check if store name is unique
            if (!await _storeService.IsStoreNameUniqueAsync(store.StoreName!))
            {
                ModelState.AddModelError("StoreName", "Store name already exists. Please choose a different name.");
                return View(store);
            }

            store.SellerId = currentSellerId;

            try
            {
                await _storeService.CreateStoreAsync(store);
                TempData["Success"] = "Store created successfully!";
                return RedirectToAction(nameof(Store));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to create store: {ex.Message}";
                return View(store);
            }
        }
    
        // GET: Seller/Store/Edit/5
        public async Task<IActionResult> EditStore(int id)
        {
            int currentSellerId = 1; // Get from authentication in real app
            var store = await _storeService.GetStoreByIdAsync(id);
            if (store == null || store.SellerId != currentSellerId)
            {
                return NotFound();
            }
            return View(store);
        }
        // POST: Seller/Store/Edit/5
        [HttpPost]
        public async Task<IActionResult> EditStore(int id, Store store)
        {
            int currentSellerId = 1; // Get from authentication in real app
            if (!ModelState.IsValid)
            {
                return View(store);
            }
            // Check if store name is unique
            if (!await _storeService.IsStoreNameUniqueAsync(store.StoreName!, id))
            {
                ModelState.AddModelError("StoreName", "Store name already exists. Please choose a different name.");
                return View(store);
            }
            try
            {
                if (await _storeService.UpdateStoreAsync(id, store))
                {
                    TempData["Success"] = "Store updated successfully!";
                    return RedirectToAction(nameof(StoreDetails), new { id });
                }
                else
                {
                    TempData["Error"] = "Failed to update store.";
                    return View(store);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to update store: {ex.Message}";
                return View(store);
            }

        }

    }
    }