using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Models;
using JollibeeClone.ViewModels;
using JollibeeClone.Data;
using System.Security.Cryptography;
using System.Text;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập thì chuyển về dashboard
            if (IsAdminLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Tìm user theo email
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

                if (user == null)
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác");
                    return View(model);
                }

                // Kiểm tra mật khẩu
                if (!VerifyPassword(model.Password, user.PasswordHash))
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác");
                    return View(model);
                }

                // Kiểm tra quyền admin
                var isAdmin = user.UserRoles.Any(ur => ur.Role.RoleName.ToLower() == "admin");
                if (!isAdmin)
                {
                    ModelState.AddModelError("", "Bạn không có quyền truy cập vào khu vực quản trị");
                    return View(model);
                }

                // Lưu thông tin đăng nhập vào session
                HttpContext.Session.SetString("AdminUserId", user.UserID.ToString());
                HttpContext.Session.SetString("AdminUserName", user.FullName);
                HttpContext.Session.SetString("AdminEmail", user.Email);
                HttpContext.Session.SetString("IsAdminLoggedIn", "true");

                if (model.RememberMe)
                {
                    // Có thể lưu cookie ở đây nếu cần
                    Response.Cookies.Append("AdminRememberMe", "true", new CookieOptions
                    {
                        Expires = DateTime.Now.AddDays(30),
                        HttpOnly = true,
                        Secure = true
                    });
                }

                TempData["SuccessMessage"] = "Đăng nhập thành công!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình đăng nhập. Vui lòng thử lại.");
                return View(model);
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // Xóa session
            HttpContext.Session.Clear();
            
            // Xóa cookie remember me
            Response.Cookies.Delete("AdminRememberMe");
            
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Login");
        }

        // Helper methods
        private bool IsAdminLoggedIn()
        {
            return HttpContext.Session.GetString("IsAdminLoggedIn") == "true";
        }

        private bool VerifyPassword(string password, string hash)
        {
            // Giả sử bạn sử dụng SHA256 hoặc bcrypt
            // Đây là ví dụ đơn giản với SHA256
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var hashedPassword = Convert.ToBase64String(hashedBytes);
                return hashedPassword == hash;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Action để tạo admin đầu tiên (chỉ dùng trong development)
        [HttpGet]
        public IActionResult CreateFirstAdmin()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreateFirstAdminApi()
        {
            try
            {
                // Kiểm tra xem đã có admin chưa
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == "admin");
                if (adminRole == null)
                {
                    adminRole = new Role { RoleName = "Admin" };
                    _context.Roles.Add(adminRole);
                    await _context.SaveChangesAsync();
                }

                var adminExists = await _context.Users
                    .Include(u => u.UserRoles)
                    .AnyAsync(u => u.UserRoles.Any(ur => ur.Role.RoleName.ToLower() == "admin"));

                if (!adminExists)
                {
                    var adminUser = new User
                    {
                        FullName = "Administrator",
                        Email = "admin@jollibee.com",
                        PasswordHash = HashPassword("admin123"), // Mật khẩu mặc định
                        IsActive = true
                    };

                    _context.Users.Add(adminUser);
                    await _context.SaveChangesAsync();

                    // Gán role admin
                    var userRole = new UserRole
                    {
                        UserID = adminUser.UserID,
                        RoleID = adminRole.RoleID
                    };

                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Tạo admin đầu tiên thành công! Email: admin@jollibee.com, Mật khẩu: admin123" });
                }

                return Json(new { success = false, message = "Admin đã tồn tại!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // =======================================================
        // API MỚI CHO TEAM - RESET ADMIN PASSWORD
        // =======================================================

        [HttpGet]
        [Route("Admin/Auth/ResetAdminPassword")]
        public async Task<IActionResult> ResetAdminPassword()
        {
            try
            {
                await SeedAdminData.ResetAdminPasswordAsync(_context);
                
                return Json(new { 
                    success = true, 
                    message = "✅ Admin password đã được reset thành công!",
                    email = "admin@jollibee.com",
                    password = "admin123",
                    note = "Tất cả thành viên team có thể sử dụng tài khoản này"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "❌ Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        [Route("Admin/Auth/SeedTeamData")]
        public async Task<IActionResult> SeedTeamData()
        {
            try
            {
                await SeedAdminData.SeedAsync(_context);
                
                return Json(new { 
                    success = true, 
                    message = "🎉 Seed data hoàn tất! Admin account và dữ liệu mẫu đã sẵn sàng cho cả team!",
                    adminAccount = new {
                        email = "admin@jollibee.com",
                        password = "admin123"
                    },
                    note = "Cả team đều có thể sử dụng tài khoản admin này"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "❌ Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        [Route("Admin/Auth/CheckAdminStatus")]
        public async Task<IActionResult> CheckAdminStatus()
        {
            try
            {
                var adminUser = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == "admin@jollibee.com");

                if (adminUser == null)
                {
                    return Json(new { 
                        success = false, 
                        message = "❌ Admin account chưa tồn tại",
                        suggestion = "Hãy chạy /Admin/Auth/SeedTeamData để tạo admin account"
                    });
                }

                var isAdmin = adminUser.UserRoles.Any(ur => ur.Role.RoleName.ToLower() == "admin");
                
                return Json(new { 
                    success = true,
                    adminExists = true,
                    hasAdminRole = isAdmin,
                    adminInfo = new {
                        email = adminUser.Email,
                        fullName = adminUser.FullName,
                        isActive = adminUser.IsActive
                    },
                    message = isAdmin ? "✅ Admin account hoạt động bình thường" : "⚠️ User tồn tại nhưng chưa có quyền admin"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "❌ Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
} 


