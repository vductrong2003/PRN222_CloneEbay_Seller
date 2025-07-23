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
}