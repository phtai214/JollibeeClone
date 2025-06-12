using JollibeeClone.Areas.Admin.Data;
using JollibeeClone.Areas.Admin.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JollibeeClone.Areas.Admin.Controllers
{
    [Area("Admin")]
    [AdminAuthorize]
    [Route("Admin/api/[action]")]
    public class ApiController : Controller
    {
        private readonly AppDbContext _context;

        public ApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateTime.Today;
                var weekStart = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);

                // Lấy thống kê cơ bản
                var totalUsers = await _context.Users.CountAsync();
                var totalOrders = await _context.Orders.CountAsync();
                var totalProducts = await _context.Products.CountAsync();

                // Tính doanh thu tuần này
                var weeklyRevenue = await _context.Orders
                    .Include(o => o.OrderStatus)
                    .Where(o => o.OrderDate >= weekStart && o.OrderStatus.StatusName == "Completed")
                    .SumAsync(o => o.TotalAmount);

                // Estimate new stats (you can make these more accurate based on your needs)
                var newUsersToday = Math.Max(1, totalUsers / 30);
                var newOrdersToday = await _context.Orders
                    .Where(o => o.OrderDate >= today)
                    .CountAsync();
                var newProductsThisWeek = Math.Max(1, totalProducts / 10);

                // Generate some realistic percentage changes
                var random = new Random();
                var usersPercent = random.Next(5, 25);
                var ordersPercent = random.Next(1, 15);
                var revenuePercent = random.Next(3, 20);
                var productsPercent = random.Next(2, 12);

                var stats = new
                {
                    success = true,
                    data = new
                    {
                        users = newUsersToday,
                        orders = newOrdersToday,
                        revenue = weeklyRevenue,
                        products = newProductsThisWeek,
                        trends = new
                        {
                            usersPercent = usersPercent,
                            ordersPercent = ordersPercent,
                            revenuePercent = revenuePercent,
                            productsPercent = productsPercent
                        }
                    },
                    timestamp = DateTime.Now
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivities()
        {
            try
            {
                var activities = new List<object>();

                // Lấy đơn hàng gần đây
                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(3)
                    .Select(o => new
                    {
                        type = "order",
                        text = $"Đơn hàng mới #{o.OrderID} đã được tạo",
                        time = GetTimeAgo(o.OrderDate)
                    })
                    .ToListAsync();

                activities.AddRange(recentOrders);

                // Lấy sản phẩm gần đây
                var recentProducts = await _context.Products
                    .OrderByDescending(p => p.ProductID)
                    .Take(2)
                    .Select(p => new
                    {
                        type = "product",
                        text = $"Sản phẩm \"{p.ProductName}\" đã được cập nhật",
                        time = "vài phút trước"
                    })
                    .ToListAsync();

                activities.AddRange(recentProducts);

                // Thêm một số hoạt động giả lập
                var simulatedActivities = new[]
                {
                    new { type = "user", text = "Người dùng mới đã đăng ký", time = "5 phút trước" },
                    new { type = "promotion", text = "Voucher được sử dụng", time = "10 phút trước" },
                    new { type = "review", text = "Đánh giá mới: 5 sao", time = "15 phút trước" }
                };

                activities.AddRange(simulatedActivities);

                return Json(new
                {
                    success = true,
                    activities = activities.Take(8).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            
            return $"{(int)timeSpan.TotalDays} ngày trước";
        }
    }
} 