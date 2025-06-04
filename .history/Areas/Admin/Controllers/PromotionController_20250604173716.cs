using JollibeeClone.Areas.Admin.Data;
using JollibeeClone.Areas.Admin.Models;
using JollibeeClone.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Areas.Admin.Attributes;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class PromotionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PromotionController> _logger;

        public PromotionController(AppDbContext context, ILogger<PromotionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Promotion
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? status, string? sort)
        {
            try
            {
                _logger.LogInformation("Starting to load promotions. Search: {search}, Status: {status}, Sort: {sort}", search, status, sort);

                // Test database connection first
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    _logger.LogError("Cannot connect to database");
                    TempData["ErrorMessage"] = "Không thể kết nối đến cơ sở dữ liệu.";
                    return View(new PromotionListViewModel());
                }

                // Check if Promotions table exists
                var tableExists = await _context.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>().ExistsAsync();
                _logger.LogInformation("Database exists: {tableExists}", tableExists);

                var promotionsQuery = _context.Promotions
                    .Include(p => p.PromotionProductScopes)
                    .Include(p => p.PromotionCategoryScopes)
                    .AsQueryable();

                _logger.LogInformation("Created base query");

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    promotionsQuery = promotionsQuery.Where(p => 
                        p.PromotionName.Contains(search) || 
                        (p.CouponCode != null && p.CouponCode.Contains(search)) ||
                        (p.Description != null && p.Description.Contains(search)));
                    _logger.LogInformation("Applied search filter: {search}", search);
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    switch (status.ToLower())
                    {
                        case "active":
                            promotionsQuery = promotionsQuery.Where(p => p.IsActive && p.EndDate >= DateTime.Today);
                            break;
                        case "inactive":
                            promotionsQuery = promotionsQuery.Where(p => !p.IsActive);
                            break;
                        case "expired":
                            promotionsQuery = promotionsQuery.Where(p => p.EndDate < DateTime.Today);
                            break;
                        case "upcoming":
                            promotionsQuery = promotionsQuery.Where(p => p.StartDate > DateTime.Today);
                            break;
                    }
                    _logger.LogInformation("Applied status filter: {status}", status);
                }

                // Apply sorting
                switch (sort?.ToLower())
                {
                    case "name":
                        promotionsQuery = promotionsQuery.OrderBy(p => p.PromotionName);
                        break;
                    case "name-desc":
                        promotionsQuery = promotionsQuery.OrderByDescending(p => p.PromotionName);
                        break;
                    case "startdate":
                        promotionsQuery = promotionsQuery.OrderBy(p => p.StartDate);
                        break;
                    case "enddate":
                        promotionsQuery = promotionsQuery.OrderBy(p => p.EndDate);
                        break;
                    case "usage":
                        promotionsQuery = promotionsQuery.OrderByDescending(p => p.UsesCount);
                        break;
                    default:
                        promotionsQuery = promotionsQuery.OrderByDescending(p => p.PromotionID);
                        break;
                }

                _logger.LogInformation("Applied sorting: {sort}", sort);

                var promotions = await promotionsQuery.ToListAsync();
                _logger.LogInformation("Loaded {count} promotions", promotions.Count);

                var viewModel = new PromotionListViewModel
                {
                    Promotions = promotions.Select(p => new PromotionDisplayViewModel
                    {
                        PromotionID = p.PromotionID,
                        PromotionName = p.PromotionName,
                        Description = p.Description,
                        CouponCode = p.CouponCode,
                        DiscountType = p.DiscountType,
                        DiscountValue = p.DiscountValue,
                        MinOrderValue = p.MinOrderValue,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        MaxUses = p.MaxUses,
                        UsesCount = p.UsesCount,
                        MaxUsesPerUser = p.MaxUsesPerUser,
                        IsActive = p.IsActive,
                        ProductCount = p.PromotionProductScopes.Count,
                        CategoryCount = p.PromotionCategoryScopes.Count
                    }).ToList(),
                    TotalCount = promotions.Count,
                    SearchTerm = search,
                    StatusFilter = status,
                    SortBy = sort
                };

                _logger.LogInformation("Created view model successfully");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading promotions. Message: {message}, StackTrace: {stackTrace}", ex.Message, ex.StackTrace);
                TempData["ErrorMessage"] = $"Có lỗi xảy ra khi tải danh sách voucher: {ex.Message}";
                return View(new PromotionListViewModel());
            }
        }

        // GET: Admin/Promotion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID voucher không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var promotion = await _context.Promotions
                    .Include(p => p.PromotionProductScopes)
                        .ThenInclude(pps => pps.Product)
                    .Include(p => p.PromotionCategoryScopes)
                        .ThenInclude(pcs => pcs.Category)
                    .FirstOrDefaultAsync(p => p.PromotionID == id);

                if (promotion == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy voucher.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new PromotionViewModel
                {
                    PromotionID = promotion.PromotionID,
                    PromotionName = promotion.PromotionName,
                    Description = promotion.Description,
                    CouponCode = promotion.CouponCode,
                    DiscountType = promotion.DiscountType,
                    DiscountValue = promotion.DiscountValue,
                    MinOrderValue = promotion.MinOrderValue,
                    StartDate = promotion.StartDate,
                    EndDate = promotion.EndDate,
                    MaxUses = promotion.MaxUses,
                    UsesCount = promotion.UsesCount,
                    MaxUsesPerUser = promotion.MaxUsesPerUser,
                    IsActive = promotion.IsActive,
                    SelectedProducts = promotion.PromotionProductScopes.Select(pps => pps.Product).ToList(),
                    SelectedCategories = promotion.PromotionCategoryScopes.Select(pcs => pcs.Category).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading promotion details for ID: {PromotionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin voucher.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Promotion/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = new PromotionViewModel();
                await LoadSelectLists(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create promotion page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang tạo voucher.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Promotion/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionViewModel model)
        {
            try
            {
                // Custom validation
                if (!string.IsNullOrEmpty(model.CouponCode))
                {
                    var existingCoupon = await _context.Promotions
                        .AnyAsync(p => p.CouponCode == model.CouponCode);
                    if (existingCoupon)
                    {
                        ModelState.AddModelError("CouponCode", "Mã voucher đã tồn tại.");
                    }
                }

                if (model.EndDate <= model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                }

                if (ModelState.IsValid)
                {
                    var promotion = new Promotion
                    {
                        PromotionName = model.PromotionName,
                        Description = model.Description,
                        CouponCode = model.CouponCode,
                        DiscountType = model.DiscountType,
                        DiscountValue = model.DiscountValue,
                        MinOrderValue = model.MinOrderValue,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        MaxUses = model.MaxUses,
                        MaxUsesPerUser = model.MaxUsesPerUser,
                        IsActive = model.IsActive
                    };

                    _context.Promotions.Add(promotion);
                    await _context.SaveChangesAsync();

                    // Add product scopes
                    if (model.SelectedProductIds.Any())
                    {
                        var productScopes = model.SelectedProductIds.Select(productId => new PromotionProductScope
                        {
                            PromotionID = promotion.PromotionID,
                            ProductID = productId
                        }).ToList();

                        _context.PromotionProductScopes.AddRange(productScopes);
                    }

                    // Add category scopes
                    if (model.SelectedCategoryIds.Any())
                    {
                        var categoryScopes = model.SelectedCategoryIds.Select(categoryId => new PromotionCategoryScope
                        {
                            PromotionID = promotion.PromotionID,
                            CategoryID = categoryId
                        }).ToList();

                        _context.PromotionCategoryScopes.AddRange(categoryScopes);
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Promotion created successfully: {PromotionName}", promotion.PromotionName);
                    TempData["SuccessMessage"] = "Voucher đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promotion: {PromotionName}", model.PromotionName);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo voucher.";
            }

            await LoadSelectLists(model);
            return View(model);
        }

        // GET: Admin/Promotion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID voucher không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var promotion = await _context.Promotions
                    .Include(p => p.PromotionProductScopes)
                    .Include(p => p.PromotionCategoryScopes)
                    .FirstOrDefaultAsync(p => p.PromotionID == id);

                if (promotion == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy voucher.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new PromotionViewModel
                {
                    PromotionID = promotion.PromotionID,
                    PromotionName = promotion.PromotionName,
                    Description = promotion.Description,
                    CouponCode = promotion.CouponCode,
                    DiscountType = promotion.DiscountType,
                    DiscountValue = promotion.DiscountValue,
                    MinOrderValue = promotion.MinOrderValue,
                    StartDate = promotion.StartDate,
                    EndDate = promotion.EndDate,
                    MaxUses = promotion.MaxUses,
                    UsesCount = promotion.UsesCount,
                    MaxUsesPerUser = promotion.MaxUsesPerUser,
                    IsActive = promotion.IsActive,
                    SelectedProductIds = promotion.PromotionProductScopes.Select(pps => pps.ProductID).ToList(),
                    SelectedCategoryIds = promotion.PromotionCategoryScopes.Select(pcs => pcs.CategoryID).ToList()
                };

                await LoadSelectLists(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit promotion page for ID: {PromotionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Promotion/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromotionViewModel model)
        {
            if (id != model.PromotionID)
            {
                TempData["ErrorMessage"] = "ID voucher không khớp.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Custom validation
                if (!string.IsNullOrEmpty(model.CouponCode))
                {
                    var existingCoupon = await _context.Promotions
                        .AnyAsync(p => p.CouponCode == model.CouponCode && p.PromotionID != id);
                    if (existingCoupon)
                    {
                        ModelState.AddModelError("CouponCode", "Mã voucher đã tồn tại.");
                    }
                }

                if (model.EndDate <= model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu.");
                }

                if (ModelState.IsValid)
                {
                    var promotion = await _context.Promotions
                        .Include(p => p.PromotionProductScopes)
                        .Include(p => p.PromotionCategoryScopes)
                        .FirstOrDefaultAsync(p => p.PromotionID == id);

                    if (promotion == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy voucher.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Update promotion properties
                    promotion.PromotionName = model.PromotionName;
                    promotion.Description = model.Description;
                    promotion.CouponCode = model.CouponCode;
                    promotion.DiscountType = model.DiscountType;
                    promotion.DiscountValue = model.DiscountValue;
                    promotion.MinOrderValue = model.MinOrderValue;
                    promotion.StartDate = model.StartDate;
                    promotion.EndDate = model.EndDate;
                    promotion.MaxUses = model.MaxUses;
                    promotion.MaxUsesPerUser = model.MaxUsesPerUser;
                    promotion.IsActive = model.IsActive;

                    // Update product scopes
                    _context.PromotionProductScopes.RemoveRange(promotion.PromotionProductScopes);
                    if (model.SelectedProductIds.Any())
                    {
                        var productScopes = model.SelectedProductIds.Select(productId => new PromotionProductScope
                        {
                            PromotionID = promotion.PromotionID,
                            ProductID = productId
                        }).ToList();

                        _context.PromotionProductScopes.AddRange(productScopes);
                    }

                    // Update category scopes
                    _context.PromotionCategoryScopes.RemoveRange(promotion.PromotionCategoryScopes);
                    if (model.SelectedCategoryIds.Any())
                    {
                        var categoryScopes = model.SelectedCategoryIds.Select(categoryId => new PromotionCategoryScope
                        {
                            PromotionID = promotion.PromotionID,
                            CategoryID = categoryId
                        }).ToList();

                        _context.PromotionCategoryScopes.AddRange(categoryScopes);
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Promotion updated successfully: {PromotionName}", promotion.PromotionName);
                    TempData["SuccessMessage"] = "Voucher đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating promotion: {PromotionName}", model.PromotionName);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật voucher.";
            }

            await LoadSelectLists(model);
            return View(model);
        }

        // GET: Admin/Promotion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID voucher không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var promotion = await _context.Promotions
                    .Include(p => p.PromotionProductScopes)
                        .ThenInclude(pps => pps.Product)
                    .Include(p => p.PromotionCategoryScopes)
                        .ThenInclude(pcs => pcs.Category)
                    .Include(p => p.Orders)
                    .FirstOrDefaultAsync(p => p.PromotionID == id);

                if (promotion == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy voucher.";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new PromotionViewModel
                {
                    PromotionID = promotion.PromotionID,
                    PromotionName = promotion.PromotionName,
                    Description = promotion.Description,
                    CouponCode = promotion.CouponCode,
                    DiscountType = promotion.DiscountType,
                    DiscountValue = promotion.DiscountValue,
                    MinOrderValue = promotion.MinOrderValue,
                    StartDate = promotion.StartDate,
                    EndDate = promotion.EndDate,
                    MaxUses = promotion.MaxUses,
                    UsesCount = promotion.UsesCount,
                    MaxUsesPerUser = promotion.MaxUsesPerUser,
                    IsActive = promotion.IsActive,
                    SelectedProducts = promotion.PromotionProductScopes.Select(pps => pps.Product).ToList(),
                    SelectedCategories = promotion.PromotionCategoryScopes.Select(pcs => pcs.Category).ToList()
                };

                ViewData["HasOrders"] = promotion.Orders.Any();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete promotion page for ID: {PromotionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang xóa voucher.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Promotion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var promotion = await _context.Promotions
                    .Include(p => p.Orders)
                    .FirstOrDefaultAsync(p => p.PromotionID == id);

                if (promotion == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy voucher.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if promotion has been used in orders
                if (promotion.Orders.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa voucher này vì đã được sử dụng trong đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Promotion deleted successfully: {PromotionName}", promotion.PromotionName);
                TempData["SuccessMessage"] = "Voucher đã được xóa thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting promotion with ID: {PromotionId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xóa voucher.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to load select lists
        private async Task LoadSelectLists(PromotionViewModel model)
        {
            model.AvailableProducts = await _context.Products
                .Where(p => p.IsAvailable)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            model.AvailableCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
        }

        // API endpoint for getting products by category
        [HttpGet]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.CategoryID == categoryId && p.IsAvailable)
                    .Select(p => new { id = p.ProductID, name = p.ProductName })
                    .ToListAsync();

                return Json(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category: {CategoryId}", categoryId);
                return Json(new List<object>());
            }
        }

        // API endpoint for getting promotion statistics
        [HttpGet]
        public async Task<IActionResult> GetPromotionStatistics(int id)
        {
            try
            {
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null)
                {
                    return Json(new { success = false, message = "Voucher không tồn tại" });
                }

                var stats = new
                {
                    success = true,
                    usesCount = promotion.UsesCount,
                    maxUses = promotion.MaxUses,
                    usagePercentage = promotion.MaxUses.HasValue ? (double)promotion.UsesCount / promotion.MaxUses.Value * 100 : 0,
                    remainingUses = promotion.MaxUses.HasValue ? promotion.MaxUses.Value - promotion.UsesCount : (int?)null,
                    daysRemaining = Math.Max(0, (promotion.EndDate - DateTime.Today).Days),
                    isActive = promotion.IsActive && promotion.StartDate <= DateTime.Today && promotion.EndDate >= DateTime.Today,
                    status = promotion.EndDate < DateTime.Today ? "expired" : 
                             promotion.StartDate > DateTime.Today ? "upcoming" : 
                             promotion.IsActive ? "active" : "inactive"
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting promotion statistics: {PromotionId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thống kê" });
            }
        }

        // API endpoint for checking coupon code availability
        [HttpGet]
        public async Task<IActionResult> CheckCouponCode(string couponCode, int? promotionId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(couponCode))
                {
                    return Json(new { available = true });
                }

                var existingPromotion = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.CouponCode == couponCode && p.PromotionID != promotionId);

                return Json(new { available = existingPromotion == null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking coupon code: {CouponCode}", couponCode);
                return Json(new { available = false });
            }
        }

        // API endpoint for toggling promotion status
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var promotion = await _context.Promotions.FindAsync(id);
                if (promotion == null)
                {
                    return Json(new { success = false, message = "Voucher không tồn tại" });
                }

                promotion.IsActive = !promotion.IsActive;
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    isActive = promotion.IsActive,
                    message = promotion.IsActive ? "Đã kích hoạt voucher" : "Đã tạm dừng voucher"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling promotion status: {PromotionId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thay đổi trạng thái" });
            }
        }

        // API endpoint for generating coupon code
        [HttpGet]
        public IActionResult GenerateCouponCode()
        {
            try
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var random = new Random();
                var couponCode = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                return Json(new { couponCode = couponCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating coupon code");
                return Json(new { couponCode = "" });
            }
        }

        // API endpoint for bulk operations
        [HttpPost]
        public async Task<IActionResult> BulkOperation(string operation, List<int> promotionIds)
        {
            try
            {
                if (!promotionIds.Any())
                {
                    return Json(new { success = false, message = "Không có voucher nào được chọn" });
                }

                var promotions = await _context.Promotions
                    .Where(p => promotionIds.Contains(p.PromotionID))
                    .ToListAsync();

                int affected = 0;
                switch (operation.ToLower())
                {
                    case "activate":
                        promotions.ForEach(p => p.IsActive = true);
                        affected = promotions.Count;
                        break;
                    case "deactivate":
                        promotions.ForEach(p => p.IsActive = false);
                        affected = promotions.Count;
                        break;
                    case "delete":
                        // Only delete promotions that haven't been used
                        var deletablePromotions = promotions.Where(p => p.UsesCount == 0).ToList();
                        _context.Promotions.RemoveRange(deletablePromotions);
                        affected = deletablePromotions.Count;
                        break;
                    default:
                        return Json(new { success = false, message = "Thao tác không hợp lệ" });
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    affected = affected,
                    message = $"Đã {operation} thành công {affected} voucher"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk operation: {Operation}", operation);
                return Json(new { success = false, message = "Có lỗi xảy ra khi thực hiện thao tác" });
            }
        }

        // Helper method to check if promotion can be edited
        private bool CanEditPromotion(Promotion promotion)
        {
            // Cannot edit if promotion has started and has been used
            return !(promotion.StartDate <= DateTime.Today && promotion.UsesCount > 0);
        }

        // Helper method to check if promotion can be deleted
        private bool CanDeletePromotion(Promotion promotion)
        {
            // Cannot delete if promotion has been used
            return promotion.UsesCount == 0;
        }

        // Export promotions to CSV
        [HttpGet]
        public async Task<IActionResult> ExportToCsv()
        {
            try
            {
                var promotions = await _context.Promotions
                    .Include(p => p.PromotionProductScopes)
                    .Include(p => p.PromotionCategoryScopes)
                    .OrderByDescending(p => p.PromotionID)
                    .ToListAsync();

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("ID,Tên Voucher,Mã Voucher,Loại Giảm Giá,Giá Trị,Đơn Hàng Tối Thiểu,Ngày Bắt Đầu,Ngày Kết Thúc,Số Lần Tối Đa,Đã Sử Dụng,Trạng Thái,Số Sản Phẩm,Số Danh Mục");

                foreach (var promotion in promotions)
                {
                    csv.AppendLine($"{promotion.PromotionID}," +
                                 $"\"{promotion.PromotionName}\"," +
                                 $"\"{promotion.CouponCode ?? ""}\"," +
                                 $"{promotion.DiscountType}," +
                                 $"{promotion.DiscountValue}," +
                                 $"{promotion.MinOrderValue ?? 0}," +
                                 $"{promotion.StartDate:yyyy-MM-dd}," +
                                 $"{promotion.EndDate:yyyy-MM-dd}," +
                                 $"{promotion.MaxUses ?? 0}," +
                                 $"{promotion.UsesCount}," +
                                 $"{(promotion.IsActive ? "Hoạt động" : "Không hoạt động")}," +
                                 $"{promotion.PromotionProductScopes.Count}," +
                                 $"{promotion.PromotionCategoryScopes.Count}");
                }

                var fileName = $"Promotions_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), 
                           "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting promotions to CSV");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xuất danh sách voucher";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Promotion - Debug version
        [HttpGet]
        [Route("Admin/Promotion/Debug")]
        public async Task<IActionResult> DebugIndex()
        {
            try
            {
                // Create a simple response without authentication
                var debugInfo = new List<string>();
                
                debugInfo.Add($"Context initialized: {_context != null}");
                
                // Test database connection
                try
                {
                    var canConnect = await _context.Database.CanConnectAsync();
                    debugInfo.Add($"Database connection: {canConnect}");
                }
                catch (Exception ex)
                {
                    debugInfo.Add($"Database connection error: {ex.Message}");
                }

                // Test if tables exist
                try
                {
                    var promotionCount = await _context.Promotions.CountAsync();
                    debugInfo.Add($"Promotion table accessible, count: {promotionCount}");
                }
                catch (Exception ex)
                {
                    debugInfo.Add($"Promotion table error: {ex.Message}");
                }

                // Test if related tables exist
                try
                {
                    var productCount = await _context.Products.CountAsync();
                    debugInfo.Add($"Product table accessible, count: {productCount}");
                }
                catch (Exception ex)
                {
                    debugInfo.Add($"Product table error: {ex.Message}");
                }

                try
                {
                    var categoryCount = await _context.Categories.CountAsync();
                    debugInfo.Add($"Category table accessible, count: {categoryCount}");
                }
                catch (Exception ex)
                {
                    debugInfo.Add($"Category table error: {ex.Message}");
                }

                ViewBag.DebugInfo = debugInfo;
                return View("DebugIndex");
            }
            catch (Exception ex)
            {
                ViewBag.DebugInfo = new List<string> { $"General error: {ex.Message}", $"StackTrace: {ex.StackTrace}" };
                return View("DebugIndex");
            }
        }

        // GET: Admin/Promotion/CreateSampleData
        [HttpGet]
        [Route("Admin/Promotion/CreateSampleData")]
        public async Task<IActionResult> CreateSampleData()
        {
            try
            {
                var messages = new List<string>();

                // Create sample categories if they don't exist
                if (!await _context.Categories.AnyAsync())
                {
                    var categories = new List<Categories>
                    {
                        new Categories { CategoryName = "Gà Rán", Description = "Các món gà rán thơm ngon", IsActive = true, DisplayOrder = 1 },
                        new Categories { CategoryName = "Burger", Description = "Burger đa dạng hương vị", IsActive = true, DisplayOrder = 2 },
                        new Categories { CategoryName = "Thức Uống", Description = "Đồ uống giải khát", IsActive = true, DisplayOrder = 3 },
                        new Categories { CategoryName = "Tráng Miệng", Description = "Các món tráng miệng", IsActive = true, DisplayOrder = 4 }
                    };

                    _context.Categories.AddRange(categories);
                    await _context.SaveChangesAsync();
                    messages.Add($"Created {categories.Count} sample categories");
                }

                // Create sample products if they don't exist
                if (!await _context.Products.AnyAsync())
                {
                    var categories = await _context.Categories.ToListAsync();
                    var products = new List<Product>
                    {
                        new Product { ProductName = "Gà Rán Giòn", ShortDescription = "Gà rán giòn tan", Price = 35000, CategoryID = categories[0].CategoryID, IsAvailable = true },
                        new Product { ProductName = "Burger Tôm", ShortDescription = "Burger tôm tươi ngon", Price = 45000, CategoryID = categories[1].CategoryID, IsAvailable = true },
                        new Product { ProductName = "Coca Cola", ShortDescription = "Nước ngọt giải khát", Price = 15000, CategoryID = categories[2].CategoryID, IsAvailable = true },
                        new Product { ProductName = "Kem Vanilla", ShortDescription = "Kem vị vanilla", Price = 25000, CategoryID = categories[3].CategoryID, IsAvailable = true }
                    };

                    _context.Products.AddRange(products);
                    await _context.SaveChangesAsync();
                    messages.Add($"Created {products.Count} sample products");
                }

                // Create sample promotions if they don't exist
                if (!await _context.Promotions.AnyAsync())
                {
                    var products = await _context.Products.ToListAsync();
                    var categories = await _context.Categories.ToListAsync();

                    var promotions = new List<Promotion>
                    {
                        new Promotion
                        {
                            PromotionName = "Giảm 20% cho Gà Rán",
                            Description = "Khuyến mãi giảm 20% cho tất cả món gà rán",
                            CouponCode = "CHICKEN20",
                            DiscountType = "Percentage",
                            DiscountValue = 20,
                            StartDate = DateTime.Today,
                            EndDate = DateTime.Today.AddDays(30),
                            MaxUses = 100,
                            UsesCount = 0,
                            IsActive = true
                        },
                        new Promotion
                        {
                            PromotionName = "Combo Burger + Drink",
                            Description = "Giảm 15% khi mua burger kèm thức uống",
                            CouponCode = "COMBO15",
                            DiscountType = "Percentage",
                            DiscountValue = 15,
                            MinOrderValue = 50000,
                            StartDate = DateTime.Today,
                            EndDate = DateTime.Today.AddDays(15),
                            MaxUses = 50,
                            MaxUsesPerUser = 2,
                            UsesCount = 0,
                            IsActive = true
                        }
                    };

                    _context.Promotions.AddRange(promotions);
                    await _context.SaveChangesAsync();

                    // Add promotion scopes
                    var promotion1 = promotions[0];
                    var promotion2 = promotions[1];

                    // Promotion 1: Apply to chicken category
                    if (categories.Any(c => c.CategoryName.Contains("Gà")))
                    {
                        var chickenCategory = categories.First(c => c.CategoryName.Contains("Gà"));
                        _context.PromotionCategoryScopes.Add(new PromotionCategoryScope
                        {
                            PromotionID = promotion1.PromotionID,
                            CategoryID = chickenCategory.CategoryID
                        });
                    }

                    // Promotion 2: Apply to specific products
                    if (products.Any(p => p.ProductName.Contains("Burger")))
                    {
                        var burgerProduct = products.First(p => p.ProductName.Contains("Burger"));
                        _context.PromotionProductScopes.Add(new PromotionProductScope
                        {
                            PromotionID = promotion2.PromotionID,
                            ProductID = burgerProduct.ProductID
                        });
                    }

                    if (categories.Any(c => c.CategoryName.Contains("Thức Uống")))
                    {
                        var drinkCategory = categories.First(c => c.CategoryName.Contains("Thức Uống"));
                        _context.PromotionCategoryScopes.Add(new PromotionCategoryScope
                        {
                            PromotionID = promotion2.PromotionID,
                            CategoryID = drinkCategory.CategoryID
                        });
                    }

                    await _context.SaveChangesAsync();
                    messages.Add($"Created {promotions.Count} sample promotions with scopes");
                }

                TempData["SuccessMessage"] = string.Join("<br>", messages);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating sample data: {ex.Message}";
                return RedirectToAction("DebugIndex");
            }
        }
    }
} 