using JollibeeClone.Areas.Admin.Data;
using JollibeeClone.Areas.Admin.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy thống kê cho dashboard
            var totalCategories = await _context.Categories.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalUsers = await _context.Users.CountAsync();

            // Tính doanh thu tuần này
            var weekStart = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);
            var weeklyRevenue = await _context.Orders
                .Include(o => o.OrderStatus)
                .Where(o => o.OrderDate >= weekStart && o.OrderStatus.StatusName == "Completed")
                .SumAsync(o => o.TotalAmount);

            // Lấy đơn hàng gần đây
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // Lấy sản phẩm gần đây
            var recentProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.ProductID)
                .Take(5)
                .ToListAsync();

            // Thống kê người dùng mới hôm nay (estimate based on recent activity)
            var today = DateTime.Today;
            var newUsersToday = Math.Max(1, totalUsers / 30); // Simple estimate: total users / 30 days

            // Thống kê đơn hàng mới hôm nay
            var newOrdersToday = await _context.Orders
                .Where(o => o.OrderDate >= today)
                .CountAsync();

            // Thống kê sản phẩm mới tuần này (estimate)
            var newProductsThisWeek = Math.Max(1, totalProducts / 10); // Simple estimate

            ViewBag.TotalCategories = totalCategories;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.WeeklyRevenue = weeklyRevenue;
            ViewBag.NewUsersToday = newUsersToday;
            ViewBag.NewOrdersToday = newOrdersToday;
            ViewBag.NewProductsThisWeek = newProductsThisWeek;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.RecentProducts = recentProducts;

            return View();
        }
    }
} 