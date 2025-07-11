using JollibeeClone.Data;
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
        private readonly AppDbContext _dbContext;

        public ApiController(AppDbContext context)
        {
            _dbContext = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats(string dateRange = "week")
        {
            try
            {
                // Get date range based on parameter
                var dateFilter = GetDateRange(dateRange);
                var startDate = dateFilter.StartDate;
                var endDate = dateFilter.EndDate;
                
                var today = DateTime.Today;
                var yesterday = today.AddDays(-1);
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var monthStart = new DateTime(today.Year, today.Month, 1);

                // Thống kê người dùng mới (dựa trên UserID cao - user mới đăng ký gần đây)
                var totalUsers = await _dbContext.Users.CountAsync();
                var maxUserId = await _dbContext.Users.MaxAsync(u => (int?)u.UserID) ?? 0;
                var recentUserThreshold = Math.Max(1, maxUserId - 50); // 50 user gần nhất
                var newUsersInRange = await _dbContext.Users
                    .Where(u => u.UserID > recentUserThreshold)
                    .CountAsync();

                // Thống kê đơn hàng trong khoảng thời gian được chọn
                var ordersInRange = await _dbContext.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .CountAsync();

                // Đơn hàng kỳ trước để tính % thay đổi
                var previousPeriod = GetDateRange(dateRange, true);
                var ordersPrevious = await _dbContext.Orders
                    .Where(o => o.OrderDate >= previousPeriod.StartDate && o.OrderDate <= previousPeriod.EndDate)
                    .CountAsync();

                // Tổng doanh thu trong khoảng thời gian (chỉ từ các đơn hàng đã hoàn thành - OrderStatusID = 6)
                var revenueInRange = await _dbContext.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.OrderStatusID == 6)
                    .SumAsync(o => o.TotalAmount);

                // Doanh thu kỳ trước để tính % thay đổi
                var revenuePrevious = await _dbContext.Orders
                    .Where(o => o.OrderDate >= previousPeriod.StartDate && o.OrderDate <= previousPeriod.EndDate && o.OrderStatusID == 6)
                    .SumAsync(o => o.TotalAmount);

                // Tổng số sản phẩm đang có sẵn
                var totalProducts = await _dbContext.Products
                    .Where(p => p.IsAvailable)
                    .CountAsync();

                // Sản phẩm mới được thêm trong tuần này
                var maxProductId = await _dbContext.Products.MaxAsync(pr => (int?)pr.ProductID) ?? 0;
                var productThreshold = Math.Max(1, maxProductId - 10);
                var newProductsThisWeek = await _dbContext.Products
                    .Where(p => p.ProductID > productThreshold)
                    .CountAsync();

                // Tính % thay đổi thực tế
                var ordersChangePercent = ordersPrevious > 0 ? 
                    Math.Round(((double)(ordersInRange - ordersPrevious) / ordersPrevious) * 100, 1) : 0;
                
                var revenueChangePercent = revenuePrevious > 0 ? 
                    Math.Round(((double)(revenueInRange - revenuePrevious) / (double)revenuePrevious) * 100, 1) : 0;

                // Estimate cho user và product vì không có dữ liệu lịch sử đầy đủ
                var usersChangePercent = 13.0; // Estimate
                var productsChangePercent = 5.0; // Estimate

                var stats = new
                {
                    success = true,
                    data = new
                    {
                        users = newUsersInRange,
                        orders = ordersInRange,
                        revenue = revenueInRange,
                        products = totalProducts,
                        trends = new
                        {
                            usersPercent = usersChangePercent,
                            ordersPercent = ordersChangePercent,
                            revenuePercent = revenueChangePercent,
                            productsPercent = productsChangePercent
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
                var recentOrders = await _dbContext.Orders
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
                var recentProducts = await _dbContext.Products
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

        [HttpGet]
        public async Task<IActionResult> SmartSearch(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Json(new { success = true, results = new List<object>() });
                }

                var results = new List<object>();
                query = query.Trim().ToLower();

                // Search Products
                var products = await _dbContext.Products
                    .Include(p => p.Category)
                    .Where(p => p.ProductName.ToLower().Contains(query) || 
                               (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(query)))
                    .Take(5)
                    .Select(p => new
                    {
                        id = p.ProductID,
                        title = p.ProductName,
                        subtitle = p.Category.CategoryName,
                        type = "product",
                        icon = "fas fa-hamburger",
                        url = $"/Admin/Product/Details/{p.ProductID}",
                        badge = p.Price.ToString("C0", new System.Globalization.CultureInfo("vi-VN"))
                    })
                    .ToListAsync();

                results.AddRange(products);

                // Search Categories
                var categories = await _dbContext.Categories
                    .Where(c => c.CategoryName.ToLower().Contains(query) || 
                               c.Description.ToLower().Contains(query))
                    .Take(3)
                    .Select(c => new
                    {
                        id = c.CategoryID,
                        title = c.CategoryName,
                        subtitle = c.Description ?? "Danh mục sản phẩm",
                        type = "category",
                        icon = "fas fa-list",
                        url = $"/Admin/Category/Details/{c.CategoryID}",
                        badge = ""
                    })
                    .ToListAsync();

                results.AddRange(categories);

                // Search Orders
                var orders = await _dbContext.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderStatus)
                    .Where(o => o.OrderCode.ToLower().Contains(query) || 
                               o.CustomerFullName.ToLower().Contains(query) ||
                               o.CustomerEmail.ToLower().Contains(query))
                    .Take(5)
                    .Select(o => new
                    {
                        id = o.OrderID,
                        title = $"Đơn hàng #{o.OrderCode}",
                        subtitle = $"{o.CustomerFullName} - {o.OrderStatus.StatusName}",
                        type = "order",
                        icon = "fas fa-shopping-cart",
                        url = $"/Admin/Order/Details/{o.OrderID}",
                        badge = o.TotalAmount.ToString("C0", new System.Globalization.CultureInfo("vi-VN"))
                    })
                    .ToListAsync();

                results.AddRange(orders);

                // Search Users
                var users = await _dbContext.Users
                    .Where(u => u.FullName.ToLower().Contains(query) || 
                               u.Email.ToLower().Contains(query))
                    .Take(3)
                    .Select(u => new
                    {
                        id = u.UserID,
                        title = u.FullName,
                        subtitle = u.Email,
                        type = "user",
                        icon = "fas fa-user",
                        url = $"/Admin/User/Details/{u.UserID}",
                        badge = u.IsActive ? "Hoạt động" : "Tạm khóa"
                    })
                    .ToListAsync();

                results.AddRange(users);

                // Search Stores
                var stores = await _dbContext.Stores
                    .Where(s => s.StoreName.ToLower().Contains(query) || 
                               s.StreetAddress.ToLower().Contains(query) ||
                               s.District.ToLower().Contains(query) ||
                               s.City.ToLower().Contains(query))
                    .Take(3)
                    .Select(s => new
                    {
                        id = s.StoreID,
                        title = s.StoreName,
                        subtitle = $"{s.StreetAddress}, {s.District}, {s.City}",
                        type = "store",
                        icon = "fas fa-store",
                        url = $"/Admin/Store/Details/{s.StoreID}",
                        badge = s.IsActive ? "Mở cửa" : "Đóng cửa"
                    })
                    .ToListAsync();

                results.AddRange(stores);

                // Add smart suggestions if no results
                if (!results.Any())
                {
                    var suggestions = new[]
                    {
                        new { 
                            id = 0, title = "Tạo sản phẩm mới", subtitle = "Thêm sản phẩm vào hệ thống", 
                            type = "suggestion", icon = "fas fa-plus", url = "/Admin/Product/Create", badge = "" 
                        },
                        new { 
                            id = 0, title = "Xem tất cả đơn hàng", subtitle = "Quản lý đơn hàng", 
                            type = "suggestion", icon = "fas fa-list", url = "/Admin/Order", badge = "" 
                        },
                        new { 
                            id = 0, title = "Quản lý danh mục", subtitle = "Xem và chỉnh sửa danh mục", 
                            type = "suggestion", icon = "fas fa-folder", url = "/Admin/Category", badge = "" 
                        }
                    };
                    results.AddRange(suggestions);
                }

                return Json(new
                {
                    success = true,
                    results = results.Take(12).ToList(),
                    total = results.Count
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

        private (DateTime StartDate, DateTime EndDate) GetDateRange(string dateRange, bool previousPeriod = false)
        {
            var now = DateTime.Now;
            var startDate = now.Date;
            var endDate = now.Date.AddDays(1).AddTicks(-1);

            switch (dateRange.ToLower())
            {
                case "today":
                    startDate = now.Date;
                    endDate = now.Date.AddDays(1).AddTicks(-1);
                    if (previousPeriod)
                    {
                        startDate = startDate.AddDays(-1);
                        endDate = endDate.AddDays(-1);
                    }
                    break;
                case "week":
                    startDate = now.Date.AddDays(-6);
                    endDate = now.Date.AddDays(1).AddTicks(-1);
                    if (previousPeriod)
                    {
                        startDate = startDate.AddDays(-7);
                        endDate = endDate.AddDays(-7);
                    }
                    break;
                case "month":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = startDate.AddMonths(1).AddTicks(-1);
                    if (previousPeriod)
                    {
                        startDate = startDate.AddMonths(-1);
                        endDate = endDate.AddMonths(-1);
                    }
                    break;
                case "quarter":
                    var quarter = (now.Month - 1) / 3 + 1;
                    startDate = new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
                    endDate = startDate.AddMonths(3).AddTicks(-1);
                    if (previousPeriod)
                    {
                        startDate = startDate.AddMonths(-3);
                        endDate = endDate.AddMonths(-3);
                    }
                    break;
                case "year":
                    startDate = new DateTime(now.Year, 1, 1);
                    endDate = startDate.AddYears(1).AddTicks(-1);
                    if (previousPeriod)
                    {
                        startDate = startDate.AddYears(-1);
                        endDate = endDate.AddYears(-1);
                    }
                    break;
            }

            return (startDate, endDate);
        }
    }
} 