using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Models;
using JollibeeClone.ViewModels;
using JollibeeClone.Attributes;
using JollibeeClone.Services;
using System.Security.Cryptography;
using System.Text;

namespace JollibeeClone.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly OrderStatusHistoryService _statusHistoryService;
        private readonly ICartMergeService _cartMergeService;

        public AccountController(AppDbContext context, ILogger<AccountController> logger, OrderStatusHistoryService statusHistoryService, ICartMergeService cartMergeService)
        {
            _context = context;
            _logger = logger;
            _statusHistoryService = statusHistoryService;
            _cartMergeService = cartMergeService;
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

                // CART MERGE: Merge anonymous cart to new user cart after successful registration
                try
                {
                    var currentSessionId = HttpContext.Session.Id;
                    var cartMergeSuccess = await _cartMergeService.MergeAnonymousCartToUserAsync(newUser.UserID, currentSessionId);
                    if (cartMergeSuccess)
                    {
                        _logger.LogInformation("Cart merge successful for new user: {Email}", model.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Cart merge failed for new user: {Email}", model.Email);
                    }
                }
                catch (Exception cartEx)
                {
                    _logger.LogError(cartEx, "Error during cart merge for new user: {Email}", model.Email);
                    // Don't fail registration if cart merge fails
                }

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
        public IActionResult Login(string returnUrl = null)
        {
            // Nếu đã đăng nhập thì chuyển về trang chủ (hoặc returnUrl nếu có)
            if (IsUserLoggedIn())
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
            
            // Store returnUrl in ViewBag for the login form
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserLoginViewModel model, string returnUrl = null)
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

                // Get current session ID before login to merge cart later
                var currentSessionId = HttpContext.Session.Id;
                _logger.LogInformation("Login - Current Session ID: {SessionId} for user: {Email}", currentSessionId, user.Email);

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

                // CART MERGE: Merge anonymous cart to user cart after successful login
                try
                {
                    var cartMergeSuccess = await _cartMergeService.MergeAnonymousCartToUserAsync(user.UserID, currentSessionId);
                    if (cartMergeSuccess)
                    {
                        _logger.LogInformation("Cart merge successful for user: {Email}", user.Email);
                    }
                    else
                    {
                        _logger.LogWarning("Cart merge failed for user: {Email}", user.Email);
                    }
                }
                catch (Exception cartEx)
                {
                    _logger.LogError(cartEx, "Error during cart merge for user: {Email}", user.Email);
                    // Don't fail login if cart merge fails
                }

                TempData["SuccessMessage"] = "Đăng nhập thành công!";
                _logger.LogInformation("User logged in: {Email}", user.Email);

                // Handle return URL for seamless UX
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    _logger.LogInformation("Redirecting user to return URL: {ReturnUrl}", returnUrl);
                    return Redirect(returnUrl);
                }

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

                // Load default address
                var defaultAddress = await _context.UserAddresses
                    .FirstOrDefaultAsync(ua => ua.UserID == user.UserID && ua.IsDefault);

                var viewModel = new UserProfileViewModel
                {
                    UserID = user.UserID,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Gender = user.Gender,
                    City = user.City,
                    DefaultAddress = defaultAddress
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
        public async Task<IActionResult> EditAddress(AddressViewModel model)
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

        // GET: Account/Promotions - Danh sách ưu đãi của user
        [HttpGet]
        [UserAuthorize]
        public async Task<IActionResult> Promotions()
        {
            try
            {
                var userId = int.Parse(HttpContext.Session.GetString("UserId"));
                
                // Lấy tất cả promotion active
                var availablePromotions = await _context.Promotions
                    .Where(p => p.IsActive && 
                               p.StartDate <= DateTime.Now && 
                               p.EndDate >= DateTime.Now &&
                               (p.MaxUses == null || p.UsesCount < p.MaxUses))
                    .Select(p => new {
                        p.PromotionID,
                        p.PromotionName,
                        p.Description,
                        p.CouponCode,
                        p.DiscountType,
                        p.DiscountValue,
                        MinOrderValue = (decimal?)p.MinOrderValue,
                        p.StartDate,
                        p.EndDate,
                        MaxUses = (int?)p.MaxUses,
                        p.UsesCount,
                        MaxUsesPerUser = (int?)p.MaxUsesPerUser,
                        UserUsageCount = p.UserPromotions.Count(up => up.UserID == userId)
                    })
                    .ToListAsync();

                // Lấy lịch sử sử dụng promotion của user
                var usedPromotions = await _context.UserPromotions
                    .Include(up => up.Promotion)
                    .Include(up => up.Order)
                    .Where(up => up.UserID == userId)
                    .OrderByDescending(up => up.UsedDate)
                    .Select(up => new {
                        up.UserPromotionID,
                        up.UsedDate,
                        up.DiscountAmount,
                        up.OrderID,
                        OrderCode = up.Order != null ? up.Order.OrderCode : null,
                        PromotionName = up.Promotion.PromotionName,
                        CouponCode = up.Promotion.CouponCode,
                        DiscountType = up.Promotion.DiscountType,
                        DiscountValue = up.Promotion.DiscountValue
                    })
                    .ToListAsync();

                // Filter promotions user có thể sử dụng
                var eligiblePromotions = availablePromotions
                    .Where(p => p.MaxUsesPerUser == null || p.UserUsageCount < p.MaxUsesPerUser)
                    .ToList();

                var viewModel = new {
                    AvailablePromotions = eligiblePromotions,
                    UsedPromotions = usedPromotions,
                    TotalAvailable = eligiblePromotions.Count,
                    TotalUsed = usedPromotions.Count
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user promotions for user {UserId}", HttpContext.Session.GetString("UserId"));
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách ưu đãi.";
                return View(new { 
                    AvailablePromotions = new List<object>(), 
                    UsedPromotions = new List<object>(),
                    TotalAvailable = 0,
                    TotalUsed = 0
                });
            }
        }

        // GET: Account/PrintOrderInvoice/{id} - In hóa đơn đơn hàng
        [HttpGet]
        [UserAuthorize]
        public async Task<IActionResult> PrintOrderInvoice(int id)
        {
            try
            {
                var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
                
                if (userId == 0)
                {
                    Console.WriteLine($"❌ User not logged in, redirecting to login");
                    return RedirectToAction("Login", "Account");
                }

                // Load order with all necessary details
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderStatus)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.DeliveryMethod)
                    .Include(o => o.Store)
                    .Include(o => o.UserAddress)
                    .Include(o => o.Promotion)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

                if (order == null)
                {
                    Console.WriteLine($"❌ Order {id} not found for user {userId}");
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("ProfileOrders");
                }

                // Map order items to view model
                var orderItems = new List<UserOrderItemViewModel>();
                foreach (var item in order.OrderItems)
                {
                    var orderItemViewModel = new UserOrderItemViewModel
                    {
                        OrderItemID = item.OrderItemID,
                        ProductID = item.ProductID,
                        ProductName = item.ProductNameSnapshot,
                        ProductImage = item.Product?.ImageUrl,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal,
                        ConfigurationOptions = new List<OrderItemConfigurationViewModel>()
                    };

                    // Parse configuration if exists
                    if (!string.IsNullOrEmpty(item.SelectedConfigurationSnapshot))
                    {
                        try
                        {
                            var configData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(item.SelectedConfigurationSnapshot);
                            if (configData != null)
                            {
                                orderItemViewModel.ConfigurationOptions = configData.Select(config => new OrderItemConfigurationViewModel
                                {
                                    GroupName = (string)config.GroupName,
                                    OptionName = (string)config.OptionProductName,
                                    OptionImage = (string?)config.OptionProductImage,
                                    Quantity = (int)config.Quantity,
                                    PriceAdjustment = (decimal)config.PriceAdjustment,
                                    VariantName = (string?)config.VariantName,
                                    VariantType = (string?)config.VariantType
                                }).ToList();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error parsing order item configuration for OrderItemID: {OrderItemID}", item.OrderItemID);
                        }
                    }

                    orderItems.Add(orderItemViewModel);
                }

                var viewModel = new UserOrderDetailViewModel
                {
                    OrderID = order.OrderID,
                    OrderCode = order.OrderCode,
                    OrderDate = order.OrderDate,
                    CustomerFullName = order.CustomerFullName,
                    CustomerPhoneNumber = order.CustomerPhoneNumber,
                    CustomerEmail = order.CustomerEmail,
                    StatusName = order.OrderStatus.StatusName,
                    StatusDescription = order.OrderStatus.Description ?? "",
                    StatusColor = GetStatusColor(order.OrderStatus.StatusName),
                    StatusIcon = GetStatusIcon(order.OrderStatus.StatusName),
                    DeliveryMethodName = order.DeliveryMethod?.MethodName ?? "",
                    DeliveryAddress = order.UserAddress?.Address,
                    StoreName = order.Store?.StoreName,
                    StoreAddress = order.Store != null ? $"{order.Store.StreetAddress}, {order.Store.District}, {order.Store.City}" : null,
                    PickupDate = order.PickupDate,
                    PickupTimeSlot = order.PickupTimeSlot,
                    PaymentMethodName = order.PaymentMethod.MethodName,
                    SubtotalAmount = order.SubtotalAmount,
                    ShippingFee = order.ShippingFee,
                    DiscountAmount = order.DiscountAmount,
                    TotalAmount = order.TotalAmount,
                    OrderItems = orderItems,
                    NotesByCustomer = order.NotesByCustomer,
                    CanCancel = CanCancelOrder(order.OrderStatus.StatusName),
                    CanReorder = order.OrderStatus.StatusName.ToLower() == "hoàn thành" || order.OrderStatus.StatusName.ToLower() == "đã hủy"
                };

                Console.WriteLine($"✅ Loading printable invoice for order {order.OrderCode}");
                
                return View("PrintInvoice", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading printable invoice for order {OrderId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải hóa đơn. Vui lòng thử lại.";
                return RedirectToAction("ProfileOrders");
            }
        }

        // ...existing code...

        // API: Check authentication status for cart and AJAX calls
        [HttpGet]
        public IActionResult CheckAuthenticationStatus()
        {
            try
            {
                var isAuthenticated = IsUserLoggedIn();
                var userId = HttpContext.Session.GetString("UserId");
                var userName = HttpContext.Session.GetString("UserName");

                return Json(new
                {
                    isAuthenticated = isAuthenticated,
                    userId = isAuthenticated ? userId : null,
                    userName = isAuthenticated ? userName : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authentication status");
                return Json(new { isAuthenticated = false });
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

        // GET: Account/Orders - Danh sách đơn hàng của user
        [HttpGet]
        [UserAuthorize]
        public async Task<IActionResult> Orders(int page = 1, string status = "")
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return RedirectToAction("Login");
                }

                const int pageSize = 10;
                var query = _context.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.DeliveryMethod)
                    .Include(o => o.UserAddress)
                    .Include(o => o.OrderItems)
                    .Where(o => o.UserID == userId);

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.OrderStatus.StatusName.Contains(status));
                }

                // Order by newest first
                query = query.OrderByDescending(o => o.OrderDate);

                var totalItems = await query.CountAsync();
                var orders = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Map to view model
                var orderSummaries = orders.Select(o => new UserOrderSummaryViewModel
                {
                    OrderID = o.OrderID,
                    OrderCode = o.OrderCode,
                    OrderDate = o.OrderDate,
                    StatusName = o.OrderStatus.StatusName,
                    StatusDescription = o.OrderStatus.Description ?? "",
                    TotalAmount = o.TotalAmount,
                    PaymentMethodName = o.PaymentMethod.MethodName,
                    DeliveryMethodName = o.DeliveryMethod?.MethodName ?? "",
                    DeliveryAddress = o.UserAddress?.Address,
                    TotalItems = o.OrderItems.Sum(oi => oi.Quantity),
                    StatusColor = GetStatusColor(o.OrderStatus.StatusName),
                    StatusIcon = GetStatusIcon(o.OrderStatus.StatusName),
                    CanCancel = CanCancelOrder(o.OrderStatus.StatusName),
                    CanReorder = o.OrderStatus.StatusName == "Hoàn thành" || o.OrderStatus.StatusName == "Đã hủy"
                }).ToList();

                // Get total orders count (without status filter)
                var totalOrdersCount = await _context.Orders
                    .Where(o => o.UserID == userId)
                    .CountAsync();

                var viewModel = new UserOrderListViewModel
                {
                    Orders = orderSummaries,
                    CurrentPage = page,
                    PageIndex = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                    TotalOrdersCount = totalOrdersCount,
                    StatusFilter = status,
                    CurrentStatusFilter = status
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user orders for UserId: {UserId}", HttpContext.Session.GetString("UserId"));
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đơn hàng.";
                return View(new UserOrderListViewModel());
            }
        }

        // GET: Account/OrderDetail/{id} - Chi tiết đơn hàng
        [HttpGet]
        [UserAuthorize]
        public async Task<IActionResult> OrderDetail(int id)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var order = await _context.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.DeliveryMethod)
                    .Include(o => o.Store)
                    .Include(o => o.UserAddress)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                    return RedirectToAction("Orders");
                }

                // Map order items
                var orderItems = new List<UserOrderItemViewModel>();
                foreach (var item in order.OrderItems)
                {
                    var orderItemViewModel = new UserOrderItemViewModel
                    {
                        OrderItemID = item.OrderItemID,
                        ProductID = item.ProductID,
                        ProductName = item.ProductNameSnapshot,
                        ProductImage = item.Product?.ImageUrl,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        Subtotal = item.Subtotal,
                        ConfigurationOptions = new List<OrderItemConfigurationViewModel>()
                    };

                    // Parse configuration if exists
                    if (!string.IsNullOrEmpty(item.SelectedConfigurationSnapshot))
                    {
                        try
                        {
                            var configData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(item.SelectedConfigurationSnapshot);
                            if (configData != null)
                            {
                                orderItemViewModel.ConfigurationOptions = configData.Select(config => new OrderItemConfigurationViewModel
                                {
                                    GroupName = (string)config.GroupName,
                                    OptionName = (string)config.OptionProductName,
                                    OptionImage = (string?)config.OptionProductImage,
                                    Quantity = (int)config.Quantity,
                                    PriceAdjustment = (decimal)config.PriceAdjustment,
                                    VariantName = (string?)config.VariantName,
                                    VariantType = (string?)config.VariantType
                                }).ToList();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error parsing order item configuration for OrderItemID: {OrderItemID}", item.OrderItemID);
                        }
                    }

                    orderItems.Add(orderItemViewModel);
                }

                // Create tracking events
                var trackingEvents = await CreateTrackingEventsAsync(order);

                var viewModel = new UserOrderDetailViewModel
                {
                    OrderID = order.OrderID,
                    OrderCode = order.OrderCode,
                    OrderDate = order.OrderDate,
                    CustomerFullName = order.CustomerFullName,
                    CustomerPhoneNumber = order.CustomerPhoneNumber,
                    CustomerEmail = order.CustomerEmail,
                    StatusName = order.OrderStatus.StatusName,
                    StatusDescription = order.OrderStatus.Description ?? "",
                    StatusColor = GetStatusColor(order.OrderStatus.StatusName),
                    StatusIcon = GetStatusIcon(order.OrderStatus.StatusName),
                    DeliveryMethodName = order.DeliveryMethod?.MethodName ?? "",
                    DeliveryAddress = order.UserAddress?.Address,
                    StoreName = order.Store?.StoreName,
                    StoreAddress = order.Store != null ? $"{order.Store.StreetAddress}, {order.Store.District}, {order.Store.City}" : null,
                    PickupDate = order.PickupDate,
                    PickupTimeSlot = order.PickupTimeSlot,
                    PaymentMethodName = order.PaymentMethod.MethodName,
                    SubtotalAmount = order.SubtotalAmount,
                    ShippingFee = order.ShippingFee,
                    DiscountAmount = order.DiscountAmount,
                    TotalAmount = order.TotalAmount,
                    OrderItems = orderItems,
                    NotesByCustomer = order.NotesByCustomer,
                    TrackingEvents = trackingEvents,
                    CanCancel = CanCancelOrder(order.OrderStatus.StatusName),
                    CanReorder = order.OrderStatus.StatusName == "Hoàn thành" || order.OrderStatus.StatusName == "Đã hủy"
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order detail for OrderID: {OrderID}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải chi tiết đơn hàng.";
                return RedirectToAction("Orders");
            }
        }

        // GET: /Account/ProfileOrders - Display orders within profile layout
        [HttpGet]
        [UserAuthorize]
        public async Task<IActionResult> ProfileOrders(string? status = null, int page = 1)
        {
            try
            {
                var isUserLoggedIn = HttpContext.Session.GetString("IsUserLoggedIn") == "true";
                var userIdFromSession = HttpContext.Session.GetString("UserId");

                if (!isUserLoggedIn || string.IsNullOrEmpty(userIdFromSession) || !int.TryParse(userIdFromSession, out int userId))
                {
                    return RedirectToAction("Login");
                }

                Console.WriteLine($"🔍 ProfileOrders called - UserID: {userId}, Status: {status}, Page: {page}");

                const int pageSize = 5; // Smaller page size for profile view

                // Build query with includes
                var query = _context.Orders
                    .Include(o => o.OrderStatus)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.DeliveryMethod)
                    .Include(o => o.UserAddress)
                    .Include(o => o.OrderItems)
                    .Where(o => o.UserID == userId);

                // Apply status filter
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    query = query.Where(o => o.OrderStatus.StatusName.ToLower().Contains(status.ToLower()));
                    Console.WriteLine($"🔍 Applied status filter: {status}");
                }

                // Get total count for pagination
                var totalItems = await query.CountAsync();

                // Get paginated orders
                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Console.WriteLine($"🔍 Found {orders.Count} orders for page {page}");

                // Map to view model
                var orderSummaries = orders.Select(o => new UserOrderSummaryViewModel
                {
                    OrderID = o.OrderID,
                    OrderCode = o.OrderCode,
                    OrderDate = o.OrderDate,
                    StatusName = o.OrderStatus.StatusName,
                    StatusDescription = o.OrderStatus.Description ?? "",
                    TotalAmount = o.TotalAmount,
                    PaymentMethodName = o.PaymentMethod.MethodName,
                    DeliveryMethodName = o.DeliveryMethod?.MethodName ?? "",
                    DeliveryAddress = o.UserAddress?.Address ?? o.CustomerFullName, // Fallback to customer name if no address
                    TotalItems = o.OrderItems.Sum(oi => oi.Quantity),
                    StatusColor = GetStatusColor(o.OrderStatus.StatusName),
                    StatusIcon = GetStatusIcon(o.OrderStatus.StatusName),
                    CanCancel = CanCancelOrder(o.OrderStatus.StatusName),
                    CanReorder = o.OrderStatus.StatusName.ToLower() == "hoàn thành" || o.OrderStatus.StatusName.ToLower() == "đã hủy"
                }).ToList();

                // Get total orders count (without status filter)
                var totalOrdersCount = await _context.Orders
                    .Where(o => o.UserID == userId)
                    .CountAsync();

                var viewModel = new UserOrderListViewModel
                {
                    Orders = orderSummaries,
                    CurrentPage = page,
                    PageIndex = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                    TotalOrdersCount = totalOrdersCount,
                    StatusFilter = status,
                    CurrentStatusFilter = status ?? "all"
                };

                Console.WriteLine($"🔍 ProfileOrders ViewModel created - Total: {totalItems}, Page: {page}/{viewModel.TotalPages}");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ProfileOrders: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách đơn hàng.";
                return RedirectToAction("Profile");
            }
        }

        // Helper methods for order status styling
        private string GetStatusColor(string statusName)
        {
            return statusName.ToLower() switch
            {
                "chờ xác nhận" => "#ffc107", // warning yellow
                "đã xác nhận" => "#17a2b8",  // info blue  
                "đang chuẩn bị" => "#fd7e14", // orange
                "đang giao hàng" => "#28a745", // success green
                "sẵn sàng lấy hàng" => "#20c997", // teal
                "hoàn thành" => "#28a745", // success green
                "đã hủy" => "#dc3545", // danger red
                _ => "#6c757d" // secondary gray
            };
        }

        private string GetStatusIcon(string statusName)
        {
            return statusName.ToLower() switch
            {
                "chờ xác nhận" => "fas fa-clock",
                "đã xác nhận" => "fas fa-check-circle",
                "đang chuẩn bị" => "fas fa-utensils",
                "đang giao hàng" => "fas fa-shipping-fast",
                "sẵn sàng lấy hàng" => "fas fa-store",
                "hoàn thành" => "fas fa-check-double",
                "đã hủy" => "fas fa-times-circle",
                _ => "fas fa-question-circle"
            };
        }

        private bool CanCancelOrder(string statusName)
        {
            // Chỉ cho phép hủy đơn hàng khi ở trạng thái "chờ xác nhận"
            // Một khi admin đã xác nhận đơn hàng thì không thể hủy nữa
            return statusName.ToLower() == "chờ xác nhận";
        }

        private async Task<List<OrderTrackingEvent>> CreateTrackingEventsAsync(Orders order)
        {
            var events = new List<OrderTrackingEvent>();
            var currentStatus = order.OrderStatus.StatusName.ToLower();
            var currentStatusId = order.OrderStatusID;

            // Lấy lịch sử trạng thái thực tế từ database
            var statusHistory = await _statusHistoryService.GetOrderStatusHistoryAsync(order.OrderID);
            var statusTimes = statusHistory.ToDictionary(h => h.OrderStatusID, h => h.UpdatedAt);

            // Đặt hàng thành công - LUÔN HIỂN THỊ
            var orderCreatedTime = statusTimes.ContainsKey(1) ? statusTimes[1] : order.OrderDate;
            events.Add(new OrderTrackingEvent
            {
                EventDate = orderCreatedTime,
                EventTitle = "Đặt hàng thành công",
                EventDescription = $"Đơn hàng #{order.OrderCode} đã được tạo",
                EventIcon = "fas fa-shopping-cart",
                EventColor = "#28a745",
                IsCompleted = true
            });

            // Kiểm tra nếu đơn hàng đã bị hủy
            if (currentStatus == "đã hủy")
            {
                // Kiểm tra xem có lịch sử xác nhận không
                var wasConfirmed = statusTimes.ContainsKey(2);
                
                if (wasConfirmed)
                {
                    events.Add(new OrderTrackingEvent
                    {
                        EventDate = statusTimes[2],
                        EventTitle = "Đã xác nhận",
                        EventDescription = "Đơn hàng đã được xác nhận bởi cửa hàng",
                        EventIcon = "fas fa-check-circle",
                        EventColor = "#28a745",
                        IsCompleted = true
                    });
                }

                // Hiển thị trạng thái đã hủy với thời gian thực tế
                var cancelledTime = statusTimes.ContainsKey(7) ? statusTimes[7] : DateTime.Now;
                events.Add(new OrderTrackingEvent
                {
                    EventDate = cancelledTime,
                    EventTitle = "Đã hủy",
                    EventDescription = wasConfirmed ? 
                        "Đơn hàng đã bị hủy" : 
                        "Đơn hàng đã bị hủy trước khi được xác nhận",
                    EventIcon = "fas fa-times-circle",
                    EventColor = "#dc3545",
                    IsCompleted = true
                });

                return events; // Dừng lại, không hiển thị các trạng thái khác
            }

            // Logic bình thường cho đơn hàng chưa hủy
            // Xác nhận đơn hàng
            var isConfirmed = statusTimes.ContainsKey(2);
            events.Add(new OrderTrackingEvent
            {
                EventDate = isConfirmed ? statusTimes[2] : DateTime.MinValue,
                EventTitle = "Đã xác nhận",
                EventDescription = isConfirmed ? "Đơn hàng đã được xác nhận bởi cửa hàng" : "Chờ xác nhận từ cửa hàng",
                EventIcon = "fas fa-check-circle",
                EventColor = isConfirmed ? "#28a745" : "#6c757d",
                IsCompleted = isConfirmed
            });

            // Chuẩn bị đơn hàng
            var isPreparing = statusTimes.ContainsKey(3);
            events.Add(new OrderTrackingEvent
            {
                EventDate = isPreparing ? statusTimes[3] : DateTime.MinValue,
                EventTitle = "Đang chuẩn bị",
                EventDescription = isPreparing ? "Đơn hàng đang được chuẩn bị" : "Chưa bắt đầu chuẩn bị",
                EventIcon = "fas fa-utensils",
                EventColor = isPreparing ? "#28a745" : "#6c757d",
                IsCompleted = isPreparing
            });

            // Xác định loại giao hàng dựa trên DeliveryMethodID
            var isDelivery = order.DeliveryMethodID == 1; // ID 1 = "Giao hàng tận nơi"
            var isPickup = order.DeliveryMethodID == 2;   // ID 2 = "Hẹn lấy tại cửa hàng"

            if (isDelivery)
            {
                // Logic cho giao hàng tận nơi
                var isDelivering = statusTimes.ContainsKey(4);
                events.Add(new OrderTrackingEvent
                {
                    EventDate = isDelivering ? statusTimes[4] : DateTime.MinValue,
                    EventTitle = "Đang giao hàng",
                    EventDescription = isDelivering ? "Đơn hàng đang được giao đến địa chỉ của bạn" : "Chưa bắt đầu giao hàng",
                    EventIcon = "fas fa-shipping-fast",
                    EventColor = isDelivering ? "#28a745" : "#6c757d",
                    IsCompleted = isDelivering
                });
            }
            else if (isPickup)
            {
                // Logic cho lấy tại cửa hàng
                var isReadyForPickup = statusTimes.ContainsKey(5);
                events.Add(new OrderTrackingEvent
                {
                    EventDate = isReadyForPickup ? statusTimes[5] : DateTime.MinValue,
                    EventTitle = "Sẵn sàng lấy hàng",
                    EventDescription = isReadyForPickup ? "Đơn hàng đã sẵn sàng để bạn đến lấy tại cửa hàng" : "Đang chuẩn bị để lấy",
                    EventIcon = "fas fa-store",
                    EventColor = isReadyForPickup ? "#28a745" : "#6c757d",
                    IsCompleted = isReadyForPickup
                });
            }

            // Hoàn thành đơn hàng - LUÔN CUỐI CÙNG
            var isCompleted = statusTimes.ContainsKey(6) && currentStatus == "hoàn thành";
            events.Add(new OrderTrackingEvent
            {
                EventDate = isCompleted ? statusTimes[6] : DateTime.MinValue,
                EventTitle = "Hoàn thành",
                EventDescription = isCompleted ? 
                    (isDelivery ? "Đơn hàng đã được giao thành công" : "Đơn hàng đã được lấy thành công") : 
                    "Chưa hoàn thành",
                EventIcon = "fas fa-check-double",
                EventColor = isCompleted ? "#28a745" : "#6c757d",
                IsCompleted = isCompleted
            });

            return events;
        }



        // POST: Cancel Order
        [HttpPost]
        [UserAuthorize]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderStatus)
                    .FirstOrDefaultAsync(o => o.OrderID == id);

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
                }

                // Check if user has permission to cancel this order
                var userIdFromSession = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdFromSession) || !int.TryParse(userIdFromSession, out int userId) || order.UserID != userId)
                {
                    return Json(new { success = false, message = "Bạn không có quyền hủy đơn hàng này." });
                }

                // Check if order can be cancelled
                if (!CanCancelOrder(order.OrderStatus.StatusName))
                {
                    return Json(new { success = false, message = "Đơn hàng này không thể hủy được vì đã được cửa hàng xác nhận. Chỉ có thể hủy đơn hàng khi đang ở trạng thái 'Chờ xác nhận'." });
                }

                // Find "Đã hủy" status
                var cancelledStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(s => s.StatusName == "Đã hủy");

                if (cancelledStatus == null)
                {
                    return Json(new { success = false, message = "Không thể hủy đơn hàng. Vui lòng liên hệ hỗ trợ." });
                }

                // Update order status to cancelled
                order.OrderStatusID = cancelledStatus.OrderStatusID;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                // Ghi lại lịch sử thay đổi trạng thái
                await _statusHistoryService.LogStatusChangeAsync(
                    orderId: order.OrderID,
                    statusId: cancelledStatus.OrderStatusID,
                    updatedBy: $"User-{userId}",
                    note: "Đơn hàng được hủy bởi khách hàng"
                );

                _logger.LogInformation($"Order {order.OrderCode} has been cancelled by user {userId}");

                return Json(new { success = true, message = "Đơn hàng đã được hủy thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderID}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi hủy đơn hàng." });
            }
        }

        // API: Get Order Status (for real-time updates)
        [HttpGet]
        [UserAuthorize]
        public async Task<IActionResult> GetOrderStatus(int orderId)
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var order = await _context.Orders
                    .Include(o => o.OrderStatus)
                    .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId);

                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                // Create tracking events for the current status
                var trackingEvents = await CreateTrackingEventsAsync(order);

                return Json(new { 
                    success = true, 
                    data = new {
                        statusName = order.OrderStatus.StatusName,
                        statusDescription = order.OrderStatus.Description ?? "",
                        statusColor = GetStatusColor(order.OrderStatus.StatusName),
                        statusIcon = GetStatusIcon(order.OrderStatus.StatusName),
                        trackingEvents = trackingEvents,
                        canCancel = CanCancelOrder(order.OrderStatus.StatusName),
                        canReorder = order.OrderStatus.StatusName == "Hoàn thành" || order.OrderStatus.StatusName == "Đã hủy"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status for OrderID: {OrderID}", orderId);
                return Json(new { success = false, message = "Internal server error" });
            }
        }
    }
}