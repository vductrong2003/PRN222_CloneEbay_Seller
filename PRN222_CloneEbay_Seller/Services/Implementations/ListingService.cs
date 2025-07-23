using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class ListingService : IListingService
    {
        private readonly CloneEbayDbContext _context;

        public ListingService(CloneEbayDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProductsAsync(int sellerId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Include(p => p.OrderItems)
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<Product>> GetProductsByStatusAsync(int sellerId, string status)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Include(p => p.OrderItems)
                .Where(p => p.SellerId == sellerId);

            return status.ToLower() switch
            {
                "active" => await query.Where(p => p.Status == "Active").ToListAsync(),
                "inactive" => await query.Where(p => p.Status == "Inactive").ToListAsync(),
                "draft" => await query.Where(p => p.Status == "Draft").ToListAsync(),
                "scheduled" => await query.Where(p => p.Status == "Scheduled").ToListAsync(),
                "ended" => await query.Where(p => p.Status == "Ended").ToListAsync(),
                "auction" => await query.Where(p => p.IsAuction == true).ToListAsync(),
                "buy-now" => await query.Where(p => p.IsAuction == false).ToListAsync(),
                _ => await GetAllProductsAsync(sellerId)
            };
        }

        public async Task<Product?> GetProductDetailsAsync(int productId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Include(p => p.OrderItems)
                .ThenInclude(oi => oi.Order)
                .Include(p => p.Reviews)
                .Include(p => p.Bids)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<Dictionary<string, int>> GetProductStatisticsAsync(int sellerId)
        {
            var products = await _context.Products
                .Include(p => p.Inventories)
                .Where(p => p.SellerId == sellerId)
                .ToListAsync();

            return new Dictionary<string, int>
            {
                ["Total"] = products.Count,
                ["Active"] = products.Count(p => p.Status == "Active"),
                ["Inactive"] = products.Count(p => p.Status == "Inactive"),
                ["Draft"] = products.Count(p => p.Status == "Draft"),
                ["Scheduled"] = products.Count(p => p.Status == "Scheduled"),
                ["Ended"] = products.Count(p => p.Status == "Ended"),
                ["Auction"] = products.Count(p => p.IsAuction == true),
                ["BuyNow"] = products.Count(p => p.IsAuction == false),
                ["LowStock"] = products.Count(p => p.Inventories.Any(i => i.Quantity > 0 && i.Quantity <= 5))
            };
        }

        public async Task<bool> CreateProductAsync(Product product)
        {
            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log error here
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            try
            {
                Console.WriteLine($"UpdateProductAsync called for Product ID: {product.Id}");
                Console.WriteLine($"Product data: Title={product.Title}, Price={product.Price}, CategoryId={product.CategoryId}, Status={product.Status}");
                
                // Find the existing product first
                var existingProduct = await _context.Products.FindAsync(product.Id);
                if (existingProduct == null)
                {
                    Console.WriteLine($"Product with ID {product.Id} not found in database");
                    return false;
                }

                // Update only the fields that should be modified
                existingProduct.Title = product.Title;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.Status = product.Status;
                existingProduct.IsAuction = product.IsAuction;
                existingProduct.AuctionEndTime = product.AuctionEndTime;
                existingProduct.Images = product.Images;
                existingProduct.SellerId = product.SellerId;

                Console.WriteLine("Attempting to save changes...");
                var changesSaved = await _context.SaveChangesAsync();
                Console.WriteLine($"Changes saved: {changesSaved} entities affected");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateProductAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            try
            {
                Console.WriteLine($"DeleteProductAsync called for Product ID: {productId}");
                
                var product = await _context.Products
                    .Include(p => p.OrderItems)
                        .ThenInclude(oi => oi.Order)
                    .Include(p => p.Reviews)
                    .Include(p => p.Bids)
                    .Include(p => p.Inventories)
                    .FirstOrDefaultAsync(p => p.Id == productId);
                
                if (product == null)
                {
                    Console.WriteLine($"Product with ID {productId} not found in database");
                    return false;
                }

                Console.WriteLine($"Found product: {product.Title}");
                
                // Log related entities before deletion
                var orderItemsCount = product.OrderItems?.Count ?? 0;
                var reviewsCount = product.Reviews?.Count ?? 0;
                var bidsCount = product.Bids?.Count ?? 0;
                var inventoriesCount = product.Inventories?.Count ?? 0;
                
                Console.WriteLine($"Related entities - OrderItems: {orderItemsCount}, Reviews: {reviewsCount}, Bids: {bidsCount}, Inventories: {inventoriesCount}");

                // Remove related entities first to avoid foreign key constraints
                if (product.Inventories?.Any() == true)
                {
                    Console.WriteLine($"Removing {product.Inventories.Count} inventory records");
                    _context.Inventories.RemoveRange(product.Inventories);
                }

                if (product.Reviews?.Any() == true)
                {
                    Console.WriteLine($"Removing {product.Reviews.Count} review records");
                    _context.Reviews.RemoveRange(product.Reviews);
                }

                if (product.Bids?.Any() == true)
                {
                    Console.WriteLine($"Removing {product.Bids.Count} bid records");
                    _context.Bids.RemoveRange(product.Bids);
                }

                // Note: OrderItems should NOT be deleted as they are part of order history
                // Only log if there are order items but don't delete them
                if (orderItemsCount > 0)
                {
                    Console.WriteLine($"Warning: Product has {orderItemsCount} order items. These will remain for order history.");
                    
                    // Check if any orders are still active - this should prevent deletion
                    var activeOrderItems = product.OrderItems.Where(oi => 
                        oi.Order?.Status != "Cancelled" && 
                        oi.Order?.Status != "Returned" &&
                        oi.Order?.Status != "Delivered").ToList();
                    
                    if (activeOrderItems.Any())
                    {
                        Console.WriteLine($"Cannot delete: Product has {activeOrderItems.Count} active order items");
                        return false;
                    }
                }

                // Remove the product itself
                Console.WriteLine("Removing product record");
                _context.Products.Remove(product);
                
                Console.WriteLine("Saving changes to database...");
                var changesCount = await _context.SaveChangesAsync();
                Console.WriteLine($"Database changes saved: {changesCount} entities affected");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in DeleteProductAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    
                    // Check for common SQL constraint violations
                    if (ex.InnerException.Message.Contains("FOREIGN KEY constraint"))
                    {
                        Console.WriteLine("Foreign key constraint violation detected");
                    }
                    if (ex.InnerException.Message.Contains("REFERENCE constraint"))
                    {
                        Console.WriteLine("Reference constraint violation detected");
                    }
                }
                return false;
            }
        }

        public async Task<bool> UpdateProductStatusAsync(int productId, string status)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    var oldStatus = product.Status;
                    product.Status = status;

                    // If changing from Draft to Active, create inventory if it doesn't exist
                    if (oldStatus == "Draft" && status == "Active")
                    {
                        var existingInventory = await _context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == productId);
                        
                        if (existingInventory == null)
                        {
                            var inventory = new Inventory
                            {
                                ProductId = productId,
                                Quantity = 1,
                                LastUpdated = DateTime.Now
                            };
                            _context.Inventories.Add(inventory);
                        }
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Inventory>> GetInventoryByProductAsync(int productId)
        {
            return await _context.Inventories
                .Where(i => i.ProductId == productId)
                .ToListAsync();
        }

        public async Task<bool> UpdateInventoryAsync(int productId, int quantity)
        {
            try
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == productId);
                
                if (inventory != null)
                {
                    inventory.Quantity = quantity;
                    inventory.LastUpdated = DateTime.Now;
                }
                else
                {
                    // Create new inventory if it doesn't exist
                    inventory = new Inventory
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        LastUpdated = DateTime.Now
                    };
                    _context.Inventories.Add(inventory);
                }
                
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<List<Product>> SearchProductsAsync(int sellerId, string query, int? categoryId = null)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Where(p => p.SellerId == sellerId);

            if (!string.IsNullOrEmpty(query))
            {
                products = products.Where(p => p.Title!.Contains(query) || p.Description!.Contains(query));
            }

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }

            return await products.OrderByDescending(p => p.Id).ToListAsync();
        }

        public async Task<Dictionary<string, object>> GetProductPerformanceAsync(int productId, int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days);
            
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.ProductId == productId && oi.Order!.OrderDate >= startDate)
                .ToListAsync();

            var totalSold = orderItems.Sum(oi => oi.Quantity ?? 0);
            var totalRevenue = orderItems.Sum(oi => (oi.Quantity ?? 0) * (oi.UnitPrice ?? 0));
            var ordersCount = orderItems.Select(oi => oi.OrderId).Distinct().Count();

            return new Dictionary<string, object>
            {
                ["TotalSold"] = totalSold,
                ["TotalRevenue"] = totalRevenue,
                ["OrdersCount"] = ordersCount,
                ["AverageOrderValue"] = ordersCount > 0 ? totalRevenue / ordersCount : 0,
                ["Period"] = days
            };
        }

        public async Task<List<Product>> GetTopPerformingProductsAsync(int sellerId, int count = 10)
        {
            var startDate = DateTime.Now.AddDays(-30);

            var topProductIds = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product!.SellerId == sellerId && oi.Order!.OrderDate >= startDate)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new {
                    ProductId = g.Key,
                    TotalRevenue = g.Sum(oi => (oi.Quantity ?? 0) * (oi.UnitPrice ?? 0))
                })
                .OrderByDescending(p => p.TotalRevenue)
                .Take(count)
                .Select(p => p.ProductId)
                .ToListAsync();

            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Where(p => topProductIds.Contains(p.Id))
                .ToListAsync();
        }

        public async Task<bool> BulkUpdateStatusAsync(List<int> productIds, string status)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                // Apply status changes based on business logic
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> BulkDeleteAsync(List<int> productIds)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                _context.Products.RemoveRange(products);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Product>> GetDraftProductsAsync(int sellerId)
        {
            // Assuming products without inventory are drafts
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Where(p => p.SellerId == sellerId && !p.Inventories.Any())
                .ToListAsync();
        }

        public async Task<List<Product>> GetScheduledProductsAsync(int sellerId)
        {
            // Assuming products with future dates are scheduled
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Where(p => p.SellerId == sellerId && p.AuctionEndTime > DateTime.Now)
                .ToListAsync();
        }

        public async Task<bool> ScheduleProductAsync(int productId, DateTime publishDate)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    product.AuctionEndTime = publishDate;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Product>> GetDraftsBySellerIdAsync(int sellerId)
        {
            return await _context.Products
                .Where(p => p.SellerId == sellerId && p.Status == "Draft")
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }
    }
}
