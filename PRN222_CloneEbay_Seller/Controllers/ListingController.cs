using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class ListingController : Controller
    {
        private readonly IListingService _listingService;
        private readonly IAccountService _accountService;

        public ListingController(IListingService listingService, IAccountService accountService)
        {
            _listingService = listingService;
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

        public async Task<IActionResult> Index(string status = "all", string search = "", int? categoryId = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to access this feature.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                // Get products based on status for current seller
                var products = await _listingService.GetProductsByStatusAsync(userId, status);

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    products = await _listingService.SearchProductsAsync(userId, search, categoryId);
                }

                // Apply category filter
                if (categoryId.HasValue && string.IsNullOrEmpty(search))
                {
                    products = products.Where(p => p.CategoryId == categoryId).ToList();
                }

                // Get statistics and categories
                var statistics = await _listingService.GetProductStatisticsAsync(userId);
                var categories = await _listingService.GetAllCategoriesAsync();

                // Pass data to view
                ViewBag.CurrentStatus = status;
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentCategoryId = categoryId;
                ViewBag.Statistics = statistics;
                ViewBag.Categories = categories;

                return View(products);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ListingController.Index: {ex.Message}");
                TempData["Error"] = "An error occurred while loading your listings.";
                return View(new List<Product>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to access this feature.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var product = await _listingService.GetProductDetailsAsync(id);
                
                if (product == null)
                {
                    return NotFound();
                }

                // Check if the product belongs to current seller
                if (product.SellerId != userId)
                {
                    TempData["Error"] = "You can only view your own products.";
                    return RedirectToAction("Index");
                }

                // Load reviews and related products
                var reviews = await _listingService.GetProductReviewsAsync(id);
                var relatedProducts = await _listingService.GetRelatedProductsAsync(id);

                ViewBag.Reviews = reviews;
                ViewBag.RelatedProducts = relatedProducts;

                return View(product);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading product details.";
                return RedirectToAction("Index");
            }
        }

        // GET: Listing/Create
        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to create listings.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var categories = await _listingService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(new Product());
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading the create form.";
                return RedirectToAction("Index");
            }
        }

        // POST: Listing/CreateDraft
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDraft(string title)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to create listings.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    TempData["Error"] = "Title is required";
                    return RedirectToAction("Create");
                }

                var product = new Product
                {
                    Title = title,
                    Status = "Draft",
                    SellerId = userId,
                    Price = 0,
                    Description = "",
                    CategoryId = 1
                };

                var success = await _listingService.CreateProductAsync(product);

                if (success)
                {
                    TempData["Success"] = "Draft created successfully!";
                    return RedirectToAction("Edit", new { id = product.Id });
                }

                TempData["Error"] = "Failed to create draft";
                return RedirectToAction("Create");
            }
            catch
            {
                TempData["Error"] = "An error occurred while creating the draft";
                return RedirectToAction("Create");
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to edit listings.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var product = await _listingService.GetProductDetailsAsync(id);

                if (product == null)
                {
                    return NotFound();
                }

                // Check if the product belongs to current seller
                if (product.SellerId != userId)
                {
                    TempData["Error"] = "You can only edit your own products.";
                    return RedirectToAction("Index");
                }

                var categories = await _listingService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                ViewBag.OriginalStatus = product.Status;

                return View(product);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading the product for editing.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to edit listings.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var originalProduct = await _listingService.GetProductDetailsAsync(id);
                if (originalProduct == null)
                {
                    return NotFound();
                }

                // Check if the product belongs to current seller
                if (originalProduct.SellerId != userId)
                {
                    TempData["Error"] = "You can only edit your own products.";
                    return RedirectToAction("Index");
                }

                // Validate activation requirements if status is being changed to Active
                if (originalProduct.Status != "Active" && product.Status == "Active")
                {
                    // Simple validation - can be expanded later
                    var validationErrors = new List<string>();
                    
                    if (string.IsNullOrEmpty(product.Title))
                        validationErrors.Add("Title is required");
                    if (product.Price <= 0)
                        validationErrors.Add("Price must be greater than 0");
                    if (string.IsNullOrEmpty(product.Description))
                        validationErrors.Add("Description is required");
                    
                    if (validationErrors.Count > 0)
                    {
                        TempData["Error"] = "Cannot activate product: " + string.Join(", ", validationErrors);
                        var categories = await _listingService.GetAllCategoriesAsync();
                        ViewBag.Categories = categories;
                        ViewBag.OriginalStatus = originalProduct.Status;
                        return View(product);
                    }
                }

                if (!ModelState.IsValid)
                {
                    var modelErrors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => $"{x.Key}: {string.Join(", ", x.Value?.Errors.Select(e => e.ErrorMessage) ?? new List<string>())}")
                        .ToList();
                    
                    TempData["Error"] = "Validation failed: " + string.Join("; ", modelErrors);
                    var categories = await _listingService.GetAllCategoriesAsync();
                    ViewBag.Categories = categories;
                    ViewBag.OriginalStatus = originalProduct.Status;
                    return View(product);
                }
                
                product.SellerId = userId; // Ensure seller ID is set correctly
                
                var success = await _listingService.UpdateProductAsync(product);

                if (success)
                {
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction("Details", new { id = product.Id });
                }

                TempData["Error"] = "Failed to update product. Please try again.";
                var categoriesForError = await _listingService.GetAllCategoriesAsync();
                ViewBag.Categories = categoriesForError;
                ViewBag.OriginalStatus = originalProduct.Status;
                return View(product);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Edit: {ex.Message}");
                TempData["Error"] = "An error occurred while updating the product.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                TempData["Error"] = "Please log in to continue.";
                return RedirectToAction("Login", "Account");
            }

            if (!await CheckSellerAccessAsync())
            {
                TempData["Error"] = "You need to be an approved seller to delete listings.";
                return RedirectToAction("RequestSeller", "Account");
            }

            try
            {
                var product = await _listingService.GetProductDetailsAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                // Check if the product belongs to current seller
                if (product.SellerId != userId)
                {
                    TempData["Error"] = "You can only delete your own products.";
                    return RedirectToAction("Index");
                }

                var success = await _listingService.DeleteProductAsync(id);

                if (success)
                {
                    TempData["Success"] = "Product deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete product.";
                }

                return RedirectToAction("Index");
            }
            catch
            {
                TempData["Error"] = "An error occurred while deleting the product.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInventory([FromBody] UpdateInventoryRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please log in to continue." });
            }

            if (!await CheckSellerAccessAsync())
            {
                return Json(new { success = false, message = "You need to be an approved seller to update inventory." });
            }

            try
            {
                var product = await _listingService.GetProductDetailsAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // Check if the product belongs to current seller
                if (product.SellerId != userId)
                {
                    return Json(new { success = false, message = "You can only update inventory for your own products." });
                }

                var success = await _listingService.UpdateInventoryAsync(request.ProductId, request.Quantity);

                if (success)
                {
                    return Json(new { success = true, message = "Inventory updated successfully!" });
                }

                return Json(new { success = false, message = "Failed to update inventory." });
            }
            catch
            {
                return Json(new { success = false, message = "An error occurred while updating inventory." });
            }
        }
    }

    // Helper class for UpdateInventory request
    public class UpdateInventoryRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
