using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Data;
using JollibeeClone.Models;

namespace JollibeeClone.Controllers
{
    public class NewsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NewsController> _logger;

        public NewsController(AppDbContext context, ILogger<NewsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy tất cả tin tức đã xuất bản với NewsType = "News"
                var news = await _context.News
                    .Include(n => n.Author)
                    .Where(n => n.IsPublished && n.NewsType == "News")
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

                ViewBag.TotalNews = news.Count;
                return View(news);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading news for user view");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách tin tức.";
                return View(new List<object>());
            }
        }

        // API để lấy chi tiết tin tức
        [HttpGet]
        public async Task<IActionResult> GetNewsDetails(int id)
        {
            try
            {
                var news = await _context.News
                    .Include(n => n.Author)
                    .Where(n => n.NewsID == id && n.IsPublished && n.NewsType == "News")
                    .FirstOrDefaultAsync();

                if (news == null)
                {
                    return Json(new { success = false, message = "Tin tức không tồn tại." });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        news.NewsID,
                        news.Title,
                        news.ShortDescription,
                        news.Content,
                        news.ImageUrl,
                        news.PublishedDate,
                        AuthorName = news.Author?.FullName ?? "Admin"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting news details for ID: {NewsId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra." });
            }
        }
    }
} 