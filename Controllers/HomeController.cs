using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JollibeeClone.Models;
using JollibeeClone.ViewModels;
using JollibeeClone.Data;

namespace JollibeeClone.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Lấy 4 tin tức mới nhất để hiển thị trên trang chủ
            var latestNews = await _context.News
                .Include(n => n.Author)
                .Where(n => n.IsPublished && n.NewsType == "News")
                .OrderByDescending(n => n.PublishedDate)
                .Take(4)
                .Select(n => new
                {
                    n.NewsID,
                    n.Title,
                    n.ShortDescription,
                    n.ImageUrl,
                    n.PublishedDate,
                    AuthorName = n.Author != null ? n.Author.FullName : "Admin"
                })
                .ToListAsync();

            ViewBag.LatestNews = latestNews;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading latest news for home page");
            ViewBag.LatestNews = new List<object>();
            return View();
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult AdminGuide()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
