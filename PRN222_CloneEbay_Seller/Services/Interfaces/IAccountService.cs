using PRN222_CloneEbay_Seller.Models;

namespace PRN222_CloneEbay_Seller.Services.Interfaces
{
    public interface IAccountService
    {
        // Authentication methods
        Task<User?> ValidateUserAsync(string emailOrUsername, string password);
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsUsernameExistsAsync(string username);
        
        // Registration methods
        Task<bool> RegisterUserAsync(User user);
        Task<string> GeneratePasswordAsync();
        Task<bool> SendPasswordEmailAsync(string email, string password, string fullName);
        
        // Password management
        Task<bool> ResetPasswordAsync(string email);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        
        // User management
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<bool> UpdateUserAsync(User user);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        
        // Seller feature access
        Task<bool> CanAccessSellerFeaturesAsync(int userId);
        
        // Profile management
        Task<ProfileViewModel?> GetUserProfileAsync(int userId);
        Task<bool> UpdateUserProfileAsync(int userId, UpdateProfileViewModel model);
        Task<Dictionary<string, object>> GetUserStatisticsAsync(int userId);
    }
}