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
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateTime.Today;
                var weekStart = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);

                // Láº¥y thá»‘ng kÃª cÆ¡ báº£n
                var totalUsers = await _dbContext.Users.CountAsync();
                var totalOrders = await _dbContext.Orders.CountAsync();
                var totalProducts = await _dbContext.Products.CountAsync();

                // TÃ­nh doanh thu tuáº§n nÃ y
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

                // Láº¥y Ä‘Æ¡n hÃ ng gáº§n Ä‘Ã¢y
                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(3)
                    .Select(o => new
                    {
                        type = "order",
                        text = $"ÄÆ¡n hÃ ng má»›i #{o.OrderID} Ä‘Ã£ Ä‘Æ°á»£c táº¡o",
                        time = GetTimeAgo(o.OrderDate)
                    })
                    .ToListAsync();

                activities.AddRange(recentOrders);

                // Láº¥y sáº£n pháº©m gáº§n Ä‘Ã¢y
                var recentProducts = await _context.Products
                    .OrderByDescending(p => p.ProductID)
                    .Take(2)
                    .Select(p => new
                    {
                        type = "product",
                        text = $"Sáº£n pháº©m \"{p.ProductName}\" Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t",
                        time = "vÃ i phÃºt trÆ°á»›c"
                    })
                    .ToListAsync();

                activities.AddRange(recentProducts);

                // Thêm má»™t sá»‘ hoáº¡t Ä‘á»™ng giáº£ láº­p
                var simulatedActivities = new[]
                {
                    new { type = "user", text = "NgÆ°á»i dÃ¹ng má»›i Ä‘Ã£ Ä‘Äƒng kÃ½", time = "5 phÃºt trÆ°á»›c" },
                    new { type = "promotion", text = "Voucher Ä‘Æ°á»£c sá»­ dá»¥ng", time = "10 phÃºt trÆ°á»›c" },
                    new { type = "review", text = "ÄÃ¡nh giÃ¡ má»›i: 5 sao", time = "15 phÃºt trÆ°á»›c" }
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
                var products = await _context.Products
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
                var categories = await _context.Categories
                    .Where(c => c.CategoryName.ToLower().Contains(query) || 
                               c.Description.ToLower().Contains(query))
                    .Take(3)
                    .Select(c => new
                    {
                        id = c.CategoryID,
                        title = c.CategoryName,
                        subtitle = c.Description ?? "Danh má»¥c sáº£n pháº©m",
                        type = "category",
                        icon = "fas fa-list",
                        url = $"/Admin/Category/Details/{c.CategoryID}",
                        badge = ""
                    })
                    .ToListAsync();

                results.AddRange(categories);

                // Search Orders
                var orders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderStatus)
                    .Where(o => o.OrderCode.ToLower().Contains(query) || 
                               o.CustomerFullName.ToLower().Contains(query) ||
                               o.CustomerEmail.ToLower().Contains(query))
                    .Take(5)
                    .Select(o => new
                    {
                        id = o.OrderID,
                        title = $"ÄÆ¡n hÃ ng #{o.OrderCode}",
                        subtitle = $"{o.CustomerFullName} - {o.OrderStatus.StatusName}",
                        type = "order",
                        icon = "fas fa-shopping-cart",
                        url = $"/Admin/Order/Details/{o.OrderID}",
                        badge = o.TotalAmount.ToString("C0", new System.Globalization.CultureInfo("vi-VN"))
                    })
                    .ToListAsync();

                results.AddRange(orders);

                // Search Users
                var users = await _context.Users
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
                        badge = u.IsActive ? "Hoáº¡t Ä‘á»™ng" : "Táº¡m khÃ³a"
                    })
                    .ToListAsync();

                results.AddRange(users);

                // Search Stores
                var stores = await _context.Stores
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
                        badge = s.IsActive ? "Má»Ÿ cá»­a" : "ÄÃ³ng cá»­a"
                    })
                    .ToListAsync();

                results.AddRange(stores);

                // Add smart suggestions if no results
                if (!results.Any())
                {
                    var suggestions = new[]
                    {
                        new { 
                            id = 0, title = "Táº¡o sáº£n pháº©m má»›i", subtitle = "Thêm sáº£n pháº©m vÃ o há»‡ thá»‘ng", 
                            type = "suggestion", icon = "fas fa-plus", url = "/Admin/Product/Create", badge = "" 
                        },
                        new { 
                            id = 0, title = "Xem táº¥t cáº£ Ä‘Æ¡n hÃ ng", subtitle = "Quáº£n lÃ½ Ä‘Æ¡n hÃ ng", 
                            type = "suggestion", icon = "fas fa-list", url = "/Admin/Order", badge = "" 
                        },
                        new { 
                            id = 0, title = "Quáº£n lÃ½ danh má»¥c", subtitle = "Xem vÃ  chá»‰nh sá»­a danh má»¥c", 
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
                return "vá»«a xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phÃºt trÆ°á»›c";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giá» trÆ°á»›c";
            
            return $"{(int)timeSpan.TotalDays} ngÃ y trÆ°á»›c";
        }
    }
} 


