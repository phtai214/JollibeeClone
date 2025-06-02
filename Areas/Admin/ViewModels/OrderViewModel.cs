using Microsoft.AspNetCore.Mvc.Rendering;
using JollibeeClone.Areas.Admin.Models;

namespace JollibeeClone.Areas.Admin.ViewModels
{
    public class OrderViewModel
    {
        public List<Orders> Orders { get; set; } = new();
        public List<OrderStatuses> OrderStatuses { get; set; } = new();
        public string? SearchString { get; set; }
        public int? SelectedStatusId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class OrderUpdateStatusViewModel
    {
        public Orders Order { get; set; } = new();
        public int NewStatusId { get; set; }
        public string? StatusNote { get; set; }
        public List<SelectListItem> OrderStatuses { get; set; } = new();
    }

    public class OrderStatisticsViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int PendingOrders { get; set; }
        public List<OrderStatusCount> OrdersByStatus { get; set; } = new();
        public List<DailyRevenueData> DailyRevenue { get; set; } = new();
    }

    public class OrderStatusCount
    {
        public string StatusName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class DailyRevenueData
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
} 