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
                var promotionsQuery = _context.Promotions
                    .Include(p => p.PromotionProductScopes)
                    .Include(p => p.PromotionCategoryScopes)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    promotionsQuery = promotionsQuery.Where(p => 
                        p.PromotionName.Contains(search) || 
                        (p.CouponCode != null && p.CouponCode.Contains(search)) ||
                        (p.Description != null && p.Description.Contains(search)));
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

                var promotions = await promotionsQuery.ToListAsync();

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

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading promotions");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách voucher.";
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
    }
} 