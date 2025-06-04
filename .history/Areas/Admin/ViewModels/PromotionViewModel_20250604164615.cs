using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JollibeeClone.Areas.Admin.Models;

namespace JollibeeClone.Areas.Admin.ViewModels
{
    public class PromotionViewModel
    {
        public int PromotionID { get; set; }

        [Required(ErrorMessage = "Tên voucher là bắt buộc")]
        [StringLength(150, ErrorMessage = "Tên voucher không được vượt quá 150 ký tự")]
        public string PromotionName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Mã voucher không được vượt quá 50 ký tự")]
        public string? CouponCode { get; set; }

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        public string DiscountType { get; set; } = "Percentage";

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0.01, 100, ErrorMessage = "Giá trị giảm giá phải từ 0.01% đến 100%")]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu phải lớn hơn hoặc bằng 0")]
        public decimal? MinOrderValue { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        [Range(1, int.MaxValue, ErrorMessage = "Số lần sử dụng tối đa phải lớn hơn 0")]
        public int? MaxUses { get; set; }

        public int UsesCount { get; set; } = 0;

        [Range(1, int.MaxValue, ErrorMessage = "Số lần sử dụng tối đa mỗi user phải lớn hơn 0")]
        public int? MaxUsesPerUser { get; set; }

        public bool IsActive { get; set; } = true;

        // For managing product and category scopes
        public List<int> SelectedProductIds { get; set; } = new List<int>();
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();

        // For display purposes
        public List<Product> AvailableProducts { get; set; } = new List<Product>();
        public List<Categories> AvailableCategories { get; set; } = new List<Categories>();
        public List<Product> SelectedProducts { get; set; } = new List<Product>();
        public List<Categories> SelectedCategories { get; set; } = new List<Categories>();

        // Custom validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate <= StartDate)
            {
                yield return new ValidationResult("Ngày kết thúc phải sau ngày bắt đầu", new[] { nameof(EndDate) });
            }

            if (StartDate < DateTime.Today)
            {
                yield return new ValidationResult("Ngày bắt đầu không thể là ngày trong quá khứ", new[] { nameof(StartDate) });
            }

            if (!string.IsNullOrEmpty(CouponCode) && CouponCode.Contains(" "))
            {
                yield return new ValidationResult("Mã voucher không được chứa khoảng trắng", new[] { nameof(CouponCode) });
            }
        }
    }

    public class PromotionListViewModel
    {
        public List<PromotionDisplayViewModel> Promotions { get; set; } = new List<PromotionDisplayViewModel>();
        public int TotalCount { get; set; }
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public string? SortBy { get; set; }
    }

    public class PromotionDisplayViewModel
    {
        public int PromotionID { get; set; }
        public string PromotionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CouponCode { get; set; }
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? MaxUses { get; set; }
        public int UsesCount { get; set; }
        public int? MaxUsesPerUser { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
        public int CategoryCount { get; set; }
        public bool IsExpired => EndDate < DateTime.Today;
        public bool IsUpcoming => StartDate > DateTime.Today;
        public bool IsRunning => StartDate <= DateTime.Today && EndDate >= DateTime.Today && IsActive;
        public double UsagePercentage => MaxUses.HasValue ? (double)UsesCount / MaxUses.Value * 100 : 0;
    }
} 