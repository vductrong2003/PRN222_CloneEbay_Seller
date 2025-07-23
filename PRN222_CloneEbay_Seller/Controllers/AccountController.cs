using Microsoft.AspNetCore.Mvc;
using PRN222_CloneEbay_Seller.Models;
using PRN222_CloneEbay_Seller.Services.Interfaces;

namespace PRN222_CloneEbay_Seller.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Redirect if already logged in
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel());
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await _accountService.ValidateUserAsync(model.EmailOrUsername, model.Password);

                if (user != null)
                {
                    // Set session data
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.Username ?? "");
                    HttpContext.Session.SetString("Username", user.Username ?? "");
                    HttpContext.Session.SetString("Email", user.Email ?? "");
                    HttpContext.Session.SetString("UserRole", user.Role ?? "Seller");
                    HttpContext.Session.SetString("Role", user.Role ?? "Seller");
                    HttpContext.Session.SetString("Status", user.Status ?? "Active");
                    
                    // Set avatar if available
                    if (!string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        HttpContext.Session.SetString("UserAvatar", user.AvatarUrl);
                    }

                    TempData["Success"] = "Login successful!";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid email/username or password");
                return View(model);
            }
            catch
            {
                TempData["Error"] = "An error occurred during login. Please try again.";
                return View(model);
            }
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            // Redirect if already logged in
            if (HttpContext.Session.GetInt32("UserId").HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if (await _accountService.IsEmailExistsAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered");
                    return View(model);
                }

                if (await _accountService.IsUsernameExistsAsync(model.Username))
                {
                    ModelState.AddModelError("Username", "This username is already taken");
                    return View(model);
                }

                var generatedPassword = await _accountService.GeneratePasswordAsync();

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = generatedPassword, 
                    Role = "Seller", // Tự động trở thành seller
                    Status = "Active"
                };

                var success = await _accountService.RegisterUserAsync(user);

                if (success)
                {
                    var emailSent = await _accountService.SendPasswordEmailAsync(
                        model.Email, 
                        generatedPassword, 
                        $"{model.FirstName} {model.LastName}"
                    );

                    if (emailSent)
                    {
                        TempData["Success"] = "Seller account created successfully! Your login credentials have been sent to your email.";
                    }
                    else
                    {
                        TempData["Warning"] = "Seller account created successfully! However, we couldn't send the email. Please contact support for your login credentials.";
                    }

                    return RedirectToAction("Login");
                }

                TempData["Error"] = "Failed to create account. Please try again.";
                return View(model);
            }
            catch
            {
                TempData["Error"] = "An error occurred during registration. Please try again.";
                return View(model);
            }
        }

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var success = await _accountService.ResetPasswordAsync(model.Email);

                if (success)
                {
                    TempData["Success"] = "A new password has been sent to your email address.";
                }
                else
                {
                    TempData["Error"] = "Email address not found or failed to send email.";
                }

                return RedirectToAction("Login");
            }
            catch
            {
                TempData["Error"] = "An error occurred while resetting password. Please try again.";
                return View(model);
            }
        }

        // GET: Account/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            return View(new ChangePasswordViewModel());
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return RedirectToAction("Login");
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var success = await _accountService.ChangePasswordAsync(userId.Value, model.CurrentPassword, model.NewPassword);

                if (success)
                {
                    TempData["Success"] = "Password changed successfully!";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                return View(model);
            }
            catch
            {
                TempData["Error"] = "An error occurred while changing password. Please try again.";
                return View(model);
            }
        }

        // Account/Logout - Support both GET and POST
        [HttpGet]
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("RememberMe");
            TempData["Info"] = "You have been logged out successfully";
            return RedirectToAction("Login");
        }

        // GET: /Account/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            try
            {
                var profile = await _accountService.GetUserProfileAsync(userId.Value);
                if (profile == null)
                {
                    TempData["Error"] = "Profile not found.";
                    return RedirectToAction("Login");
                }

                return View(profile);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading your profile.";
                return RedirectToAction("Login");
            }
        }

        // GET: /Account/EditProfile
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            try
            {
                var profile = await _accountService.GetUserProfileAsync(userId.Value);
                if (profile == null)
                {
                    TempData["Error"] = "Profile not found.";
                    return RedirectToAction("Profile");
                }

                var model = new UpdateProfileViewModel
                {
                    Username = profile.Username,
                    Email = profile.Email,
                    AvatarUrl = profile.AvatarUrl
                };

                return View(model);
            }
            catch
            {
                TempData["Error"] = "An error occurred while loading the edit form.";
                return RedirectToAction("Profile");
            }
        }

        // POST: /Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(UpdateProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _accountService.UpdateUserProfileAsync(userId.Value, model);
                
                if (success)
                {
                    TempData["Success"] = "Profile updated successfully!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    TempData["Error"] = "Failed to update profile. Username or email may already be taken.";
                    return View(model);
                }
            }
            catch
            {
                TempData["Error"] = "An error occurred while updating your profile.";
                return View(model);
            }
        }


        
    }
}
