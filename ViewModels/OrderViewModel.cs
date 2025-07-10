using Microsoft.AspNetCore.Mvc.Rendering;
using JollibeeClone.Models;

namespace JollibeeClone.ViewModels
{
    // Admin Order ViewModels
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

    // User Order ViewModels
    public class UserOrderListViewModel
    {
        public List<UserOrderSummaryViewModel> Orders { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int PageIndex { get; set; } = 1; // Alias for CurrentPage
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int TotalOrdersCount { get; set; } // Total count of all orders
        public string? StatusFilter { get; set; }
        public string? CurrentStatusFilter { get; set; } // Current filter applied
        public bool HasOrders => Orders.Any();
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class UserOrderSummaryViewModel
    {
        public int OrderID { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public string DeliveryMethodName { get; set; } = string.Empty;
        public string? DeliveryAddress { get; set; }
        public int TotalItems { get; set; }
        public string FormattedOrderDate => OrderDate.ToString("dd/MM/yyyy HH:mm");
        public string FormattedTotalAmount => TotalAmount.ToString("N0") + " đ";
        
        // Order tracking status
        public string StatusColor { get; set; } = "#6c757d"; // Default gray
        public string StatusIcon { get; set; } = "fas fa-clock"; // Default clock icon
        public bool CanCancel { get; set; }
        public bool CanReorder { get; set; }
        
        // Estimated times
        public DateTime? EstimatedDeliveryTime { get; set; }
        public DateTime? PickupTime { get; set; }
    }

    public class UserOrderDetailViewModel
    {
        public int OrderID { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerFullName { get; set; } = string.Empty;
        public string CustomerPhoneNumber { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        
        // Order Status
        public string StatusName { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#6c757d";
        public string StatusIcon { get; set; } = "fas fa-clock";
        
        // Delivery Information
        public string DeliveryMethodName { get; set; } = string.Empty;
        public string? DeliveryAddress { get; set; }
        public string? StoreName { get; set; }
        public string? StoreAddress { get; set; }
        public DateTime? PickupDate { get; set; }
        public TimeSpan? PickupTimeSlot { get; set; }
        
        // Payment Information
        public string PaymentMethodName { get; set; } = string.Empty;
        public decimal SubtotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        
        // Order Items
        public List<UserOrderItemViewModel> OrderItems { get; set; } = new();
        
        // Notes and Timeline
        public string? NotesByCustomer { get; set; }
        public List<OrderTrackingEvent> TrackingEvents { get; set; } = new();
        
        // Actions
        public bool CanCancel { get; set; }
        public bool CanReorder { get; set; }
        
        // Formatted properties
        public string FormattedOrderDate => OrderDate.ToString("dd/MM/yyyy HH:mm");
        public string FormattedPickupTime => PickupDate?.ToString("dd/MM/yyyy") + 
            (PickupTimeSlot?.ToString(@"hh\:mm") != null ? " " + PickupTimeSlot?.ToString(@"hh\:mm") : "");
        public string FormattedSubtotal => SubtotalAmount.ToString("N0") + " đ";
        public string FormattedShippingFee => ShippingFee.ToString("N0") + " đ";
        public string FormattedDiscountAmount => DiscountAmount.ToString("N0") + " đ";
        public string FormattedTotalAmount => TotalAmount.ToString("N0") + " đ";
    }

    public class UserOrderItemViewModel
    {
        public int OrderItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
        public List<OrderItemConfigurationViewModel> ConfigurationOptions { get; set; } = new();
        public string FormattedUnitPrice => UnitPrice.ToString("N0") + " đ";
        public string FormattedSubtotal => Subtotal.ToString("N0") + " đ";
        public bool HasConfiguration => ConfigurationOptions.Any();
    }

    public class OrderItemConfigurationViewModel
    {
        public string GroupName { get; set; } = string.Empty;
        public string OptionName { get; set; } = string.Empty;
        public string? OptionImage { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAdjustment { get; set; }
        public string? VariantName { get; set; }
        public string? VariantType { get; set; }
        public string FormattedPrice => PriceAdjustment > 0 ? $"(+{PriceAdjustment:N0} đ)" : "";
    }

    public class OrderTrackingEvent
    {
        public DateTime EventDate { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public string EventIcon { get; set; } = "fas fa-circle";
        public string EventColor { get; set; } = "#6c757d";
        public bool IsCompleted { get; set; }
        public string FormattedEventDate => EventDate.ToString("dd/MM/yyyy HH:mm");
    }

    public class OrderSuccessViewModel
    {
        public Orders Order { get; set; } = new();
        public List<UserOrderItemViewModel> OrderItems { get; set; } = new();
        public string EstimatedDeliveryTime { get; set; } = string.Empty;
        public string CustomerSupportPhone { get; set; } = "1900-1533";
        public bool IsDelivery { get; set; }
        public bool IsPickup { get; set; }
    }
} 

