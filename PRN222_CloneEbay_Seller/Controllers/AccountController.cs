//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using PRN222_CloneEbay_Seller.Models;

//namespace PRN222_CloneEbay_Seller.Controllers
//{
//    public class AccountController : Controller
//    {
//        private readonly IUserService _UserService;
//        private readonly IWebHostEnvironment _environment;

//        public AccountController(IUserService UserService, IWebHostEnvironment environment)
//        {
//            _UserService = UserService;
//            _environment = environment;
//        }

//        [HttpGet]
//        public IActionResult Register()
//        {
//            return View(new User());
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Register(User model, IFormFile? AvatarFile, string ConfirmPassword, bool AgreeToTerms)
//        {
//            // Custom validation
//            if (!AgreeToTerms)
//            {
//                ModelState.AddModelError("", "You must agree to the terms and conditions");
//            }

//            if (string.IsNullOrEmpty(ConfirmPassword) || model.Password != ConfirmPassword)
//            {
//                ModelState.AddModelError("", "Passwords do not match");
//            }

//            // Check if email exists
//            if (await _UserService.IsEmailExistsAsync(model.email))
//            {
//                ModelState.AddModelError("email", "This email is already registered");
//            }

//            // Check if Username exists
//            if (await _UserService.IsUsernameExistsAsync(model.Username))
//            {
//                ModelState.AddModelError("Username", "This Username is already taken");
//            }

//            if (!ModelState.IsValid)
//            {
//                return View(model);
//            }

//            // Hash Password
//            model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

//            // Handle avatar upload
//            if (AvatarFile != null && AvatarFile.Length > 0)
//            {
//                model.avatarURL = await SaveAvatarAsync(AvatarFile);
//            }

//            // Save User
//            _context.Users.Add(model);
//            await _context.SaveChangesAsync();

//            // Set session
//            HttpContext.Session.SetInt32("UserId", model.id);
//            HttpContext.Session.SetString("Username", model.Username);
//            HttpContext.Session.SetString("Role", model.role);

//            TempData["SuccessMessage"] = "Account created successfully!";
//            return RedirectToAction("Overview", "Seller");
//        }

//        [HttpGet]
//        public IActionResult Login()
//        {
//            return View(new LoginModel());
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Login(LoginModel model)
//        {
//            if (!ModelState.IsValid)
//            {
//                return View(model);
//            }

//            var User = await _UserService.ValidateUserAsync(model.EmailOrUsername, model.Password);

//            if (User != null)
//            {
//                // Set session
//                HttpContext.Session.SetInt32("UserId", User.id);
//                HttpContext.Session.SetString("Username", User.Username);
//                HttpContext.Session.SetString("Email", User.email);
//                HttpContext.Session.SetString("Role", User.role);
//                HttpContext.Session.SetString("AvatarURL", User.avatarURL ?? "");

//                // Set remember me cookie
//                if (model.RememberMe)
//                {
//                    var cookieOptions = new CookieOptions
//                    {
//                        Expires = DateTime.Now.AddDays(30),
//                        HttpOnly = true,
//                        Secure = true
//                    };
//                    Response.Cookies.Append("RememberMe", User.id.ToString(), cookieOptions);
//                }

//                TempData["SuccessMessage"] = $"Welcome back, {User.Username}!";
//                return RedirectToAction("Overview", "Seller");
//            }

//            ModelState.AddModelError("", "Invalid email/Username or Password");
//            return View(model);
//        }

//        [HttpPost]
//        public IActionResult Logout()
//        {
//            HttpContext.Session.Clear();
//            Response.Cookies.Delete("RememberMe");
//            TempData["InfoMessage"] = "You have been logged out successfully";
//            return RedirectToAction("Login");
//        }

//        private async Task<string> SaveAvatarAsync(IFormFile avatar)
//        {
//            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");

//            if (!Directory.Exists(uploadsFolder))
//                Directory.CreateDirectory(uploadsFolder);

//            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
//            var filePath = Path.Combine(uploadsFolder, fileName);

//            using (var stream = new FileStream(filePath, FileMode.Create))
//            {
//                await avatar.CopyToAsync(stream);
//            }

//            return $"/uploads/avatars/{fileName}";
//        }
//    }
//}
