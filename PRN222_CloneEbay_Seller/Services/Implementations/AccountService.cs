using Microsoft.EntityFrameworkCore;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace PRN222_CloneEbay_Seller.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly CloneEbayDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountService(CloneEbayDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<User?> ValidateUserAsync(string emailOrUsername, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == emailOrUsername || u.Username == emailOrUsername);

                if (user != null && !string.IsNullOrEmpty(user.Password) && VerifyPassword(password, user.Password))
                {
                    return user;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> RegisterUserAsync(User user)
        {
            try
            {
                // Hash the password before saving
                if (!string.IsNullOrEmpty(user.Password))
                {
                    user.Password = HashPassword(user.Password);
                }

                // Set default values - người dùng tự động trở thành seller
                user.Role = "User";  // Thay đổi từ "User" thành "Seller"
                user.Status = "Active";
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Email == email);
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.Username == username);
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GeneratePasswordAsync()
        {
            await Task.CompletedTask; // For async consistency
            
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<bool> SendPasswordEmailAsync(string email, string password, string fullName)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUser = _configuration["EmailSettings:SmtpUser"] ?? "";
                var smtpPass = _configuration["EmailSettings:SmtpPassword"] ?? "";
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUser;


                if (string.IsNullOrEmpty(smtpUser) || smtpUser.Contains("your-email") || 
                    string.IsNullOrEmpty(smtpPass) || smtpPass.Contains("your-app-password"))
                {
                    Console.WriteLine($"Email credentials not configured properly. Using console output only.");
                    return true; // Return success for development
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, "CloneEbay Seller Platform"),
                    Subject = "Welcome to CloneEbay Seller Platform - Your Account Details",
                    Body = $@"
                        <html>
                        <body>
                            <h2>Welcome to CloneEbay Seller Platform!</h2>
                            <p>Dear {fullName},</p>
                            <p>Your seller account has been successfully created. Here are your login credentials:</p>
                            <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p><strong>Email:</strong> {email}</p>
                                <p><strong>Password:</strong> {password}</p>
                            </div>
                            <p>Please keep these credentials safe and consider changing your password after your first login.</p>
                            <p>You can now log in to start selling on our platform!</p>
                            <br>
                            <p>Best regards,<br>CloneEbay Seller Team</p>
                        </body>
                        </html>",
                    IsBodyHtml = true
                };

                message.To.Add(email);
                await client.SendMailAsync(message);
                
                Console.WriteLine($"✅ Email sent successfully to {email}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email sending failed: {ex.Message}");
                Console.WriteLine($"Password for {email}: {password}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return true; // Return success for development to continue flow
            }
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null) return false;

                var newPassword = await GeneratePasswordAsync();
                user.Password = HashPassword(newPassword);
                
                await _context.SaveChangesAsync();

                // Send new password via email (with fallback to console)
                var emailSent = await SendPasswordEmailAsync(email, newPassword, user.Username ?? "User");


                return emailSent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Password reset failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.Password)) return false;

                if (!VerifyPassword(currentPassword, user.Password))
                    return false;

                user.Password = HashPassword(newPassword);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _context.Users.FindAsync(userId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            }
            catch
            {
                return null;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CanAccessSellerFeaturesAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }
            
            // Check if user has seller status and is active
            return user.Status == "Active" ;
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "CloneEbaySalt"));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            var hashToVerify = HashPassword(password);
            return hashToVerify == hashedPassword;
        }

        public async Task<ProfileViewModel?> GetUserProfileAsync(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Stores)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) return null;

                var statistics = await GetUserStatisticsAsync(userId);

                var profile = new ProfileViewModel
                {
                    Id = user.Id,
                    Username = user.Username ?? "",
                    Email = user.Email ?? "",
                    AvatarUrl = user.AvatarUrl,
                    Role = user.Role,
                    Status = user.Status,
                    Store = user.Stores?.FirstOrDefault(),
                    TotalListings = (int)(statistics.GetValueOrDefault("TotalListings", 0)),
                    ActiveListings = (int)(statistics.GetValueOrDefault("ActiveListings", 0)),
                    TotalOrders = (int)(statistics.GetValueOrDefault("TotalOrders", 0)),
                    TotalRevenue = (decimal)(statistics.GetValueOrDefault("TotalRevenue", 0m)),
                    AverageRating = (double)(statistics.GetValueOrDefault("AverageRating", 0.0)),
                    TotalReviews = (int)(statistics.GetValueOrDefault("TotalReviews", 0))
                };

                return profile;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, UpdateProfileViewModel model)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                // Check if username is unique (excluding current user)
                var usernameExists = await _context.Users
                    .AnyAsync(u => u.Username == model.Username && u.Id != userId);
                if (usernameExists) return false;

                // Check if email is unique (excluding current user)
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == model.Email && u.Id != userId);
                if (emailExists) return false;

                // Update user properties
                user.Username = model.Username;
                user.Email = model.Email;
                user.AvatarUrl = model.AvatarUrl;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetUserStatisticsAsync(int userId)
        {
            try
            {
                var statistics = new Dictionary<string, object>();

                // Product statistics
                var totalListings = await _context.Products
                    .CountAsync(p => p.SellerId == userId);
                var activeListings = await _context.Products
                    .CountAsync(p => p.SellerId == userId && p.Status == "Active");

                statistics["TotalListings"] = totalListings;
                statistics["ActiveListings"] = activeListings;

                // Order statistics
                var totalOrders = await _context.OrderTables
                    .Where(o => o.OrderItems.Any(oi => oi.Product.SellerId == userId))
                    .CountAsync();

                var totalRevenue = await _context.OrderTables
                    .Where(o => o.OrderItems.Any(oi => oi.Product.SellerId == userId))
                    .SelectMany(o => o.OrderItems)
                    .Where(oi => oi.Product.SellerId == userId)
                    .SumAsync(oi => oi.Quantity * oi.UnitPrice);

                statistics["TotalOrders"] = totalOrders;
                statistics["TotalRevenue"] = totalRevenue;

                // Review statistics
                var productIds = await _context.Products
                    .Where(p => p.SellerId == userId)
                    .Select(p => p.Id)
                    .ToListAsync();

                var reviews = await _context.Reviews
                    .Where(r => productIds.Contains((int)r.ProductId))
                    .ToListAsync();

                var totalReviews = reviews.Count;
                var averageRating = totalReviews > 0 ? reviews.Average(r => r.Rating) : 0.0;

                statistics["TotalReviews"] = totalReviews;
                statistics["AverageRating"] = averageRating;

                return statistics;
            }
            catch
            {
                return new Dictionary<string, object>
                {
                    ["TotalListings"] = 0,
                    ["ActiveListings"] = 0,
                    ["TotalOrders"] = 0,
                    ["TotalRevenue"] = 0m,
                    ["TotalReviews"] = 0,
                    ["AverageRating"] = 0.0
                };
            }
        }
    }
}