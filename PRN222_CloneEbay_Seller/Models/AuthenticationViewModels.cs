using System.ComponentModel.DataAnnotations;

namespace PRN222_CloneEbay_Seller.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email or Username is required")]
        [Display(Name = "Email or Username")]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "You must agree to the terms and conditions")]
        [Display(Name = "I agree to the Terms and Conditions")]
        public bool AgreeToTerms { get; set; }

        [Display(Name = "Subscribe to promotional emails")]
        public bool SubscribeToPromotions { get; set; } = true;
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class SellerRequestViewModel
    {
        [Required(ErrorMessage = "Business name is required")]
        [Display(Name = "Business Name")]
        [StringLength(200, ErrorMessage = "Business name cannot exceed 200 characters")]
        public string BusinessName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Business address is required")]
        [Display(Name = "Business Address")]
        [StringLength(500, ErrorMessage = "Business address cannot exceed 500 characters")]
        public string BusinessAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Business phone is required")]
        [Display(Name = "Business Phone")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string BusinessPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Business email is required")]
        [Display(Name = "Business Email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string BusinessEmail { get; set; } = string.Empty;

        [Display(Name = "Tax ID (Optional)")]
        [StringLength(50, ErrorMessage = "Tax ID cannot exceed 50 characters")]
        public string? TaxId { get; set; }

        [Required(ErrorMessage = "Please explain why you want to become a seller")]
        [Display(Name = "Why do you want to become a seller?")]
        [StringLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
        public string RequestReason { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must agree to the seller terms and conditions")]
        [Display(Name = "I agree to the Seller Terms and Conditions")]
        public bool AgreeToSellerTerms { get; set; }
    }

    public class ProfileViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Username is required")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Avatar URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? AvatarUrl { get; set; }

        public string? Role { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Store information if user is a seller
        public Store? Store { get; set; }
        
        // Statistics
        public int TotalListings { get; set; }
        public int ActiveListings { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }

    public class UpdateProfileViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Avatar URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? AvatarUrl { get; set; }
    }

    public class OverviewViewModel
    {
        // Store Information
        public Store? Store { get; set; }
        public string StoreName => Store?.StoreName ?? "No Store";
        public string StoreDescription => Store?.Description ?? "No description";
        public string? StoreLogoUrl => Store?.BannerImageUrl;

        // Sales Statistics
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal WeeklyRevenue { get; set; }
        public decimal DailyRevenue { get; set; }
        public decimal RevenueGrowth { get; set; } // % compared to last month

        // Order Statistics
        public int TotalOrders { get; set; }
        public int MonthlyOrders { get; set; }
        public int WeeklyOrders { get; set; }
        public int DailyOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int RefundedOrders { get; set; }

        // Order Success Rates
        public double SuccessRate { get; set; } // % delivered orders
        public double RefundRate { get; set; } // % refunded orders
        public double CancellationRate { get; set; } // % cancelled orders

        // Product Statistics
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int DraftProducts { get; set; }

        // Customer & Review Statistics
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStarReviews { get; set; }
        public int FourStarReviews { get; set; }
        public int ThreeStarReviews { get; set; }
        public int TwoStarReviews { get; set; }
        public int OneStarReviews { get; set; }

        // Top Products
        public List<TopSellerProductViewModel> TopSellingProducts { get; set; } = new();
        public List<TopSellerProductViewModel> TopRatedProducts { get; set; } = new();
        public List<TopSellerProductViewModel> TopRevenueProducts { get; set; } = new();

        // Recent Activities
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new();
        public List<RecentReviewViewModel> RecentReviews { get; set; } = new();

        // Performance Metrics
        public decimal AverageOrderValue { get; set; }
        public int AverageDeliveryDays { get; set; }
        public double CustomerReturnRate { get; set; }

        // Monthly Data for Charts
        public List<MonthlyDataViewModel> MonthlyRevenueData { get; set; } = new();
        public List<MonthlyDataViewModel> MonthlyOrderData { get; set; } = new();
    }

    public class TopSellerProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int StockQuantity { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class RecentOrderViewModel
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public int ItemCount { get; set; }
    }

    public class RecentReviewViewModel
    {
        public int ReviewId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
    }

    public class MonthlyDataViewModel
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public int Year { get; set; }
        public int MonthNumber { get; set; }
    }
}