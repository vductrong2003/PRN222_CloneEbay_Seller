using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class StoresController : Controller
    {
        private readonly IStoreService _storeService;
        private readonly IAccountService _accountService;

        public StoresController(IStoreService storeService, IAccountService accountService)
        {
            _storeService = storeService;
            _accountService = accountService;
        }

        private async Task<bool> CheckSellerAccessAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return false;
            
            return await _accountService.CanAccessSellerFeaturesAsync(userId.Value);
        }

        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        // GET: Store - Display all stores for current seller
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to access stores.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var stores = await _storeService.GetStoresBySellerIdAsync(userId);
                ViewBag.CurrentSellerId = userId;
                
                return View(stores);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading your stores.";
                return View(new List<Store>());
            }
        }

        // GET: Store/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to access store details.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var store = await _storeService.GetStoreByIdAsync(id);
                if (store == null || store.SellerId != userId)
                {
                    TempData["Error"] = "Store not found or you don't have permission to view it.";
                    return RedirectToAction("Index");
                }

                var statistics = await _storeService.GetStoreStatisticsAsync(id);
                var products = await _storeService.GetStoreProductsAsync(id);
                
                ViewBag.Statistics = statistics;
                ViewBag.Products = products;
                
                return View(store);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading store details.";
                return RedirectToAction("Index");
            }
        }

        // GET: Store/Create
        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to create stores.";
                return RedirectToAction("RequestSeller", "Account");
            }

            return View();
        }

        // POST: Store/Create
        [HttpPost]
        public async Task<IActionResult> Create(Store store)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to create stores.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    return View(store);
                }

                // Check if store name is unique
                var isUnique = await _storeService.IsStoreNameUniqueAsync(store.StoreName);
                if (!isUnique)
                {
                    ModelState.AddModelError("StoreName", "This store name is already taken");
                    return View(store);
                }

                store.SellerId = userId; // Set the current user as seller
                var createdStore = await _storeService.CreateStoreAsync(store);

                if (createdStore != null)
                {
                    TempData["Success"] = "Store created successfully!";
                    return RedirectToAction("Details", new { id = createdStore.Id });
                }

                TempData["Error"] = "Failed to create store. Please try again.";
                return View(store);
            }
            catch
            {
                TempData["Error"] = "An error occurred while creating the store.";
                return View(store);
            }
        }

        // GET: Store/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to edit stores.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var store = await _storeService.GetStoreByIdAsync(id);
                if (store == null || store.SellerId != userId)
                {
                    TempData["Error"] = "Store not found or you don't have permission to edit it.";
                    return RedirectToAction("Index");
                }

                return View(store);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading the store for editing.";
                return RedirectToAction("Index");
            }
        }

        // POST: Store/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, Store store, IFormFile? bannerFile)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to edit stores.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var existingStore = await _storeService.GetStoreByIdAsync(id);
                if (existingStore == null || existingStore.SellerId != userId)
                {
                    TempData["Error"] = "Store not found or you don't have permission to edit it.";
                    return RedirectToAction("Index");
                }

                if (!ModelState.IsValid)
                {
                    return View(store);
                }

                // Check if store name is unique (excluding current store)
                var isUnique = await _storeService.IsStoreNameUniqueAsync(store.StoreName, id);
                if (!isUnique)
                {
                    ModelState.AddModelError("StoreName", "This store name is already taken");
                    return View(store);
                }

                // Handle banner file upload if provided
                if (bannerFile != null && bannerFile.Length > 0)
                {
                    // Validate file type
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(bannerFile.ContentType.ToLower()))
                    {
                        ModelState.AddModelError("BannerFile", "Only image files (JPEG, PNG, GIF) are allowed");
                        return View(store);
                    }

                    // Validate file size (max 5MB)
                    if (bannerFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("BannerFile", "File size cannot exceed 5MB");
                        return View(store);
                    }

                    // Save the uploaded file
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "banners");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    var fileName = $"{Guid.NewGuid()}_{bannerFile.FileName}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await bannerFile.CopyToAsync(stream);
                    }

                    store.BannerImageUrl = $"/uploads/banners/{fileName}";
                }
                else
                {
                    // Keep existing banner if no new file uploaded
                    store.BannerImageUrl = existingStore.BannerImageUrl;
                }

                store.SellerId = userId; // Ensure seller ID is correct
                var success = await _storeService.UpdateStoreAsync(id, store);

                if (success)
                {
                    TempData["Success"] = "Store updated successfully!";
                    return RedirectToAction("Details", new { id = id });
                }

                TempData["Error"] = "Failed to update store. Please try again.";
                return View(store);
            }
            catch
            {
                TempData["Error"] = "An error occurred while updating the store.";
                return View(store);
            }
        }

        // POST: Store/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please log in to continue." });
            }

            if (!await CheckSellerAccessAsync())
            {
                return Json(new { success = false, message = "You need to be an approved seller to delete stores." });
            }

            try
            {
                var store = await _storeService.GetStoreByIdAsync(id);
                if (store == null || store.SellerId != userId)
                {
                    return Json(new { success = false, message = "Store not found or you don't have permission to delete it." });
                }

                var success = await _storeService.DeleteStoreAsync(id);

                if (success)
                {
                    return Json(new { success = true, message = "Store deleted successfully!" });
                }

                return Json(new { success = false, message = "Failed to delete store." });
            }
            catch
            {
                return Json(new { success = false, message = "An error occurred while deleting the store." });
            }
        }
    }
}