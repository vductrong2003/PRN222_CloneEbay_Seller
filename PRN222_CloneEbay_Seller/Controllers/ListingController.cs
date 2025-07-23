using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class ListingController : Controller
    {
        private readonly IListingService _listingService;
        private const int SELLER_ID = 1; // Temporary hardcoded seller ID

        public ListingController(IListingService listingService)
        {
            _listingService = listingService;
        }

        public async Task<IActionResult> Index(string status = "all", string search = "", int? categoryId = null)
        {
            try
            {
                // Get products based on status
                var products = await _listingService.GetProductsByStatusAsync(SELLER_ID, status);

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    products = await _listingService.SearchProductsAsync(SELLER_ID, search, categoryId);
                }

                // Apply category filter
                if (categoryId.HasValue && string.IsNullOrEmpty(search))
                {
                    products = products.Where(p => p.CategoryId == categoryId).ToList();
                }

                // Get statistics and categories
                var statistics = await _listingService.GetProductStatisticsAsync(SELLER_ID);
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
                TempData["Error"] = "An error occurred while loading products.";
                return View(new List<Product>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var product = await _listingService.GetProductDetailsAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                // Get performance data
                var performance = await _listingService.GetProductPerformanceAsync(id);
                ViewBag.Performance = performance;

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading product details.";
                return RedirectToAction("Index");
            }
        }

        // GET: Listing/Create
        public async Task<IActionResult> Create()
        {
            // Get existing drafts for current seller
            var sellerId = GetCurrentSellerId(); // You'll need to implement this method
            var drafts = await _listingService.GetDraftsBySellerIdAsync(sellerId);

            ViewBag.Drafts = drafts;
            return View();
        }

        // POST: Listing/CreateDraft
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDraft(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["Error"] = "Please enter a product title";
                return RedirectToAction("Create");
            }

            var sellerId = GetCurrentSellerId();

            // Create new draft product
            var product = new Product
            {
                Title = title.Trim(),
                SellerId = sellerId,
                Status = "Draft",
                IsAuction = false,
                Price = 0
            };

            var success = await _listingService.CreateProductAsync(product);
            if (success)
            {
                TempData["Success"] = "Draft created successfully!";
                return RedirectToAction("Edit", new { id = product.Id });
            }

            TempData["Error"] = "Failed to create draft. Please check your input.";
            return RedirectToAction("Create");
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var product = await _listingService.GetProductDetailsAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                var categories = await _listingService.GetAllCategoriesAsync();
                ViewBag.Categories = categories;
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading the product.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            try
            {
                // Debug: Log received data
                Console.WriteLine($"Edit POST - ID: {id}, Product.Id: {product.Id}");
                Console.WriteLine($"Title: {product.Title}");
                Console.WriteLine($"Price: {product.Price}");
                Console.WriteLine($"CategoryId: {product.CategoryId}");
                Console.WriteLine($"Status: {product.Status}");
                Console.WriteLine($"IsAuction: {product.IsAuction}");

                if (id != product.Id)
                {
                    TempData["Error"] = "Invalid product ID.";
                    return RedirectToAction("Index");
                }

                // Get the original product to check status
                var originalProduct = await _listingService.GetProductDetailsAsync(id);
                if (originalProduct == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                Console.WriteLine($"Original Status: {originalProduct.Status}");

                // Business logic: Cannot change back to Draft once it's been published
                if (originalProduct.Status != "Draft" && product.Status == "Draft")
                {
                    TempData["Error"] = "Cannot change published product back to Draft status.";
                    var categories = await _listingService.GetAllCategoriesAsync();
                    ViewBag.Categories = categories;
                    ViewBag.OriginalStatus = originalProduct.Status;
                    return View(product);
                }

                // Validation: Draft products must have basic required fields to become Active
                if (originalProduct.Status == "Draft" && product.Status == "Active")
                {
                    var validationErrors = new List<string>();
                    
                    if (string.IsNullOrWhiteSpace(product.Title))
                        validationErrors.Add("Product title is required.");
                    
                    if (!product.Price.HasValue || product.Price <= 0)
                        validationErrors.Add("Valid price is required.");
                    
                    if (!product.CategoryId.HasValue)
                        validationErrors.Add("Category is required.");
                    
                    if (string.IsNullOrWhiteSpace(product.Description))
                        validationErrors.Add("Product description is required.");

                    if (product.IsAuction == true && !product.AuctionEndTime.HasValue)
                        validationErrors.Add("Auction end time is required for auction listings.");

                    if (validationErrors.Any())
                    {
                        Console.WriteLine($"Validation failed: {string.Join(", ", validationErrors)}");
                        TempData["Error"] = "Cannot activate product: " + string.Join(" ", validationErrors);
                        var categories = await _listingService.GetAllCategoriesAsync();
                        ViewBag.Categories = categories;
                        ViewBag.OriginalStatus = originalProduct.Status;
                        return View(product);
                    }
                }

                // Debug: Check ModelState
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState is invalid:");
                    foreach (var error in ModelState)
                    {
                        Console.WriteLine($"Key: {error.Key}");
                        foreach (var subError in error.Value.Errors)
                        {
                            Console.WriteLine($"  Error: {subError.ErrorMessage}");
                        }
                    }
                    
                    // Show specific validation errors
                    var modelErrors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")
                        .ToList();
                    
                    TempData["Error"] = "Validation failed: " + string.Join("; ", modelErrors);
                    var categories = await _listingService.GetAllCategoriesAsync();
                    ViewBag.Categories = categories;
                    ViewBag.OriginalStatus = originalProduct.Status;
                    return View(product);
                }

                Console.WriteLine("ModelState is valid, proceeding with update...");
                
                product.SellerId = SELLER_ID;
                
                var wasStatusChanged = originalProduct.Status != product.Status;
                
                var success = await _listingService.UpdateProductAsync(product);
                Console.WriteLine($"Update result: {success}");
                
                if (success)
                {
                    // If status changed from Draft to Active, ensure inventory exists
                    if (wasStatusChanged && originalProduct.Status == "Draft" && product.Status == "Active")
                    {
                        var existingInventory = await _listingService.GetInventoryByProductAsync(id);
                        if (!existingInventory.Any())
                        {
                            await _listingService.UpdateInventoryAsync(id, 1);
                        }
                    }
                    
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction("Details", new { id = product.Id });
                }

                Console.WriteLine("Update service returned false");
                TempData["Error"] = "Failed to update product. The service could not save the changes.";
                var categoriesForView = await _listingService.GetAllCategoriesAsync();
                ViewBag.Categories = categoriesForView;
                ViewBag.OriginalStatus = originalProduct.Status;
                return View(product);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in Edit: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["Error"] = $"An error occurred while updating the product: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                Console.WriteLine($"Delete attempt for Product ID: {id}");
                
                // Check if product exists and get its details
                var product = await _listingService.GetProductDetailsAsync(id);
                if (product == null)
                {
                    Console.WriteLine($"Product with ID {id} not found");
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                Console.WriteLine($"Product found: {product.Title}, Status: {product.Status}");
                Console.WriteLine($"Has OrderItems: {product.OrderItems?.Count ?? 0}");
                Console.WriteLine($"Has Reviews: {product.Reviews?.Count ?? 0}");
                Console.WriteLine($"Has Bids: {product.Bids?.Count ?? 0}");
                Console.WriteLine($"Has Inventories: {product.Inventories?.Count ?? 0}");

                // Check business rules - some products shouldn't be deleted
                var canDelete = true;
                var reasons = new List<string>();

                // Check if product has active orders
                if (product.OrderItems?.Any() == true)
                {
                    var activeOrders = product.OrderItems.Where(oi => 
                        oi.Order?.Status != "Cancelled" && 
                        oi.Order?.Status != "Returned").ToList();
                    
                    if (activeOrders.Any())
                    {
                        canDelete = false;
                        reasons.Add($"Product has {activeOrders.Count} active order(s)");
                        Console.WriteLine($"Cannot delete: has {activeOrders.Count} active orders");
                    }
                }

                // Check if auction is still active
                if (product.IsAuction == true && product.AuctionEndTime.HasValue && product.AuctionEndTime > DateTime.Now)
                {
                    if (product.Bids?.Any() == true)
                    {
                        canDelete = false;
                        reasons.Add("Active auction with existing bids");
                        Console.WriteLine("Cannot delete: active auction with bids");
                    }
                }

                // If cannot delete due to business rules
                if (!canDelete)
                {
                    var errorMessage = "Cannot delete product: " + string.Join(", ", reasons);
                    Console.WriteLine($"Delete blocked: {errorMessage}");
                    TempData["Error"] = errorMessage;
                    return RedirectToAction("Index");
                }

                Console.WriteLine("Business rules passed, attempting database deletion...");
                
                var success = await _listingService.DeleteProductAsync(id);
                Console.WriteLine($"Delete operation result: {success}");
                
                if (success)
                {
                    TempData["Success"] = "Product deleted successfully!";
                    Console.WriteLine("Product deleted successfully");
                }
                else
                {
                    TempData["Error"] = "Failed to delete product. The product may have dependencies that prevent deletion.";
                    Console.WriteLine("Delete failed - service returned false");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during delete: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                TempData["Error"] = $"An error occurred while deleting the product: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInventory([FromBody] UpdateInventoryRequest request)
        {
            try
            {
                // Check product status before allowing inventory update
                var product = await _listingService.GetProductDetailsAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // Only allow stock updates for Active or Inactive products
                if (product.Status != "Active" && product.Status != "Inactive")
                {
                    return Json(new { success = false, message = $"Cannot update stock for products with status '{product.Status}'. Only Active or Inactive products can have their stock updated." });
                }

                var success = await _listingService.UpdateInventoryAsync(request.ProductId, request.Quantity);
                if (success)
                {
                    return Json(new { success = true, message = "Stock updated successfully." });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to update stock." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            try
            {
                var success = await _listingService.UpdateProductStatusAsync(id, status);
                if (success)
                {
                    TempData["Success"] = $"Product status updated to {status}!";
                }
                else
                {
                    TempData["Error"] = "Failed to update product status.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating product status.";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Performance(int id)
        {
            try
            {
                var product = await _listingService.GetProductDetailsAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index");
                }

                var performance30Days = await _listingService.GetProductPerformanceAsync(id, 30);
                var performance7Days = await _listingService.GetProductPerformanceAsync(id, 7);
                var performance90Days = await _listingService.GetProductPerformanceAsync(id, 90);

                ViewBag.Performance30Days = performance30Days;
                ViewBag.Performance7Days = performance7Days;
                ViewBag.Performance90Days = performance90Days;

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading performance data.";
                return RedirectToAction("Index");
            }
        }

        // Quick action routes
        public async Task<IActionResult> Active()
        {
            return await Index("active");
        }

        public async Task<IActionResult> Inactive()
        {
            return await Index("inactive");
        }

        public async Task<IActionResult> Drafts()
        {
            return await Index("draft");
        }

        public async Task<IActionResult> Scheduled()
        {
            return await Index("scheduled");
        }

        public async Task<IActionResult> TopPerforming()
        {
            try
            {
                var topProducts = await _listingService.GetTopPerformingProductsAsync(SELLER_ID, 20);
                var statistics = await _listingService.GetProductStatisticsAsync(SELLER_ID);
                var categories = await _listingService.GetAllCategoriesAsync();

                ViewBag.CurrentStatus = "top-performing";
                ViewBag.Statistics = statistics;
                ViewBag.Categories = categories;

                return View("Index", topProducts);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading top performing products.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleProduct(int id, DateTime publishDate)
        {
            try
            {
                var success = await _listingService.ScheduleProductAsync(id, publishDate);
                if (success)
                {
                    TempData["Success"] = "Product scheduled successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to schedule product.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while scheduling the product.";
            }

            return RedirectToAction("Index");
        }

        // API endpoints for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetProductStats(int productId)
        {
            try
            {
                var performance = await _listingService.GetProductPerformanceAsync(productId);
                return Json(performance);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInventory(int productId)
        {
            try
            {
                var inventory = await _listingService.GetInventoryByProductAsync(productId);
                return Json(inventory);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Pause(int id)
        {
            try
            {
                var success = await _listingService.UpdateProductStatusAsync(id, "Inactive");
                return Json(new { success = success });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Activate(int id)
        {
            try
            {
                var success = await _listingService.UpdateProductStatusAsync(id, "Active");
                return Json(new { success = success });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper method to get current seller ID
        private int GetCurrentSellerId()
        {
            // TODO: Implement proper user authentication
            // For now, return a default seller ID
            return 1; // Replace with actual logic to get current user's seller ID
        }
    }

    // Helper class for UpdateInventory request
    public class UpdateInventoryRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
