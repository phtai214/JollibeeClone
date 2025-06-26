using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Models;
using JollibeeClone.ViewModels;
using JollibeeClone.Attributes;
using System.Security.Cryptography;
using System.Text;

namespace JollibeeClone.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            // Nếu đã đăng nhập thì chuyển về trang chủ
            if (IsUserLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
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

                // Kiểm tra email đã tồn tại
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được đăng ký.");
                    return View(model);
                }

                // Kiểm tra số điện thoại đã tồn tại
                var existingPhone = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);
                if (existingPhone != null)
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được đăng ký.");
                    return View(model);
                }

                // Đảm bảo có role "User"
                var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
                if (userRole == null)
                {
                    userRole = new Role { RoleName = "User" };
                    _context.Roles.Add(userRole);
                    await _context.SaveChangesAsync();
                }

                // Tạo user mới
                var newUser = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    PasswordHash = HashPassword(model.Password),
                    IsActive = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Gán role User
                var userRoleAssignment = new UserRole
                {
                    UserID = newUser.UserID,
                    RoleID = userRole.RoleID
                };

                _context.UserRoles.Add(userRoleAssignment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New user registered: {Email}", model.Email);
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                
                // Chuyển hướng đến trang đăng nhập
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration: {Email}", model.Email);
                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình đăng ký. Vui lòng thử lại.";
                return View(model);
            }
        }

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập thì chuyển về trang chủ
            if (IsUserLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserLoginViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Tìm user theo email hoặc số điện thoại
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => 
                        (u.Email == model.EmailOrPhone || u.PhoneNumber == model.EmailOrPhone) 
                        && u.IsActive);

                if (user == null)
                {
                    ModelState.AddModelError("", "Thông tin đăng nhập không chính xác.");
                    return View(model);
                }

                // Kiểm tra mật khẩu
                if (!VerifyPassword(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "Thông tin đăng nhập không chính xác.");
                    return View(model);
                }

                // Kiểm tra user không phải admin
                var isAdmin = user.UserRoles.Any(ur => ur.Role.RoleName.ToLower() == "admin");
                if (isAdmin)
                {
                    ModelState.AddModelError("", "Vui lòng sử dụng trang đăng nhập dành cho quản trị viên.");
                    return View(model);
                }

                // Lưu thông tin đăng nhập vào session
                HttpContext.Session.SetString("IsUserLoggedIn", "true");
                HttpContext.Session.SetString("UserId", user.UserID.ToString());
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserEmail", user.Email);

                if (model.RememberMe)
                {
                    // Tạo cookie remember me
                    Response.Cookies.Append("UserRememberMe", "true", new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30),
                        HttpOnly = true,
                        Secure = true
                    });
                }

                TempData["SuccessMessage"] = "Đăng nhập thành công!";
                _logger.LogInformation("User logged in: {Email}", user.Email);

                // Chuyển hướng về trang chủ
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login: {EmailOrPhone}", model.EmailOrPhone);
                ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình đăng nhập. Vui lòng thử lại.");
                return View(model);
            }
        }

        // POST: Account/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            // Xóa session
            HttpContext.Session.Clear();
            
            // Xóa cookie remember me
            Response.Cookies.Delete("UserRememberMe");
            
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }

        // Helper methods
        private bool IsUserLoggedIn()
        {
            return HttpContext.Session.GetString("IsUserLoggedIn") == "true";
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hashedPassword = Convert.ToBase64String(hashedBytes);
                return hashedPassword == hash;
            }
        }
    }
} 