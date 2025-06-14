using JollibeeClone.Data;
using JollibeeClone.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Areas.Admin.Attributes;
using System.Security.Cryptography;
using System.Text;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/User
        [HttpGet]
        [Route("Admin/User")]
        [Route("Admin/User/Index")]
        public async Task<IActionResult> Index(string searchString, bool? isActive, string sortOrder, int? page)
        {
            try
            {
                // Pagination settings
                int pageSize = 15; // 15 users per page
                int pageNumber = page ?? 1;

                var query = _context.Users.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(u => u.FullName.Contains(searchString) || 
                                           u.Email.Contains(searchString) ||
                                           (u.PhoneNumber != null && u.PhoneNumber.Contains(searchString)));
                }

                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "name_desc":
                        query = query.OrderByDescending(u => u.FullName);
                        break;
                    case "email":
                        query = query.OrderBy(u => u.Email);
                        break;
                    case "email_desc":
                        query = query.OrderByDescending(u => u.Email);
                        break;
                    case "status":
                        query = query.OrderBy(u => u.IsActive).ThenBy(u => u.FullName);
                        break;
                    case "status_desc":
                        query = query.OrderByDescending(u => u.IsActive).ThenBy(u => u.FullName);
                        break;
                    default:
                        query = query.OrderBy(u => u.FullName);
                        break;
                }

                // Create paginated list
                var paginatedUsers = await PaginatedList<User>.CreateAsync(query, pageNumber, pageSize);

                // ViewBag for filters and pagination
                ViewBag.CurrentSearch = searchString;
                ViewBag.CurrentActive = isActive;
                ViewBag.CurrentSort = sortOrder;

                // Check if we need to create sample data
                if (!await _context.Users.AnyAsync())
                {
                    await CreateSampleUsersAsync();
                    // Reload data after creating sample users
                    var refreshedQuery = _context.Users.AsQueryable();
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        refreshedQuery = refreshedQuery.Where(u => u.FullName.Contains(searchString) || 
                                                                   u.Email.Contains(searchString) ||
                                                                   (u.PhoneNumber != null && u.PhoneNumber.Contains(searchString)));
                    }
                    if (isActive.HasValue)
                    {
                        refreshedQuery = refreshedQuery.Where(u => u.IsActive == isActive.Value);
                    }
                    paginatedUsers = await PaginatedList<User>.CreateAsync(refreshedQuery, pageNumber, pageSize);
                }

                return View(paginatedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users");
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i danh sÃ¡ch ngÆ°á»i dÃ¹ng.";
                return View(new PaginatedList<User>(new List<User>(), 0, 1, 15));
            }
        }

        // GET: Admin/User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) 
            {
                TempData["ErrorMessage"] = "ID ngÆ°á»i dÃ¹ng khÃ´ng há»£p lá»‡.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _context.Users
                    .Include(u => u.Orders)
                    .Include(u => u.UserAddresses)
                    .FirstOrDefaultAsync(m => m.UserID == id);

                if (user == null) 
                {
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng.";
                    return RedirectToAction(nameof(Index));
                }

                // Count orders for this user
                ViewBag.OrderCount = user.Orders.Count;
                ViewBag.ActiveOrderCount = user.Orders.Count(o => o.OrderStatusID != 4); // Assuming 4 is cancelled status

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user details for ID: {UserId}", id);
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i thÃ´ng tin ngÆ°á»i dÃ¹ng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Email,PhoneNumber,IsActive")] User user, string password)
        {
            ModelState.Remove("PasswordHash");
            ModelState.Remove("UserRoles");
            ModelState.Remove("UserAddresses");
            ModelState.Remove("Carts");
            ModelState.Remove("Orders");

            try
            {
                // Validate email uniqueness
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    ModelState.AddModelError("Email", "Email nÃ y Ä‘Ã£ tá»“n táº¡i trong há»‡ thá»‘ng.");
                }

                // Validate password
                if (string.IsNullOrWhiteSpace(password))
                {
                    ModelState.AddModelError("Password", "Máº­t kháº©u lÃ  báº¯t buá»™c.");
                }
                else if (password.Length < 6)
                {
                    ModelState.AddModelError("Password", "Máº­t kháº©u pháº£i cÃ³ Ã­t nháº¥t 6 kÃ½ tá»±.");
                }

                if (ModelState.IsValid)
                {
                    // Hash password
                    user.PasswordHash = HashPassword(password);
                    
                    _context.Add(user);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Táº¡o ngÆ°á»i dÃ¹ng thÃ nh cÃ´ng!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº¡o ngÆ°á»i dÃ¹ng.";
            }

            return View(user);
        }

        // GET: Admin/User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID ngÆ°á»i dÃ¹ng khÃ´ng há»£p lá»‡.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng.";
                    return RedirectToAction(nameof(Index));
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user for edit, ID: {UserId}", id);
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i thÃ´ng tin ngÆ°á»i dÃ¹ng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserID,FullName,Email,PhoneNumber,IsActive")] User user, string? newPassword)
        {
            if (id != user.UserID)
            {
                TempData["ErrorMessage"] = "Dá»¯ liá»‡u khÃ´ng há»£p lá»‡.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("PasswordHash");
            ModelState.Remove("UserRoles");
            ModelState.Remove("UserAddresses");
            ModelState.Remove("Carts");
            ModelState.Remove("Orders");

            try
            {
                // Get current user to preserve password if not changing
                var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == id);
                if (currentUser == null)
                {
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate email uniqueness (excluding current user)
                if (await _context.Users.AnyAsync(u => u.Email == user.Email && u.UserID != id))
                {
                    ModelState.AddModelError("Email", "Email nÃ y Ä‘Ã£ tá»“n táº¡i trong há»‡ thá»‘ng.");
                }

                // Validate new password if provided
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    if (newPassword.Length < 6)
                    {
                        ModelState.AddModelError("NewPassword", "Máº­t kháº©u pháº£i cÃ³ Ã­t nháº¥t 6 kÃ½ tá»±.");
                    }
                    else
                    {
                        user.PasswordHash = HashPassword(newPassword);
                    }
                }
                else
                {
                    // Keep current password
                    user.PasswordHash = currentUser.PasswordHash;
                }

                if (ModelState.IsValid)
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating user ID: {UserId}", id);
                if (!await UserExistsAsync(user.UserID))
                {
                    TempData["ErrorMessage"] = "Người dùng không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật. Vui lòng thử lại.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user ID: {UserId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật người dùng.";
            }

            return View(user);
        }

        // GET: Admin/User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID ngÆ°á»i dÃ¹ng khÃ´ng há»£p lá»‡.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var user = await _context.Users
                    .Include(u => u.Orders)
                    .FirstOrDefaultAsync(m => m.UserID == id);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if user has orders
                ViewBag.HasOrders = user.Orders.Any();
                ViewBag.OrderCount = user.Orders.Count;

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user for delete, ID: {UserId}", id);
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i thÃ´ng tin ngÆ°á»i dÃ¹ng.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Orders)
                    .FirstOrDefaultAsync(u => u.UserID == id);

                if (user != null)
                {
                    // Check if user has orders - if yes, only deactivate
                    if (user.Orders.Any())
                    {
                        user.IsActive = false;
                        _context.Update(user);
                        TempData["SuccessMessage"] = "NgÆ°á»i dÃ¹ng Ä‘Ã£ Ä‘Æ°á»£c vÃ´ hiá»‡u hÃ³a thÃ nh cÃ´ng! (KhÃ´ng thá»ƒ xÃ³a vÃ¬ Ä‘Ã£ cÃ³ Ä‘Æ¡n hÃ ng)";
                    }
                    else
                    {
                        // Safe to delete completely
                        _context.Users.Remove(user);
                        TempData["SuccessMessage"] = "XÃ³a ngÆ°á»i dÃ¹ng thÃ nh cÃ´ng!";
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user ID: {UserId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa người dùng.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> UserExistsAsync(int id)
        {
            return await _context.Users.AnyAsync(e => e.UserID == id);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private async Task CreateSampleUsersAsync()
        {
            try
            {
                var sampleUsers = new List<User>
                {
                    new User
                    {
                        FullName = "Nguyễn Văn An",
                        Email = "nguyen.van.an@email.com",
                        PhoneNumber = "0901234567",
                        PasswordHash = HashPassword("123456"),
                        IsActive = true
                    },
                    new User
                    {
                        FullName = "Trần Thị Bích",
                        Email = "tran.thi.bich@email.com",
                        PhoneNumber = "0912345678",
                        PasswordHash = HashPassword("123456"),
                        IsActive = true
                    },
                    new User
                    {
                        FullName = "Lê Văn Cường",
                        Email = "le.van.cuong@email.com",
                        PhoneNumber = "0923456789",
                        PasswordHash = HashPassword("123456"),
                        IsActive = true
                    },
                    new User
                    {
                        FullName = "Phạm Thị Dung",
                        Email = "pham.thi.dung@email.com",
                        PhoneNumber = "0934567890",
                        PasswordHash = HashPassword("123456"),
                        IsActive = false
                    },
                    new User
                    {
                        FullName = "Hoàng Văn Em",
                        Email = "hoang.van.em@email.com",
                        PhoneNumber = "0945678901",
                        PasswordHash = HashPassword("123456"),
                        IsActive = true
                    },
                    new User
                    {
                        FullName = "Vũ Thị Phương",
                        Email = "vu.thi.phuong@email.com",
                        PhoneNumber = "0956789012",
                        PasswordHash = HashPassword("123456"),
                        IsActive = true
                    },
                    new User
                    {
                        FullName = "Đặng Văn Giang",
                        Email = "dang.van.giang@email.com",
                        PhoneNumber = "0967890123",
                        PasswordHash = HashPassword("123456"),
                        IsActive = false
                    },
                    new User
                    {
                        FullName = "Bùi Thị Hà",
                        Email = "bui.thi.ha@email.com",
                        PhoneNumber = "0978901234",
                        PasswordHash = HashPassword("123456"),
                        IsActive = true
                    }
                };

                _context.Users.AddRange(sampleUsers);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created {Count} sample users", sampleUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample users");
            }
        }
    }
} 


