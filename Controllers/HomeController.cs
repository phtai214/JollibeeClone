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
            // Test connection và lấy số lượng tin tức
            var totalCount = await _context.News.CountAsync();
            var publishedCount = await _context.News.Where(n => n.IsPublished).CountAsync();
            
            _logger.LogInformation($"Database connection OK. Total news: {totalCount}, Published: {publishedCount}");

            // Lấy tin tức đơn giản nhất
            var allNews = await _context.News.ToListAsync();
            var newsForView = allNews
                .Where(n => n.IsPublished)
                .OrderByDescending(n => n.PublishedDate)
                .Take(4)
                .Select(n => new
                {
                    NewsID = n.NewsID,
                    Title = n.Title ?? "Không có tiêu đề",
                    ShortDescription = n.ShortDescription ?? "Không có mô tả",
                    ImageUrl = n.ImageUrl ?? "",
                    PublishedDate = n.PublishedDate,
                    AuthorName = "Admin"
                }).ToList();

            ViewBag.LatestNews = newsForView;
            ViewBag.TotalNewsInDB = totalCount;
            ViewBag.PublishedNewsInDB = publishedCount;
            
            _logger.LogInformation($"Sending {newsForView.Count} news items to view");
            
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading news: {Message}", ex.Message);
            ViewBag.LatestNews = new List<object>();
            ViewBag.ErrorMessage = ex.Message;
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

        public IActionResult CartDebug()
        {
            return View();
        }

        public IActionResult AnonymousCartDebug()
        {
            return View();
        }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
