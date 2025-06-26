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

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            // Nếu đã đăng nhập thì chuyển về trang chủ
            if (IsUserLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
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

                // Tìm user theo số điện thoại
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber && u.IsActive);

                if (user == null)
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này chưa được đăng ký trong hệ thống.");
                    return View(model);
                }

                // Chuyển hướng đến trang đặt lại mật khẩu
                TempData["PhoneNumber"] = model.PhoneNumber;
                TempData["SuccessMessage"] = "Số điện thoại hợp lệ! Vui lòng đặt mật khẩu mới.";
                return RedirectToAction("ResetPassword");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forgot password process: {PhoneNumber}", model.PhoneNumber);
                ModelState.AddModelError("", "Có lỗi xảy ra. Vui lòng thử lại.");
                return View(model);
            }
        }

        // GET: Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword()
        {
            // Kiểm tra có số điện thoại từ TempData không
            if (TempData["PhoneNumber"] == null)
            {
                TempData["ErrorMessage"] = "Phiên làm việc đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                PhoneNumber = TempData["PhoneNumber"]?.ToString() ?? ""
            };

            // Giữ lại PhoneNumber cho POST request
            TempData.Keep("PhoneNumber");

            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            try
            {
                // Lấy số điện thoại từ TempData
                var phoneNumber = TempData["PhoneNumber"]?.ToString();
                if (string.IsNullOrEmpty(phoneNumber))
                {
                    TempData["ErrorMessage"] = "Phiên làm việc đã hết hạn. Vui lòng thử lại.";
                    return RedirectToAction("ForgotPassword");
                }

                model.PhoneNumber = phoneNumber;

                if (!ModelState.IsValid)
                {
                    TempData.Keep("PhoneNumber");
                    return View(model);
                }

                // Tìm user theo số điện thoại
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && u.IsActive);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản. Vui lòng thử lại.";
                    return RedirectToAction("ForgotPassword");
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = HashPassword(model.NewPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset successfully for user: {PhoneNumber}", phoneNumber);
                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.";
                
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset: {PhoneNumber}", model.PhoneNumber);
                TempData.Keep("PhoneNumber");
                ModelState.AddModelError("", "Có lỗi xảy ra khi đặt lại mật khẩu. Vui lòng thử lại.");
                return View(model);
            }
        }

        // GET: Account/Profile
        [HttpGet]
        [UserAuthorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login");
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserID == int.Parse(userId));

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin tài khoản.";
                    return RedirectToAction("Login");
                }

                var viewModel = new UserProfileViewModel
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Gender = user.Gender,
                    City = user.City
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin tài khoản.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId) || model.UserID != int.Parse(userId))
                {
                    return RedirectToAction("Login");
                }

                var user = await _context.Users.FindAsync(model.UserID);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin tài khoản.";
                    return RedirectToAction("Login");
                }

                // Validate email unique (except current user)
                if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserID != model.UserID))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                }

                // Validate phone unique (except current user)
                if (!string.IsNullOrEmpty(model.PhoneNumber) && 
                    await _context.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber && u.UserID != model.UserID))
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại này đã được sử dụng bởi tài khoản khác.");
                }

                if (ModelState.IsValid)
                {
                    // Update user information
                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Gender = model.Gender;
                    user.City = model.City;

                    await _context.SaveChangesAsync();

                    // Update session data
                    HttpContext.Session.SetString("UserName", user.FullName);
                    HttpContext.Session.SetString("UserEmail", user.Email);

                    TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
                    return RedirectToAction("Profile");
                }

                return View("Profile", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thông tin tài khoản.";
                return View("Profile", model);
            }
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn." });
                }

                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin tài khoản." });
                }

                // Verify current password
                if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    return Json(new { success = false, message = "Mật khẩu hiện tại không chính xác." });
                }

                // Update password
                user.PasswordHash = HashPassword(model.NewPassword);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                var userId = HttpContext.Session.GetString("UserId");
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi đổi mật khẩu." });
            }
        }

        // GET: Account/DeliveryAddresses
        [HttpGet]
        [UserAuthorize]
        public async Task<IActionResult> DeliveryAddresses()
        {
            try
            {
                var userId = int.Parse(HttpContext.Session.GetString("UserId"));
                var addresses = await _context.UserAddresses
                    .Where(ua => ua.UserID == userId)
                    .OrderByDescending(ua => ua.IsDefault)
                    .ThenBy(ua => ua.AddressID)
                    .ToListAsync();

                var user = await _context.Users.FindAsync(userId);
                ViewBag.UserName = user?.FullName ?? "";

                return View(addresses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delivery addresses for user {UserId}", HttpContext.Session.GetString("UserId"));
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách địa chỉ.";
                return RedirectToAction("Profile");
            }
        }

        // GET: Account/EditAddress/{id?}
        [HttpGet]
        [UserAuthorize]
        public async Task<IActionResult> EditAddress(int? id)
        {
            try
            {
                var userId = int.Parse(HttpContext.Session.GetString("UserId"));
                
                if (id.HasValue)
                {
                    // Chỉnh sửa địa chỉ có sẵn
                    var address = await _context.UserAddresses
                        .FirstOrDefaultAsync(ua => ua.AddressID == id.Value && ua.UserID == userId);
                    
                    if (address == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy địa chỉ này.";
                        return RedirectToAction("DeliveryAddresses");
                    }

                    var viewModel = new AddressViewModel
                    {
                        AddressID = address.AddressID,
                        UserID = address.UserID,
                        FullName = address.FullName,
                        PhoneNumber = address.PhoneNumber,
                        Address = address.Address,
                        Note = address.Note,
                        IsDefault = address.IsDefault
                    };

                    return View(viewModel);
                }
                else
                {
                    // Tạo địa chỉ mới
                    var user = await _context.Users.FindAsync(userId);
                    var viewModel = new AddressViewModel
                    {
                        UserID = userId,
                        FullName = user?.FullName ?? "",
                        PhoneNumber = user?.PhoneNumber ?? "",
                        IsDefault = false
                    };

                    return View(viewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit address page for user {UserId}", HttpContext.Session.GetString("UserId"));
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa địa chỉ.";
                return RedirectToAction("DeliveryAddresses");
            }
        }

        // POST: Account/EditAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public async Task<IActionResult> EditAddress(UserAddress model)
        {
            try
            {
                var userId = int.Parse(HttpContext.Session.GetString("UserId"));
                model.UserID = userId;

                if (!ModelState.IsValid)
                {
                    // Debug validation errors
                    var errors = new List<string>();
                    foreach (var kvp in ModelState)
                    {
                        foreach (var error in kvp.Value.Errors)
                        {
                            var errorMsg = $"{kvp.Key}: {error.ErrorMessage}";
                            errors.Add(errorMsg);
                            _logger.LogError("Validation error: {Error}", errorMsg);
                        }
                    }
                    TempData["ErrorMessage"] = $"Lỗi validation: {string.Join("; ", errors)}";
                    return View(model);
                }

                if (model.AddressID == 0)
                {
                    // Tạo địa chỉ mới - không cần navigation property User
                    var newAddress = new UserAddress
                    {
                        UserID = userId,
                        FullName = model.FullName,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address,
                        Note = model.Note,
                        IsDefault = model.IsDefault
                    };
                    
                    _logger.LogInformation("Adding new address for user {UserId}: {FullName}, {Address}", userId, newAddress.FullName, newAddress.Address);
                    _context.UserAddresses.Add(newAddress);
                    TempData["SuccessMessage"] = "Thêm địa chỉ giao hàng thành công!";
                }
                else
                {
                    // Cập nhật địa chỉ có sẵn
                    var existingAddress = await _context.UserAddresses
                        .FirstOrDefaultAsync(ua => ua.AddressID == model.AddressID && ua.UserID == userId);
                    
                    if (existingAddress == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy địa chỉ này.";
                        return RedirectToAction("DeliveryAddresses");
                    }

                    existingAddress.FullName = model.FullName;
                    existingAddress.PhoneNumber = model.PhoneNumber;
                    existingAddress.Address = model.Address;
                    existingAddress.Note = model.Note;
                    existingAddress.IsDefault = model.IsDefault;

                    _context.UserAddresses.Update(existingAddress);
                    TempData["SuccessMessage"] = "Cập nhật địa chỉ giao hàng thành công!";
                }

                // Nếu đây là địa chỉ mặc định, bỏ mặc định của các địa chỉ khác
                if (model.IsDefault)
                {
                    var otherAddresses = await _context.UserAddresses
                        .Where(ua => ua.UserID == userId && ua.AddressID != model.AddressID)
                        .ToListAsync();
                    
                    foreach (var addr in otherAddresses)
                    {
                        addr.IsDefault = false;
                    }
                    _context.UserAddresses.UpdateRange(otherAddresses);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Address saved successfully for user {UserId}", userId);
                return RedirectToAction("DeliveryAddresses");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving address for user {UserId}", HttpContext.Session.GetString("UserId"));
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu địa chỉ. Vui lòng thử lại.";
                return View(model);
            }
        }

        // POST: Account/DeleteAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            try
            {
                var userId = int.Parse(HttpContext.Session.GetString("UserId"));
                var address = await _context.UserAddresses
                    .FirstOrDefaultAsync(ua => ua.AddressID == id && ua.UserID == userId);

                if (address == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy địa chỉ này.";
                    return RedirectToAction("DeliveryAddresses");
                }

                // Kiểm tra xem địa chỉ có đang được sử dụng trong đơn hàng không
                var hasOrders = await _context.Orders.AnyAsync(o => o.UserAddressID == id);
                if (hasOrders)
                {
                    TempData["ErrorMessage"] = "Không thể xóa địa chỉ này vì đã có đơn hàng sử dụng.";
                    return RedirectToAction("DeliveryAddresses");
                }

                _context.UserAddresses.Remove(address);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa địa chỉ thành công!";
                return RedirectToAction("DeliveryAddresses");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address {AddressId} for user {UserId}", id, HttpContext.Session.GetString("UserId"));
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa địa chỉ. Vui lòng thử lại.";
                return RedirectToAction("DeliveryAddresses");
            }
        }

        // POST: Account/SetDefaultAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        [UserAuthorize]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            try
            {
                var userId = int.Parse(HttpContext.Session.GetString("UserId"));
                
                // Bỏ mặc định tất cả địa chỉ của user
                var allAddresses = await _context.UserAddresses
                    .Where(ua => ua.UserID == userId)
                    .ToListAsync();

                foreach (var addr in allAddresses)
                {
                    addr.IsDefault = (addr.AddressID == id);
                }

                _context.UserAddresses.UpdateRange(allAddresses);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã đặt làm địa chỉ mặc định!";
                return RedirectToAction("DeliveryAddresses");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default address {AddressId} for user {UserId}", id, HttpContext.Session.GetString("UserId"));
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đặt địa chỉ mặc định.";
                return RedirectToAction("DeliveryAddresses");
            }
        }

        // TEST: Debug action to check database connection
        [HttpGet]
        [UserAuthorize]
        public async Task<IActionResult> TestAddress()
        {
            try
            {
                var userId = int.Parse(HttpContext.Session.GetString("UserId"));
                
                // Test creating a simple address
                var testAddress = new UserAddress
                {
                    UserID = userId,
                    FullName = "Test User",
                    PhoneNumber = "0123456789",
                    Address = "Test Address",
                    IsDefault = false
                };

                _context.UserAddresses.Add(testAddress);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Test address created successfully", addressId = testAddress.AddressID });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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

        // GET: Account/EditProfile
        [UserAuthorize]
        public async Task<IActionResult> EditProfile()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                UserID = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? "",
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                City = user.City
            };

            return View(model);
        }

        // POST: Account/EditProfile
        [HttpPost]
        [UserAuthorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin cơ bản
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;
            user.City = model.City;

            // Xử lý đổi mật khẩu nếu có
            if (model.ChangePassword && !string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
            {
                // Verify current password
                if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                    return View(model);
                }

                // Update password
                user.PasswordHash = HashPassword(model.NewPassword);
            }

            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();

                if (model.ChangePassword && !string.IsNullOrEmpty(model.NewPassword))
                {
                    // Clear session and redirect to login if password changed
                    HttpContext.Session.Clear();
                    Response.Cookies.Delete("UserRememberMe");
                    TempData["SuccessMessage"] = "Thông tin và mật khẩu đã được cập nhật thành công! Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login");
                }
                else
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Profile");
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thông tin.";
                return View(model);
            }
        }
    }
} 