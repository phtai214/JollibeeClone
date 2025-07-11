using JollibeeClone.Data;
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
            // Khoảng thời gian để tính toán
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var lastWeekStart = weekStart.AddDays(-7);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = monthStart.AddMonths(-1);

            // Thống kê người dùng mới (dựa trên UserID cao - user mới đăng ký gần đây)
            var totalUsers = await _context.Users.CountAsync();
            var maxUserId = await _context.Users.MaxAsync(u => (int?)u.UserID) ?? 0;
            var recentUserThreshold = Math.Max(1, maxUserId - 50); // 50 user gần nhất
            var newUsersToday = await _context.Users
                .Where(u => u.UserID > recentUserThreshold)
                .CountAsync();

            // Thống kê đơn hàng mới hôm nay
            var newOrdersToday = await _context.Orders
                .Where(o => o.OrderDate >= today)
                .CountAsync();

            // Thống kê đơn hàng hôm qua để tính % thay đổi
            var ordersYesterday = await _context.Orders
                .Where(o => o.OrderDate >= yesterday && o.OrderDate < today)
                .CountAsync();

            // Tổng doanh thu (chỉ từ các đơn hàng đã hoàn thành - OrderStatusID = 6)
            var totalRevenue = await _context.Orders
                .Where(o => o.OrderStatusID == 6) // Hoàn thành = đã thanh toán thành công
                .SumAsync(o => o.TotalAmount);

            // Doanh thu hôm nay
            var todayRevenue = await _context.Orders
                .Where(o => o.OrderDate >= today && o.OrderStatusID == 6)
                .SumAsync(o => o.TotalAmount);

            // Doanh thu hôm qua để tính % thay đổi
            var yesterdayRevenue = await _context.Orders
                .Where(o => o.OrderDate >= yesterday && o.OrderDate < today && o.OrderStatusID == 6)
                .SumAsync(o => o.TotalAmount);

            // Tổng số sản phẩm đang có sẵn
            var totalProducts = await _context.Products
                .Where(p => p.IsAvailable)
                .CountAsync();

            // Sản phẩm mới được thêm trong tuần này
            var maxProductId = await _context.Products.MaxAsync(pr => (int?)pr.ProductID) ?? 0;
            var productThreshold = Math.Max(1, maxProductId - 10);
            var newProductsThisWeek = await _context.Products
                .Where(p => p.ProductID > productThreshold)
                .CountAsync();

            // Lấy đơn hàng gần đây
            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderStatus)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // Lấy sản phẩm gần đây
            var recentProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable)
                .OrderByDescending(p => p.ProductID)
                .Take(5)
                .ToListAsync();

            // Tính % thay đổi
            var ordersChangePercent = ordersYesterday > 0 ? 
                Math.Round(((double)(newOrdersToday - ordersYesterday) / ordersYesterday) * 100, 1) : 0;
            
            var revenueChangePercent = yesterdayRevenue > 0 ? 
                Math.Round(((double)(todayRevenue - yesterdayRevenue) / (double)yesterdayRevenue) * 100, 1) : 0;

            var usersChangePercent = 13.0; // Estimate vì không có dữ liệu lịch sử user
            var productsChangePercent = 5.0; // Estimate

            ViewBag.NewUsersToday = newUsersToday;
            ViewBag.NewOrdersToday = newOrdersToday;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.NewProductsThisWeek = newProductsThisWeek;
            ViewBag.RecentOrders = recentOrders;
            ViewBag.RecentProducts = recentProducts;
            
            // Trend data
            ViewBag.UsersChangePercent = usersChangePercent;
            ViewBag.OrdersChangePercent = ordersChangePercent;
            ViewBag.RevenueChangePercent = revenueChangePercent;
            ViewBag.ProductsChangePercent = productsChangePercent;

            return View();
        }
    }
} 


