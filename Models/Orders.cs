using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Models
{
    public class Orders
    {
        public int OrderID { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderCode { get; set; }

        public int? UserID { get; set; }

        [Required(ErrorMessage = "Họ tên khách hàng là bắt buộc")]
        [StringLength(100)]
        public string CustomerFullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255)]
        public string CustomerEmail { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [StringLength(20)]
        public string CustomerPhoneNumber { get; set; }

        public int? UserAddressID { get; set; }
        public int? DeliveryMethodID { get; set; }
        public DateTime? PickupDate { get; set; }
        public TimeSpan? PickupTimeSlot { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubtotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public int OrderStatusID { get; set; }
        public int PaymentMethodID { get; set; }
        public int? PromotionID { get; set; }
        public int? StoreID { get; set; }

        public string? NotesByCustomer { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public UserAddress? UserAddress { get; set; }
        public OrderStatuses OrderStatus { get; set; }
        public PaymentMethods PaymentMethod { get; set; }
        public DeliveryMethods? DeliveryMethod { get; set; }
        public virtual Promotion? Promotion { get; set; }
        public Store? Store { get; set; }
        public ICollection<OrderItems> OrderItems { get; set; } = new List<OrderItems>();
        public virtual ICollection<Payments> Payments { get; set; } = new List<Payments>();
        public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();
    }
}

