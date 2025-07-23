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

        // GET: Stores - Display or create seller's store
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to access your store.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                // Get seller's store - each seller has only one store
                var store = await _storeService.GetStoreBySellerIdAsync(userId);
                
                if (store != null)
                {
                    // Store exists - show store details with statistics
                    var statistics = await _storeService.GetStoreStatisticsAsync(store.Id);
                    var products = await _storeService.GetStoreProductsAsync(store.Id);
                    
                    ViewBag.Statistics = statistics;
                    ViewBag.Products = products;
                    ViewBag.IsEditMode = false;
                    
                    return View(store);
                }
                else
                {
                    // Store doesn't exist - show empty store template for creation
                    var emptyStore = new Store
                    {
                        SellerId = userId,
                        StoreName = "",
                        Description = ""
                    };
                    
                    ViewBag.Statistics = new Dictionary<string, object>();
                    ViewBag.Products = new List<Product>();
                    ViewBag.IsEditMode = true;
                    ViewBag.IsNewStore = true;
                    
                    return View(emptyStore);
                }
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading your store.";
                return View(new Store { SellerId = userId });
            }
        }

        // GET: Stores/Edit - Switch to edit mode
        public async Task<IActionResult> Edit()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to edit your store.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var store = await _storeService.GetStoreBySellerIdAsync(userId);
                
                if (store == null)
                {
                    // No store exists, create new one
                    store = new Store
                    {
                        SellerId = userId,
                        StoreName = "",
                        Description = ""
                    };
                    ViewBag.IsNewStore = true;
                }
                else
                {
                    ViewBag.IsNewStore = false;
                }

                ViewBag.IsEditMode = true;
                return View("Index", store);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading the store for editing.";
                return RedirectToAction("Index");
            }
        }

        // POST: Stores/Save - Create or Update store
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Store store, IFormFile? bannerFile, string? bannerUrl)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to save store information.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.IsEditMode = true;
                    ViewBag.IsNewStore = store.Id == 0;
                    return View("Index", store);
                }

                // Check if store name is unique
                var existingStore = await _storeService.GetStoreBySellerIdAsync(userId);
                var isUnique = await _storeService.IsStoreNameUniqueAsync(store.StoreName, existingStore?.Id);
                if (!isUnique)
                {
                    ModelState.AddModelError("StoreName", "This store name is already taken");
                    ViewBag.IsEditMode = true;
                    ViewBag.IsNewStore = store.Id == 0;
                    return View("Index", store);
                }

                // Handle banner image
                if (!string.IsNullOrEmpty(bannerUrl))
                {
                    if (bannerUrl == "REMOVE_BANNER")
                    {
                        // Remove banner
                        store.BannerImageUrl = null;
                    }
                    else
                    {
                        // Import from URL
                        try
                        {
                            // Validate URL
                            var uri = new Uri(bannerUrl);
                            
                            // Simple validation - check if it's an image URL
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                            var hasValidExtension = allowedExtensions.Any(ext => 
                                uri.AbsolutePath.ToLower().EndsWith(ext));
                            
                            if (hasValidExtension || bannerUrl.Contains("image") || bannerUrl.Contains("photo"))
                            {
                                store.BannerImageUrl = bannerUrl;
                            }
                            else
                            {
                                ModelState.AddModelError("BannerUrl", "URL does not appear to be a valid image");
                                ViewBag.IsEditMode = true;
                                ViewBag.IsNewStore = store.Id == 0;
                                return View("Index", store);
                            }
                        }
                        catch (UriFormatException)
                        {
                            ModelState.AddModelError("BannerUrl", "Please enter a valid URL");
                            ViewBag.IsEditMode = true;
                            ViewBag.IsNewStore = store.Id == 0;
                            return View("Index", store);
                        }
                    }
                }
                else if (bannerFile != null && bannerFile.Length > 0)
                {
                    // Handle file upload
                    // Validate file type
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                    if (!allowedTypes.Contains(bannerFile.ContentType.ToLower()))
                    {
                        ModelState.AddModelError("BannerFile", "Only image files (JPEG, PNG, GIF, WebP) are allowed");
                        ViewBag.IsEditMode = true;
                        ViewBag.IsNewStore = store.Id == 0;
                        return View("Index", store);
                    }

                    // Validate file size (max 5MB)
                    if (bannerFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("BannerFile", "File size cannot exceed 5MB");
                        ViewBag.IsEditMode = true;
                        ViewBag.IsNewStore = store.Id == 0;
                        return View("Index", store);
                    }

                    // Save the uploaded file
                    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "banners");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(bannerFile.FileName)}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await bannerFile.CopyToAsync(stream);
                    }

                    store.BannerImageUrl = $"/uploads/banners/{fileName}";
                }
                else if (existingStore != null)
                {
                    // Keep existing banner if no new file or URL provided
                    store.BannerImageUrl = existingStore.BannerImageUrl;
                }

                store.SellerId = userId; // Ensure seller ID is correct

                Store savedStore;
                if (existingStore == null)
                {
                    // Create new store
                    savedStore = await _storeService.CreateStoreAsync(store);
                    if (savedStore != null)
                    {
                        TempData["Success"] = "Store created successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create store. Please try again.";
                        ViewBag.IsEditMode = true;
                        ViewBag.IsNewStore = true;
                        return View("Index", store);
                    }
                }
                else
                {
                    // Update existing store
                    store.Id = existingStore.Id;
                    var success = await _storeService.UpdateStoreAsync(existingStore.Id, store);
                    if (success)
                    {
                        TempData["Success"] = "Store updated successfully!";
                        savedStore = store;
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update store. Please try again.";
                        ViewBag.IsEditMode = true;
                        ViewBag.IsNewStore = false;
                        return View("Index", store);
                    }
                }

                return RedirectToAction("Index");
            }
            catch
            {
                TempData["Error"] = "An error occurred while saving the store.";
                ViewBag.IsEditMode = true;
                ViewBag.IsNewStore = store.Id == 0;
                return View("Index", store);
            }
        }

        // POST: Stores/Delete - Delete seller's store
        [HttpPost]
        public async Task<IActionResult> Delete()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please log in to continue." });
            }

            if (!await CheckSellerAccessAsync())
            {
                return Json(new { success = false, message = "You need to be an approved seller to delete your store." });
            }

            try
            {
                var store = await _storeService.GetStoreBySellerIdAsync(userId);
                if (store == null)
                {
                    return Json(new { success = false, message = "No store found to delete." });
                }

                var success = await _storeService.DeleteStoreAsync(store.Id);

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