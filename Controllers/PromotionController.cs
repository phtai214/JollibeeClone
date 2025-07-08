using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Models;
using JollibeeClone.ViewModels;

namespace JollibeeClone.Controllers
{
    public class PromotionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PromotionController> _logger;

        public PromotionController(AppDbContext context, ILogger<PromotionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Debug action để kiểm tra data
        [HttpGet]
        public async Task<IActionResult> Debug()
        {
            try
            {
                var allNews = await _context.News.ToListAsync();
                var promotionNews = await _context.News
                    .Where(n => n.NewsType == "Promotion")
                    .ToListAsync();
                
                var publishedPromotions = await _context.News
                    .Where(n => n.NewsType == "Promotion" && n.IsPublished)
                    .ToListAsync();

                return Json(new
                {
                    TotalNews = allNews.Count,
                    AllNewsTypes = allNews.Select(n => new { n.NewsID, n.Title, n.NewsType, n.IsPublished }).ToList(),
                    PromotionNewsCount = promotionNews.Count,
                    PromotionNewsList = promotionNews.Select(n => new { n.NewsID, n.Title, n.NewsType, n.IsPublished }).ToList(),
                    PublishedPromotionsCount = publishedPromotions.Count,
                    PublishedPromotionsList = publishedPromotions.Select(n => new { n.NewsID, n.Title, n.NewsType, n.IsPublished }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Debug action đơn giản hơn
        [HttpGet]
        public async Task<IActionResult> SimpleDebug()
        {
            var allNewsCount = await _context.News.CountAsync();
            var promotionCount = await _context.News.CountAsync(n => n.NewsType == "Promotion");
            var publishedPromotionCount = await _context.News.CountAsync(n => n.NewsType == "Promotion" && n.IsPublished);
            
            return Content($"Total News: {allNewsCount}, Promotion News: {promotionCount}, Published Promotions: {publishedPromotionCount}");
        }

        // Test action để force render view mới
        [HttpGet]
        public async Task<IActionResult> Test()
        {
            var promotions = await _context.News
                .Include(n => n.Author)
                .Where(n => n.IsPublished && n.NewsType == "Promotion")
                .OrderByDescending(n => n.PublishedDate)
                .Select(n => new
                {
                    n.NewsID,
                    n.Title,
                    n.ShortDescription,
                    n.Content,
                    n.ImageUrl,
                    n.PublishedDate,
                    AuthorName = n.Author != null ? n.Author.FullName : "Admin"
                })
                .ToListAsync();

            ViewBag.TotalPromotions = promotions.Count;
            ViewBag.IsTestPage = true;
            
            return Content($"<h1>TEST PAGE</h1><p>Found {promotions.Count} promotions</p><ul>" + 
                          string.Join("", promotions.Select(p => $"<li>{p.Title} - {p.PublishedDate}</li>")) + 
                          "</ul>", "text/html");
        }

        // Debug raw data 
        [HttpGet]
        public async Task<IActionResult> DebugData()
        {
            try
            {
                var news = await _context.News
                    .Include(n => n.Author)
                    .Where(n => n.NewsType == "Promotion")
                    .Select(n => new
                    {
                        n.NewsID,
                        n.Title,
                        n.ShortDescription,
                        n.Content,
                        n.ImageUrl,
                        n.PublishedDate,
                        n.IsPublished,
                        n.NewsType,
                        AuthorName = n.Author != null ? n.Author.FullName : "NULL_AUTHOR",
                        AuthorID = n.AuthorID,
                        HasContent = !string.IsNullOrEmpty(n.Content),
                        ContentLength = n.Content != null ? n.Content.Length : 0
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    count = news.Count,
                    data = news
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("=== PROMOTION INDEX ACTION CALLED ===");
                
                // Lấy tất cả khuyến mãi đã xuất bản với NewsType = "Promotion"
                var promotions = await _context.News
                    .Include(n => n.Author)
                    .Where(n => n.IsPublished && n.NewsType == "Promotion")
                    .OrderByDescending(n => n.PublishedDate)
                    .Select(n => new NewsPromotionViewModel
                    {
                        NewsID = n.NewsID,
                        Title = n.Title,
                        ShortDescription = n.ShortDescription,
                        Content = n.Content,
                        ImageUrl = n.ImageUrl,
                        PublishedDate = n.PublishedDate,
                        AuthorName = n.Author != null && !string.IsNullOrEmpty(n.Author.FullName) 
                            ? n.Author.FullName 
                            : "Admin"
                    })
                    .ToListAsync();

                ViewBag.TotalPromotions = promotions.Count;
                
                // Debug log
                _logger.LogInformation("=== Found {Count} promotions ===", promotions.Count);
                foreach (var promo in promotions)
                {
                    _logger.LogInformation("Promotion: ID={ID}, Title={Title}, Date={Date}, Author={Author}", 
                        promo.NewsID, promo.Title, promo.PublishedDate, promo.AuthorName);
                }
                
                return View(promotions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading promotions for user view");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách khuyến mãi.";
                return View(new List<NewsPromotionViewModel>());
            }
        }

        // API để lấy thông tin khuyến mãi để sử dụng trong cart/checkout
        [HttpGet]
        public async Task<IActionResult> GetPromotionDetails(int id)
        {
            try
            {
                _logger.LogInformation("=== GetPromotionDetails called with ID: {ID} ===", id);
                
                var promotion = await _context.News
                    .Include(n => n.Author)
                    .Where(n => n.NewsID == id && 
                               n.IsPublished && 
                               n.NewsType == "Promotion")
                    .FirstOrDefaultAsync();

                _logger.LogInformation("=== Query result: {Found} ===", promotion != null ? "FOUND" : "NOT FOUND");

                if (promotion == null)
                {
                    // Debug: check if news exists with different criteria
                    var anyNews = await _context.News.Where(n => n.NewsID == id).FirstOrDefaultAsync();
                    if (anyNews != null)
                    {
                        _logger.LogInformation("=== News exists but NewsType={Type}, IsPublished={Published} ===", 
                            anyNews.NewsType, anyNews.IsPublished);
                    }
                    else
                    {
                        _logger.LogInformation("=== No news found with ID: {ID} ===", id);
                    }
                    
                    return Json(new { success = false, message = "Khuyến mãi không tồn tại hoặc đã hết hạn." });
                }

                _logger.LogInformation("=== Returning promotion: {Title} ===", promotion.Title);

                // Fix Author null issue
                var authorName = promotion.Author != null && !string.IsNullOrEmpty(promotion.Author.FullName)
                    ? promotion.Author.FullName 
                    : "Admin";

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        promotion.NewsID,
                        promotion.Title,
                        promotion.ShortDescription,
                        promotion.Content,
                        promotion.ImageUrl,
                        promotion.PublishedDate,
                        AuthorName = authorName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting promotion details for ID: {PromotionId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }

        // API để validate mã coupon (nếu có hệ thống mã giảm giá)
        [HttpPost]
        public async Task<IActionResult> ValidateCouponCode([FromBody] ValidateCouponRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.CouponCode))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã giảm giá." });
                }

                // Tìm kiếm promotion với mã coupon trong content hoặc title
                var promotion = await _context.News
                    .Where(n => n.IsPublished && 
                               n.NewsType == "Promotion" &&
                               (n.Content.Contains(request.CouponCode) || 
                                n.Title.Contains(request.CouponCode)))
                    .FirstOrDefaultAsync();

                if (promotion == null)
                {
                    return Json(new { 
                        success = false, 
                        message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." 
                    });
                }

                // Giả sử có logic tính discount (có thể mở rộng sau)
                decimal discountAmount = request.OrderAmount * 0.1m; // 10% discount
                decimal maxDiscount = 50000m; // Tối đa 50,000 VND
                
                discountAmount = Math.Min(discountAmount, maxDiscount);

                return Json(new
                {
                    success = true,
                    message = "Mã giảm giá hợp lệ!",
                    data = new
                    {
                        promotion.NewsID,
                        promotion.Title,
                        CouponCode = request.CouponCode,
                        DiscountAmount = discountAmount,
                        DiscountPercent = 10,
                        MaxDiscount = maxDiscount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating coupon code: {CouponCode}", request.CouponCode);
                return Json(new { 
                    success = false, 
                    message = "Có lỗi xảy ra khi kiểm tra mã giảm giá." 
                });
            }
        }
    }

    // Request model cho validate coupon
    public class ValidateCouponRequest
    {
        public string CouponCode { get; set; } = string.Empty;
        public decimal OrderAmount { get; set; }
        public int? UserId { get; set; }
    }
} 