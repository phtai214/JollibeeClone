using JollibeeClone.Data;
using JollibeeClone.Models;
using JollibeeClone.ViewModels;
using JollibeeClone.Areas.Admin.Services;
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
        private readonly IPromotionService _promotionService;

        public PromotionController(AppDbContext context, ILogger<PromotionController> logger, IPromotionService promotionService)
        {
            _context = context;
            _logger = logger;
            _promotionService = promotionService;
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
                    TempData["ErrorMessage"] = "KhÃ´ng thá»ƒ káº¿t ná»‘i Ä‘áº¿n cÆ¡ sá»Ÿ dá»¯ liá»‡u.";
                    return View(new PromotionListViewModel());
                }

                _logger.LogInformation("Database connection successful");

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
                TempData["ErrorMessage"] = $"CÃ³ lá»—i xáº£y ra khi táº£i danh sÃ¡ch voucher: {ex.Message}";
                return View(new PromotionListViewModel());
            }
        }

        // GET: Admin/Promotion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID voucher khÃ´ng há»£p lá»‡.";
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
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y voucher.";
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
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i thÃ´ng tin voucher.";
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
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i trang táº¡o voucher.";
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
                _logger.LogInformation("Starting Create promotion. Model received:");
                _logger.LogInformation("PromotionName: {PromotionName}", model.PromotionName);
                _logger.LogInformation("DiscountValue: {DiscountValue}", model.DiscountValue);
                _logger.LogInformation("SelectedProductIds count: {Count}", model.SelectedProductIds?.Count ?? 0);
                _logger.LogInformation("SelectedCategoryIds count: {Count}", model.SelectedCategoryIds?.Count ?? 0);
                
                // Ensure lists are not null
                model.SelectedProductIds = model.SelectedProductIds ?? new List<int>();
                model.SelectedCategoryIds = model.SelectedCategoryIds ?? new List<int>();
                
                _logger.LogInformation("SelectedProductIds: {ProductIds}", string.Join(", ", model.SelectedProductIds));
                _logger.LogInformation("SelectedCategoryIds: {CategoryIds}", string.Join(", ", model.SelectedCategoryIds));

                // Custom validation
                if (!string.IsNullOrEmpty(model.CouponCode))
                {
                    var existingCoupon = await _context.Promotions
                        .AnyAsync(p => p.CouponCode == model.CouponCode);
                    if (existingCoupon)
                    {
                        ModelState.AddModelError("CouponCode", "MÃ£ voucher Ä‘Ã£ tá»“n táº¡i.");
                    }
                }

                if (model.EndDate <= model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "NgÃ y káº¿t thÃºc pháº£i sau ngÃ y báº¯t Ä‘áº§u.");
                }

                // LOGIC CÅ¨: Báº®T BUá»˜C PHáº¢I CHá»ŒN Cáº¢ PRODUCTS VÃ€ CATEGORIES
                if (!model.SelectedProductIds.Any() || !model.SelectedCategoryIds.Any())
                {
                    ModelState.AddModelError("", "Vui lÃ²ng chá»n Ã­t nháº¥t má»™t sáº£n pháº©m VÃ€ má»™t danh má»¥c Ä‘á»ƒ Ã¡p dá»¥ng voucher.");
                }

                if (ModelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid, creating promotion...");
                    
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
                    
                    _logger.LogInformation("Promotion created with ID: {PromotionId}", promotion.PromotionID);

                    // Add product scopes
                    if (model.SelectedProductIds.Any())
                    {
                        var productScopes = model.SelectedProductIds.Select(productId => new PromotionProductScope
                        {
                            PromotionID = promotion.PromotionID,
                            ProductID = productId
                        }).ToList();

                        _context.PromotionProductScopes.AddRange(productScopes);
                        _logger.LogInformation("Added {Count} product scopes", productScopes.Count);
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
                        _logger.LogInformation("Added {Count} category scopes", categoryScopes.Count);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("All scopes saved successfully");

                    _logger.LogInformation("Promotion created successfully: {PromotionName}", promotion.PromotionName);
                    TempData["SuccessMessage"] = "Voucher Ä‘Ã£ Ä‘Æ°á»£c táº¡o thÃ nh cÃ´ng!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid:");
                    foreach (var error in ModelState)
                    {
                        _logger.LogWarning("Field: {Field}, Errors: {Errors}", 
                            error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating promotion: {PromotionName}", model.PromotionName);
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº¡o voucher.";
            }

            await LoadSelectLists(model);
            return View(model);
        }

        // GET: Admin/Promotion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID voucher khÃ´ng há»£p lá»‡.";
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
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y voucher.";
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
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i trang chá»‰nh sá»­a.";
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
                TempData["ErrorMessage"] = "ID voucher khÃ´ng khá»›p.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _logger.LogInformation("Starting Edit promotion ID: {PromotionId}. Model received:", id);
                _logger.LogInformation("PromotionName: {PromotionName}", model.PromotionName);
                _logger.LogInformation("DiscountValue: {DiscountValue}", model.DiscountValue);
                _logger.LogInformation("SelectedProductIds count: {Count}", model.SelectedProductIds?.Count ?? 0);
                _logger.LogInformation("SelectedCategoryIds count: {Count}", model.SelectedCategoryIds?.Count ?? 0);
                
                // Ensure lists are not null
                model.SelectedProductIds = model.SelectedProductIds ?? new List<int>();
                model.SelectedCategoryIds = model.SelectedCategoryIds ?? new List<int>();
                
                _logger.LogInformation("SelectedProductIds: {ProductIds}", string.Join(", ", model.SelectedProductIds));
                _logger.LogInformation("SelectedCategoryIds: {CategoryIds}", string.Join(", ", model.SelectedCategoryIds));

                // Custom validation
                if (!string.IsNullOrEmpty(model.CouponCode))
                {
                    var existingCoupon = await _context.Promotions
                        .AnyAsync(p => p.CouponCode == model.CouponCode && p.PromotionID != id);
                    if (existingCoupon)
                    {
                        ModelState.AddModelError("CouponCode", "MÃ£ voucher Ä‘Ã£ tá»“n táº¡i.");
                    }
                }

                if (model.EndDate <= model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "NgÃ y káº¿t thÃºc pháº£i sau ngÃ y báº¯t Ä‘áº§u.");
                }

                // LOGIC CÅ¨: Báº®T BUá»˜C PHáº¢I CHá»ŒN Cáº¢ PRODUCTS VÃ€ CATEGORIES
                if (!model.SelectedProductIds.Any() || !model.SelectedCategoryIds.Any())
                {
                    ModelState.AddModelError("", "Vui lÃ²ng chá»n Ã­t nháº¥t má»™t sáº£n pháº©m VÃ€ má»™t danh má»¥c Ä‘á»ƒ Ã¡p dá»¥ng voucher.");
                }

                if (ModelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid, updating promotion...");
                    
                    var promotion = await _context.Promotions
                        .Include(p => p.PromotionProductScopes)
                        .Include(p => p.PromotionCategoryScopes)
                        .FirstOrDefaultAsync(p => p.PromotionID == id);

                    if (promotion == null)
                    {
                        TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y voucher.";
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
                        _logger.LogInformation("Updated with {Count} product scopes", productScopes.Count);
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
                        _logger.LogInformation("Updated with {Count} category scopes", categoryScopes.Count);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("All updates saved successfully");

                    _logger.LogInformation("Promotion updated successfully: {PromotionName}", promotion.PromotionName);
                    TempData["SuccessMessage"] = "Voucher Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t thÃ nh cÃ´ng!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid for Edit:");
                    foreach (var error in ModelState)
                    {
                        _logger.LogWarning("Field: {Field}, Errors: {Errors}", 
                            error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating promotion: {PromotionName}", model.PromotionName);
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi cáº­p nháº­t voucher.";
            }

            await LoadSelectLists(model);
            return View(model);
        }

        // GET: Admin/Promotion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID voucher khÃ´ng há»£p lá»‡.";
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
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y voucher.";
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
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi táº£i trang xÃ³a voucher.";
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
                    TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y voucher.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if promotion has been used in orders
                if (promotion.Orders.Any())
                {
                    TempData["ErrorMessage"] = "KhÃ´ng thá»ƒ xÃ³a voucher nÃ y vÃ¬ Ä‘Ã£ Ä‘Æ°á»£c sá»­ dá»¥ng trong Ä‘Æ¡n hÃ ng.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Promotion deleted successfully: {PromotionName}", promotion.PromotionName);
                TempData["SuccessMessage"] = "Voucher Ä‘Ã£ Ä‘Æ°á»£c xÃ³a thÃ nh cÃ´ng!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting promotion with ID: {PromotionId}", id);
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi xÃ³a voucher.";
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
                    return Json(new { success = false, message = "Voucher khÃ´ng tá»“n táº¡i" });
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
                return Json(new { success = false, message = "CÃ³ lá»—i xáº£y ra khi láº¥y thá»‘ng kÃª" });
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
                    return Json(new { success = false, message = "Voucher khÃ´ng tá»“n táº¡i" });
                }

                promotion.IsActive = !promotion.IsActive;
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    isActive = promotion.IsActive,
                    message = promotion.IsActive ? "ÄÃ£ kÃ­ch hoáº¡t voucher" : "ÄÃ£ táº¡m dá»«ng voucher"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling promotion status: {PromotionId}", id);
                return Json(new { success = false, message = "CÃ³ lá»—i xáº£y ra khi thay Ä‘á»•i tráº¡ng thÃ¡i" });
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
                    return Json(new { success = false, message = "KhÃ´ng cÃ³ voucher nÃ o Ä‘Æ°á»£c chá»n" });
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
                        return Json(new { success = false, message = "Thao tÃ¡c khÃ´ng há»£p lá»‡" });
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    affected = affected,
                    message = $"ÄÃ£ {operation} thÃ nh cÃ´ng {affected} voucher"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk operation: {Operation}", operation);
                return Json(new { success = false, message = "CÃ³ lá»—i xáº£y ra khi thá»±c hiá»‡n thao tÃ¡c" });
            }
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
                csv.AppendLine("ID,TÃªn Voucher,MÃ£ Voucher,Loáº¡i Giáº£m GiÃ¡,GiÃ¡ Trá»‹,ÄÆ¡n HÃ ng Tá»‘i Thiá»ƒu,NgÃ y Báº¯t Äáº§u,NgÃ y Káº¿t ThÃºc,Sá»‘ Láº§n Tá»‘i Äa,ÄÃ£ Sá»­ Dá»¥ng,Tráº¡ng ThÃ¡i,Sá»‘ Sáº£n Pháº©m,Sá»‘ Danh Má»¥c");

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
                                 $"{(promotion.IsActive ? "Hoáº¡t Ä‘á»™ng" : "KhÃ´ng hoáº¡t Ä‘á»™ng")}," +
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
                TempData["ErrorMessage"] = "CÃ³ lá»—i xáº£y ra khi xuáº¥t danh sÃ¡ch voucher";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Promotion/Test - Simple test without includes
        [HttpGet]
        [Route("Admin/Promotion/Test")]
        public async Task<IActionResult> TestBasic()
        {
            try
            {
                // Test 1: Basic connection
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    ViewBag.Error = "Cannot connect to database";
                    return View("TestResult");
                }

                // Test 2: Count promotions without includes
                var promotionCount = await _context.Promotions.CountAsync();
                ViewBag.PromotionCount = promotionCount;

                // Test 3: Get basic promotion data without scope includes
                var basicPromotions = await _context.Promotions
                    .Select(p => new {
                        p.PromotionID,
                        p.PromotionName,
                        p.DiscountValue,
                        p.IsActive
                    })
                    .Take(5)
                    .ToListAsync();

                ViewBag.BasicPromotions = basicPromotions;

                // Test 4: Try to access PromotionProductScopes table directly
                try
                {
                    var scopeCount = await _context.PromotionProductScopes.CountAsync();
                    ViewBag.ScopeCount = scopeCount;
                    ViewBag.ScopeError = null;
                }
                catch (Exception scopeEx)
                {
                    ViewBag.ScopeCount = -1;
                    ViewBag.ScopeError = scopeEx.Message;
                }

                // Test 5: Try to access PromotionCategoryScopes table directly
                try
                {
                    var catScopeCount = await _context.PromotionCategoryScopes.CountAsync();
                    ViewBag.CatScopeCount = catScopeCount;
                    ViewBag.CatScopeError = null;
                }
                catch (Exception catScopeEx)
                {
                    ViewBag.CatScopeCount = -1;
                    ViewBag.CatScopeError = catScopeEx.Message;
                }

                ViewBag.Success = true;
                return View("TestResult");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Success = false;
                return View("TestResult");
            }
        }

        // GET: Admin/Promotion/Simple - Simplified version without includes
        [HttpGet]
        [Route("Admin/Promotion/Simple")]
        public async Task<IActionResult> SimpleIndex()
        {
            try
            {
                // Simple query without includes to avoid the error
                var promotions = await _context.Promotions
                    .OrderByDescending(p => p.PromotionID)
                    .ToListAsync();

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
                        ProductCount = 0, // Skip counting for now
                        CategoryCount = 0 // Skip counting for now
                    }).ToList(),
                    TotalCount = promotions.Count,
                    SearchTerm = null,
                    StatusFilter = null,
                    SortBy = null
                };

                ViewBag.IsSimpleMode = true;
                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Simple mode error: {ex.Message}";
                return View("Index", new PromotionListViewModel());
            }
        }

        // GET: Admin/Promotion/TestCreate - Simple test for debugging
        [HttpGet]
        [Route("Admin/Promotion/TestCreate")]
        public async Task<IActionResult> TestCreate()
        {
            try
            {
                _logger.LogInformation("Loading TestCreate page...");
                
                var viewModel = new PromotionViewModel
                {
                    PromotionName = "Test Voucher",
                    DiscountValue = 10,
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(30),
                    IsActive = true
                };
                
                await LoadSelectLists(viewModel);
                _logger.LogInformation("Available Products: {Count}", viewModel.AvailableProducts.Count);
                _logger.LogInformation("Available Categories: {Count}", viewModel.AvailableCategories.Count);
                
                ViewBag.IsTestMode = true;
                return View("Create", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestCreate");
                TempData["ErrorMessage"] = $"Test Create Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Promotion/TestCreateSimple - Simple test create without complex validation
        [HttpPost]
        [Route("Admin/Promotion/TestCreateSimple")]
        public async Task<IActionResult> TestCreateSimple()
        {
            try
            {
                _logger.LogInformation("TestCreateSimple started");
                
                var promotion = new Promotion
                {
                    PromotionName = "Test Voucher " + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    Description = "Test Description",
                    DiscountType = "Percentage",
                    DiscountValue = 10,
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddDays(30),
                    IsActive = true
                };

                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Test promotion created with ID: {PromotionId}", promotion.PromotionID);

                // Add a simple product scope if products exist
                var firstProduct = await _context.Products.FirstOrDefaultAsync();
                if (firstProduct != null)
                {
                    var productScope = new PromotionProductScope
                    {
                        PromotionID = promotion.PromotionID,
                        ProductID = firstProduct.ProductID
                    };
                    
                    _context.PromotionProductScopes.Add(productScope);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Added product scope for product: {ProductId}", firstProduct.ProductID);
                }

                TempData["SuccessMessage"] = $"Test voucher created successfully! ID: {promotion.PromotionID}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestCreateSimple");
                TempData["ErrorMessage"] = $"Test Create Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Promotion/TestValidation - Debug validation
        [HttpPost]
        [Route("Admin/Promotion/TestValidation")]
        public async Task<IActionResult> TestValidation(PromotionViewModel model)
        {
            try
            {
                _logger.LogInformation("=== TEST VALIDATION DEBUG ===");
                _logger.LogInformation("PromotionName: {PromotionName}", model.PromotionName);
                _logger.LogInformation("DiscountValue: {DiscountValue}", model.DiscountValue);
                _logger.LogInformation("SelectedProductIds: {ProductIds}", string.Join(",", model.SelectedProductIds ?? new List<int>()));
                _logger.LogInformation("SelectedCategoryIds: {CategoryIds}", string.Join(",", model.SelectedCategoryIds ?? new List<int>()));
                
                // Ensure lists are not null
                model.SelectedProductIds = model.SelectedProductIds ?? new List<int>();
                model.SelectedCategoryIds = model.SelectedCategoryIds ?? new List<int>();
                
                _logger.LogInformation("Product count: {Count}", model.SelectedProductIds.Count);
                _logger.LogInformation("Category count: {Count}", model.SelectedCategoryIds.Count);
                
                // Test our validation logic
                bool hasProducts = model.SelectedProductIds.Any();
                bool hasCategories = model.SelectedCategoryIds.Any();
                bool shouldPass = hasProducts || hasCategories;
                
                _logger.LogInformation("Has products: {HasProducts}", hasProducts);
                _logger.LogInformation("Has categories: {HasCategories}", hasCategories);
                _logger.LogInformation("Should pass validation: {ShouldPass}", shouldPass);
                
                // Check ModelState
                _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState errors:");
                    foreach (var error in ModelState)
                    {
                        if (error.Value.Errors.Any())
                        {
                            _logger.LogWarning("Field: {Field}, Errors: {Errors}", 
                                error.Key, string.Join("; ", error.Value.Errors.Select(e => e.ErrorMessage)));
                        }
                    }
                }
                
                return Json(new {
                    success = true,
                    hasProducts = hasProducts,
                    hasCategories = hasCategories,
                    shouldPass = shouldPass,
                    modelStateValid = ModelState.IsValid,
                    productIds = model.SelectedProductIds,
                    categoryIds = model.SelectedCategoryIds,
                    modelStateErrors = ModelState.Where(x => x.Value.Errors.Any())
                        .ToDictionary(x => x.Key, x => x.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestValidation");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: Admin/Promotion/CreateSimple - Simple create without complex validation
        [HttpPost]
        [Route("Admin/Promotion/CreateSimple")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSimple(PromotionViewModel model)
        {
            try
            {
                _logger.LogInformation("=== SIMPLE CREATE TEST ===");
                _logger.LogInformation("PromotionName: {PromotionName}", model.PromotionName);
                _logger.LogInformation("DiscountValue: {DiscountValue}", model.DiscountValue);
                
                // Ensure lists are not null
                model.SelectedProductIds = model.SelectedProductIds ?? new List<int>();
                model.SelectedCategoryIds = model.SelectedCategoryIds ?? new List<int>();
                
                _logger.LogInformation("SelectedProductIds: {ProductIds}", string.Join(",", model.SelectedProductIds));
                _logger.LogInformation("SelectedCategoryIds: {CategoryIds}", string.Join(",", model.SelectedCategoryIds));

                // Basic validation only
                if (string.IsNullOrEmpty(model.PromotionName))
                {
                    TempData["ErrorMessage"] = "TÃªn voucher lÃ  báº¯t buá»™c";
                    await LoadSelectLists(model);
                    return View("Create", model);
                }

                if (model.DiscountValue <= 0)
                {
                    TempData["ErrorMessage"] = "GiÃ¡ trá»‹ giáº£m giÃ¡ pháº£i lá»›n hÆ¡n 0";
                    await LoadSelectLists(model);
                    return View("Create", model);
                }

                if (model.EndDate <= model.StartDate)
                {
                    TempData["ErrorMessage"] = "NgÃ y káº¿t thÃºc pháº£i sau ngÃ y báº¯t Ä‘áº§u";
                    await LoadSelectLists(model);
                    return View("Create", model);
                }

                // Check selection
                if (!model.SelectedProductIds.Any() && !model.SelectedCategoryIds.Any())
                {
                    TempData["ErrorMessage"] = "Vui lÃ²ng chá»n Ã­t nháº¥t má»™t sáº£n pháº©m hoáº·c danh má»¥c";
                    await LoadSelectLists(model);
                    return View("Create", model);
                }

                _logger.LogInformation("Basic validation passed, creating promotion...");

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

                _logger.LogInformation("Promotion created with ID: {PromotionId}", promotion.PromotionID);

                // Add product scopes
                if (model.SelectedProductIds.Any())
                {
                    var productScopes = model.SelectedProductIds.Select(productId => new PromotionProductScope
                    {
                        PromotionID = promotion.PromotionID,
                        ProductID = productId
                    }).ToList();

                    _context.PromotionProductScopes.AddRange(productScopes);
                    _logger.LogInformation("Added {Count} product scopes", productScopes.Count);
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
                    _logger.LogInformation("Added {Count} category scopes", categoryScopes.Count);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("All scopes saved successfully");

                TempData["SuccessMessage"] = $"Voucher '{promotion.PromotionName}' Ä‘Ã£ Ä‘Æ°á»£c táº¡o thÃ nh cÃ´ng!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateSimple");
                TempData["ErrorMessage"] = $"CÃ³ lá»—i xáº£y ra: {ex.Message}";
                await LoadSelectLists(model);
                return View("Create", model);
            }
        }

        // ===================
        // NEW VOUCHER API METHODS
        // ===================

        // API: Validate voucher for user
        [HttpPost]
        [Route("Admin/Promotion/ValidateVoucher")]
        public async Task<IActionResult> ValidateVoucher([FromBody] VoucherValidationRequest request)
        {
            try
            {
                _logger.LogInformation("Validating voucher: {CouponCode} for user: {UserId}, amount: {Amount}", 
                    request.CouponCode, request.UserId, request.OrderAmount);

                var result = await _promotionService.ValidatePromotionForUserAsync(
                    request.UserId, request.CouponCode, request.OrderAmount);

                return Json(new
                {
                    success = result.IsValid,
                    message = result.ErrorMessage,
                    discountAmount = result.DiscountAmount,
                    promotion = result.Promotion != null ? new
                    {
                        id = result.Promotion.PromotionID,
                        name = result.Promotion.PromotionName,
                        discountType = result.Promotion.DiscountType,
                        discountValue = result.Promotion.DiscountValue
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating voucher: {CouponCode}", request.CouponCode);
                return Json(new { success = false, message = "CÃ³ lá»—i xáº£y ra khi kiá»ƒm tra voucher" });
            }
        }

        // API: Apply voucher for user
        [HttpPost]
        [Route("Admin/Promotion/ApplyVoucher")]
        public async Task<IActionResult> ApplyVoucher([FromBody] VoucherApplicationRequest request)
        {
            try
            {
                _logger.LogInformation("Applying voucher: promotion {PromotionId} for user: {UserId}, order: {OrderId}", 
                    request.PromotionId, request.UserId, request.OrderId);

                var userPromotion = await _promotionService.ApplyPromotionAsync(
                    request.UserId, request.PromotionId, request.OrderId, request.DiscountAmount);

                return Json(new
                {
                    success = true,
                    message = "Voucher Ä‘Ã£ Ä‘Æ°á»£c Ã¡p dá»¥ng thÃ nh cÃ´ng",
                    userPromotionId = userPromotion.UserPromotionID,
                    discountAmount = userPromotion.DiscountAmount,
                    usedDate = userPromotion.UsedDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying voucher: promotion {PromotionId} for user {UserId}", 
                    request.PromotionId, request.UserId);
                return Json(new { success = false, message = ex.Message });
            }
        }

        // API: Get available vouchers for user
        [HttpGet]
        [Route("Admin/Promotion/AvailableForUser/{userId}")]
        public async Task<IActionResult> GetAvailableVouchersForUser(int userId)
        {
            try
            {
                _logger.LogInformation("Getting available vouchers for user: {UserId}", userId);

                var promotions = await _promotionService.GetAvailablePromotionsForUserAsync(userId);

                var result = promotions.Select(p => new
                {
                    id = p.PromotionID,
                    name = p.PromotionName,
                    description = p.Description,
                    couponCode = p.CouponCode,
                    discountType = p.DiscountType,
                    discountValue = p.DiscountValue,
                    minOrderValue = p.MinOrderValue,
                    startDate = p.StartDate,
                    endDate = p.EndDate,
                    maxUses = p.MaxUses,
                    usesCount = p.UsesCount,
                    remainingUses = p.MaxUses.HasValue ? p.MaxUses.Value - p.UsesCount : (int?)null
                }).ToList();

                return Json(new { success = true, vouchers = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available vouchers for user: {UserId}", userId);
                return Json(new { success = false, message = " voucher" });
            }
        }

        // API: Check if user has used voucher
        [HttpGet]
        [Route("Admin/Promotion/CheckUserUsage/{userId}/{promotionId}")]
        public async Task<IActionResult> CheckUserUsage(int userId, int promotionId)
        {
            try
            {
                var hasUsed = await _promotionService.HasUserUsedPromotionAsync(userId, promotionId);
                
                return Json(new { 
                    success = true, 
                    hasUsed = hasUsed,
                    message = hasUsed ? "User Đã sử dụng voucher này" : "User chưa sử dụng voucher này"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user usage: user {UserId}, promotion {PromotionId}", 
                    userId, promotionId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi kiểm tra" });
            }
        }

        // API: Get user's voucher usage history
        [HttpGet]
        [Route("Admin/Promotion/UserHistory/{userId}")]
        public async Task<IActionResult> GetUserVoucherHistory(int userId)
        {
            try
            {
                var history = await _context.UserPromotions
                    .Include(up => up.Promotion)
                    .Include(up => up.Order)
                    .Where(up => up.UserID == userId)
                    .OrderByDescending(up => up.UsedDate)
                    .Select(up => new
                    {
                        id = up.UserPromotionID,
                        promotionName = up.Promotion.PromotionName,
                        couponCode = up.Promotion.CouponCode,
                        discountAmount = up.DiscountAmount,
                        usedDate = up.UsedDate,
                        orderCode = up.Order != null ? up.Order.OrderCode : null,
                        orderId = up.OrderID
                    })
                    .ToListAsync();

                return Json(new { success = true, history = history });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting voucher history for user: {UserId}", userId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy lịch sử sử dụng voucher" });
            }
        }

        // API: Get promotion usage statistics (who used this promotion)
        [HttpGet]
        [Route("Admin/Promotion/UsageStatistics/{promotionId}")]
        public async Task<IActionResult> GetPromotionUsageStatistics(int promotionId)
        {
            try
            {
                var usageStats = await _context.UserPromotions
                    .Include(up => up.User)
                    .Include(up => up.Order)
                    .Where(up => up.PromotionID == promotionId)
                    .OrderByDescending(up => up.UsedDate)
                    .Select(up => new
                    {
                        userPromotionId = up.UserPromotionID,
                        userId = up.UserID,
                        userFullName = up.User.FullName,
                        userEmail = up.User.Email,
                        discountAmount = up.DiscountAmount,
                        usedDate = up.UsedDate,
                        orderId = up.OrderID,
                        orderCode = up.Order != null ? up.Order.OrderCode : null,
                        orderTotal = up.Order != null ? up.Order.TotalAmount : (decimal?)null
                    })
                    .ToListAsync();

                var totalUsers = usageStats.Count;
                var totalDiscountGiven = usageStats.Sum(u => u.discountAmount);

                return Json(new { 
                    success = true, 
                    usageStats = usageStats,
                    summary = new {
                        totalUsers = totalUsers,
                        totalDiscountGiven = totalDiscountGiven,
                        averageDiscountPerUser = totalUsers > 0 ? totalDiscountGiven / totalUsers : 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage statistics for promotion: {PromotionId}", promotionId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi lấy thống kê sử dụng" });
            }
        }
    }

    // DTOs for API requests
    public class VoucherValidationRequest
    {
        public int UserId { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public decimal OrderAmount { get; set; }
    }

    public class VoucherApplicationRequest
    {
        public int UserId { get; set; }
        public int PromotionId { get; set; }
        public int? OrderId { get; set; }
        public decimal DiscountAmount { get; set; }
    }
} 


