using JollibeeClone.Models;

namespace JollibeeClone.ViewModels
{
    public class DashboardViewModel
    {
        // Quick Stats
        public DashboardStats Stats { get; set; } = new();
        
        // Recent Activities
        public List<Orders> RecentOrders { get; set; } = new();
        public List<Product> RecentProducts { get; set; } = new();
        public List<User> RecentUsers { get; set; } = new();
        
        // Charts Data
        public List<ChartDataPoint> SalesChartData { get; set; } = new();
        public List<ChartDataPoint> OrdersChartData { get; set; } = new();
        public List<ChartDataPoint> UsersChartData { get; set; } = new();
        public List<ChartDataPoint> ProductsChartData { get; set; } = new();
        
        // Top Performers
        public List<TopProduct> TopSellingProducts { get; set; } = new();
        public List<TopCategory> TopCategories { get; set; } = new();
        
        // Alerts and Notifications
        public List<DashboardAlert> Alerts { get; set; } = new();
        public List<DashboardNotification> Notifications { get; set; } = new();
        
        // Real-time Data
        public RealTimeData RealTime { get; set; } = new();
    }

    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal DailyRevenue { get; set; }
        public int OnlineUsers { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockProducts { get; set; }
        
        // Growth Percentages
        public double UserGrowth { get; set; }
        public double OrderGrowth { get; set; }
        public double RevenueGrowth { get; set; }
        public double ProductGrowth { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class TopProduct
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public double GrowthRate { get; set; }
    }

    public class TopCategory
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public double MarketShare { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class DashboardAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlertType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Icon { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public bool IsRead { get; set; }
    }

    public class DashboardNotification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Icon { get; set; } = string.Empty;
        public string? ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public string? UserId { get; set; }
    }

    public class RealTimeData
    {
        public int OnlineUsers { get; set; }
        public int TodayOrders { get; set; }
        public decimal TodayRevenue { get; set; }
        public int NewProducts { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public List<RecentActivity> RecentActivities { get; set; } = new();
    }

    public class RecentActivity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Description { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public ActivityType Type { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Icon { get; set; } = string.Empty;
        public string? RelatedId { get; set; }
    }

    public enum NotificationType
    {
        Order,
        Product,
        User,
        System,
        Security
    }
} 

