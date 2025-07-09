using System.ComponentModel.DataAnnotations;
using JollibeeClone.Models;

namespace JollibeeClone.ViewModels
{
    public class CheckoutViewModel
    {
        // Order Information from Shipping
        public string CustomerFullName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhoneNumber { get; set; } = string.Empty;
        public string? DeliveryAddress { get; set; }
        public int? UserAddressID { get; set; }
        public string? UserAddressLabel { get; set; } // Label của địa chỉ (Nhà, Công ty, etc.)
        public string? FullDeliveryAddress { get; set; } // Địa chỉ đầy đủ để hiển thị
        public int DeliveryMethodID { get; set; }
        public string DeliveryMethodName { get; set; } = string.Empty;
        public int? StoreID { get; set; }
        public string? StoreName { get; set; }
        public string? StoreAddress { get; set; }
        public DateTime? PickupDate { get; set; }
        public TimeSpan? PickupTimeSlot { get; set; }
        public string? NotesByCustomer { get; set; }

        // Payment Information
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public int PaymentMethodID { get; set; }
        public List<PaymentMethods> PaymentMethods { get; set; } = new List<PaymentMethods>();

        // Cart Items
        public List<CheckoutCartItemViewModel> CartItems { get; set; } = new List<CheckoutCartItemViewModel>();

        // Pricing Information
        public decimal SubtotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // Voucher Information
        public string? VoucherCode { get; set; }
        public int? AppliedPromotionID { get; set; }
        public string? AppliedPromotionName { get; set; }
        public bool HasVoucherApplied => AppliedPromotionID.HasValue;

        // Available Vouchers for User
        public List<AvailableVoucherViewModel> AvailableVouchers { get; set; } = new List<AvailableVoucherViewModel>();

        // User Information
        public int? UserID { get; set; }
        public bool IsUserLoggedIn { get; set; }

        // Calculated Properties
        public decimal FinalAmount => SubtotalAmount + ShippingFee - DiscountAmount;
        public bool IsDelivery => DeliveryMethodName?.Contains("giao hàng") == true || DeliveryMethodName?.Contains("ship") == true;
        public bool IsPickup => !IsDelivery;
        public string DisplayDeliveryMethod => IsDelivery ? "Giao hàng tận nơi" : "Nhận tại cửa hàng";
        
        // Address display logic
        public string GetDisplayAddress()
        {
            if (!string.IsNullOrEmpty(FullDeliveryAddress))
                return FullDeliveryAddress;
            return DeliveryAddress ?? "";
        }
        
        // Time slot display logic
        public string GetDisplayTimeSlot()
        {
            if (!PickupTimeSlot.HasValue)
                return "";
                
            var startTime = PickupTimeSlot.Value;
            var endTime = startTime.Add(TimeSpan.FromHours(1)); // Add 1 hour
            
            return $"{startTime:hh\\:mm} - {endTime:hh\\:mm}";
        }
        
        // Pickup date display logic with relative day names
        public string GetDisplayPickupDate()
        {
            if (!PickupDate.HasValue)
                return "";
                
            var pickupDate = PickupDate.Value.Date;
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            
            var dayDifference = (pickupDate - today).Days;
            var dateString = pickupDate.ToString("dd/MM/yyyy");
            
            if (dayDifference == 0)
            {
                return $"Hôm nay, {dateString}";
            }
            else if (dayDifference == 1)
            {
                return $"Ngày mai, {dateString}";
            }
            else if (dayDifference >= 2 && dayDifference <= 7)
            {
                var daysOfWeek = new[] { "Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy" };
                var dayName = daysOfWeek[(int)pickupDate.DayOfWeek];
                return $"{dayName}, {dateString}";
            }
            else
            {
                // For dates further in the future, just show the date
                return dateString;
            }
        }
    }

    public class AvailableVoucherViewModel
    {
        public int PromotionID { get; set; }
        public string PromotionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CouponCode { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public DateTime EndDate { get; set; }
        public bool CanApply { get; set; }
        public string? CannotApplyReason { get; set; }
        public decimal EstimatedDiscount { get; set; }
    }

    public class ApplyVoucherRequest
    {
        [Required(ErrorMessage = "Mã voucher là bắt buộc")]
        public string VoucherCode { get; set; } = string.Empty;
        public decimal OrderAmount { get; set; }
        public int? UserID { get; set; }
    }

    public class ApplyVoucherResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? PromotionID { get; set; }
        public string? PromotionName { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NewTotalAmount { get; set; }
    }
} 