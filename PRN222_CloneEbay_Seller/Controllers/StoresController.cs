using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class StoresController : Controller
    {
        private readonly IStoreService _storeService;

        public StoresController(IStoreService storeService)
        {
            _storeService = storeService;
        }

        // GET: Store - Display all stores for current seller
        public async Task<IActionResult> Index()
        {
            // For demo purposes, using seller ID = 1. In real app, get from authentication
            int currentSellerId = 1;
            
            var stores = await _storeService.GetStoresBySellerIdAsync(currentSellerId);
            ViewBag.CurrentSellerId = currentSellerId;
            
            return View(stores);
        }

        // GET: Store/Details/5
        public async Task<IActionResult> Details(int id)
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

        // GET: Store/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Store/Create
        [HttpPost]
        public async Task<IActionResult> Create(Store store)
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
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to create store: {ex.Message}";
                return View(store);
            }
        }

        // GET: Store/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            int currentSellerId = 1; // Get from authentication in real app
            
            var store = await _storeService.GetStoreByIdAsync(id);
            if (store == null || store.SellerId != currentSellerId)
            {
                return NotFound();
            }

            return View(store);
        }

        // POST: Store/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Store store, IFormFile? bannerFile)
        {
            int currentSellerId = 1; // Get from authentication in real app
            
            if (!ModelState.IsValid)
            {
                return View(store);
            }

            // Verify ownership
            if (!await _storeService.IsStoreOwnerAsync(id, currentSellerId))
            {
                return NotFound();
            }

            // Check if store name is unique (excluding current store)
            if (!await _storeService.IsStoreNameUniqueAsync(store.StoreName!, id))
            {
                ModelState.AddModelError("StoreName", "Store name already exists. Please choose a different name.");
                return View(store);
            }

            // Handle banner upload if provided
            if (bannerFile != null && bannerFile.Length > 0)
            {
                try
                {
                    var bannerUrl = await SaveBannerImageAsync(bannerFile, currentSellerId);
                    store.BannerImageUrl = bannerUrl;
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    return View(store);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Failed to upload banner: {ex.Message}");
                    return View(store);
                }
            }

            try
            {
                var result = await _storeService.UpdateStoreAsync(id, store);
                if (result)
                {
                    TempData["Success"] = "Store updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Store not found.";
                    return View(store);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to update store: {ex.Message}";
                return View(store);
            }
        }

        // Helper method to save banner image
        private async Task<string> SaveBannerImageAsync(IFormFile bannerFile, int sellerId)
        {
            // Validate file
            if (bannerFile == null || bannerFile.Length == 0)
                throw new ArgumentException("No file provided");

            // Check file size (max 2MB)
            if (bannerFile.Length > 2 * 1024 * 1024)
                throw new ArgumentException("File size must be less than 2MB");

            // Check file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(bannerFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Only image files (JPG, PNG, GIF, WebP) are allowed");

            // Create directory structure: wwwroot/images/store/{sellerId}/banner/
            var uploadsFolder = Path.Combine("wwwroot", "images", "store", sellerId.ToString(), "banner");
            var fullUploadsPath = Path.Combine(Directory.GetCurrentDirectory(), uploadsFolder);
            
            if (!Directory.Exists(fullUploadsPath))
            {
                Directory.CreateDirectory(fullUploadsPath);
            }

            // Generate unique filename
            var fileName = $"banner_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
            var filePath = Path.Combine(fullUploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await bannerFile.CopyToAsync(stream);
            }

            // Return relative URL for web access
            return $"/images/store/{sellerId}/banner/{fileName}";
        }

        // POST: Store/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            int currentSellerId = 1; // Get from authentication in real app
            
            // Verify ownership
            if (!await _storeService.IsStoreOwnerAsync(id, currentSellerId))
            {
                return Json(new { success = false, message = "Store not found or access denied." });
            }

            try
            {
                var result = await _storeService.DeleteStoreAsync(id);
                if (result)
                {
                    return Json(new { success = true, message = "Store deleted successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Store not found." });
                }
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to delete store: {ex.Message}" });
            }
        }

        // GET: Store/Settings/5
        public async Task<IActionResult> Settings(int id)
        {
            int currentSellerId = 1; // Get from authentication in real app
            
            var store = await _storeService.GetStoreByIdAsync(id);
            if (store == null || store.SellerId != currentSellerId)
            {
                return NotFound();
            }

            return View(store);
        }

        // POST: Store/Settings/5
        [HttpPost]
        public async Task<IActionResult> Settings(int id, Store store)
        {
            int currentSellerId = 1; // Get from authentication in real app
            
            if (!ModelState.IsValid)
            {
                return View(store);
            }

            // Verify ownership
            if (!await _storeService.IsStoreOwnerAsync(id, currentSellerId))
            {
                return NotFound();
            }

            try
            {
                var result = await _storeService.UpdateStoreAsync(id, store);
                if (result)
                {
                    TempData["Success"] = "Store settings updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    TempData["Error"] = "Store not found.";
                    return View(store);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to update store settings: {ex.Message}";
                return View(store);
            }
        }

        // GET: Store/GetStatistics/5 - API endpoint for store statistics
        [HttpGet]
        public async Task<IActionResult> GetStatistics(int id)
        {
            try
            {
                int currentSellerId = 1; // Get from authentication in real app
                
                // Verify ownership
                if (!await _storeService.IsStoreOwnerAsync(id, currentSellerId))
                {
                    return Json(new { error = "Access denied" });
                }

                var statistics = await _storeService.GetStoreStatisticsAsync(id);
                return Json(statistics);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}