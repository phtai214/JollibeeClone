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
                .Where(o => o.OrderDate >= weekStart && o.OrderStatus == "Completed")
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

            // Thống kê người dùng mới hôm nay
            var today = DateTime.Today;
            var newUsersToday = await _context.Users
                .Where(u => u.CreatedAt >= today)
                .CountAsync();

            // Thống kê đơn hàng mới hôm nay
            var newOrdersToday = await _context.Orders
                .Where(o => o.OrderDate >= today)
                .CountAsync();

            // Thống kê sản phẩm mới tuần này
            var newProductsThisWeek = await _context.Products
                .Where(p => p.ProductID > 0) // Assuming ProductID is auto-increment
                .OrderByDescending(p => p.ProductID)
                .Take(totalProducts >= 10 ? 10 : totalProducts)
                .CountAsync();

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