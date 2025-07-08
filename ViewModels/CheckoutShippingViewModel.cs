using System.ComponentModel.DataAnnotations;
using JollibeeClone.Models;

namespace JollibeeClone.ViewModels
{
    public class CheckoutShippingViewModel
    {
        // Customer Information
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100)]
        public string CustomerFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [StringLength(20)]
        public string CustomerPhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255)]
        public string CustomerEmail { get; set; } = string.Empty;

        // Delivery Information
        [Required(ErrorMessage = "Vui lòng chọn phương thức vận chuyển")]
        public int DeliveryMethodID { get; set; }

        public int? UserAddressID { get; set; }
        public string? DeliveryAddress { get; set; }

        // Pickup Information (for store pickup)
        public int? StoreID { get; set; }
        public DateTime? PickupDate { get; set; }
        public TimeSpan? PickupTimeSlot { get; set; }

        // Order Notes
        public string? NotesByCustomer { get; set; }

        // Cart Data
        public List<CheckoutCartItemViewModel> CartItems { get; set; } = new List<CheckoutCartItemViewModel>();
        public decimal SubtotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // Available Options
        public List<DeliveryMethods> DeliveryMethods { get; set; } = new List<DeliveryMethods>();
        public List<Store> Stores { get; set; } = new List<Store>();
        public List<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();

        // Available Time Slots
        public List<TimeSlotOption> AvailableTimeSlots { get; set; } = new List<TimeSlotOption>
        {
            new TimeSlotOption { Value = new TimeSpan(8, 0, 0), Text = "08:00 - 09:00" },
            new TimeSlotOption { Value = new TimeSpan(9, 0, 0), Text = "09:00 - 10:00" },
            new TimeSlotOption { Value = new TimeSpan(10, 0, 0), Text = "10:00 - 11:00" },
            new TimeSlotOption { Value = new TimeSpan(11, 0, 0), Text = "11:00 - 12:00" },
            new TimeSlotOption { Value = new TimeSpan(14, 0, 0), Text = "14:00 - 15:00" },
            new TimeSlotOption { Value = new TimeSpan(15, 0, 0), Text = "15:00 - 16:00" },
            new TimeSlotOption { Value = new TimeSpan(16, 0, 0), Text = "16:00 - 17:00" },
            new TimeSlotOption { Value = new TimeSpan(17, 0, 0), Text = "17:00 - 18:00" },
            new TimeSlotOption { Value = new TimeSpan(18, 0, 0), Text = "18:00 - 19:00" },
            new TimeSlotOption { Value = new TimeSpan(19, 0, 0), Text = "19:00 - 20:00" }
        };

        // User Information (if logged in)
        public User? CurrentUser { get; set; }
        public bool IsUserLoggedIn { get; set; }
    }

    public class TimeSlotOption
    {
        public TimeSpan Value { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class CheckoutCartItemViewModel
    {
        public int CartItemID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public string? ConfigurationDescription { get; set; }
        public List<ConfigurationOptionDisplay>? ConfigurationOptions { get; set; }
    }

    public class ConfigurationOptionDisplay
    {
        public string GroupName { get; set; } = string.Empty;
        public string OptionName { get; set; } = string.Empty;
        public string? OptionImage { get; set; }
        public decimal PriceAdjustment { get; set; }
        public int Quantity { get; set; }
        public string? VariantName { get; set; }
    }
} 