using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JollibeeClone.Models;

namespace JollibeeClone.ViewModels
{
    public class PromotionViewModel : IValidatableObject
    {
        public int PromotionID { get; set; }

        [Required(ErrorMessage = "TÃªn voucher lÃ  báº¯t buá»™c")]
        [StringLength(150, ErrorMessage = "TÃªn voucher khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 150 kÃ½ tá»±")]
        [Display(Name = "TÃªn voucher")]
        public string PromotionName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "MÃ´ táº£ khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 500 kÃ½ tá»±")]
        [Display(Name = "MÃ´ táº£")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "MÃ£ voucher khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 50 kÃ½ tá»±")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "MÃ£ voucher chá»‰ Ä‘Æ°á»£c chá»©a chá»¯ hoa vÃ  sá»‘, khÃ´ng cÃ³ khoáº£ng tráº¯ng")]
        [Display(Name = "MÃ£ voucher")]
        public string? CouponCode { get; set; }

        [Required(ErrorMessage = "Loáº¡i giáº£m giÃ¡ lÃ  báº¯t buá»™c")]
        [Display(Name = "Loáº¡i giáº£m giÃ¡")]
        public string DiscountType { get; set; } = "Percentage";

        [Required(ErrorMessage = "GiÃ¡ trá»‹ giáº£m giÃ¡ lÃ  báº¯t buá»™c")]
        [Range(0.01, 100, ErrorMessage = "GiÃ¡ trá»‹ giáº£m giÃ¡ pháº£i tá»« 0.01% Ä‘áº¿n 100%")]
        [Display(Name = "GiÃ¡ trá»‹ giáº£m giÃ¡ (%)")]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "GiÃ¡ trá»‹ Ä‘Æ¡n hÃ ng tá»‘i thiá»ƒu pháº£i lá»›n hÆ¡n hoáº·c báº±ng 0")]
        [Display(Name = "GiÃ¡ trá»‹ Ä‘Æ¡n hÃ ng tá»‘i thiá»ƒu")]
        public decimal? MinOrderValue { get; set; }

        [Required(ErrorMessage = "NgÃ y báº¯t Ä‘áº§u lÃ  báº¯t buá»™c")]
        [DataType(DataType.DateTime)]
        [Display(Name = "NgÃ y báº¯t Ä‘áº§u")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "NgÃ y káº¿t thÃºc lÃ  báº¯t buá»™c")]
        [DataType(DataType.DateTime)]
        [Display(Name = "NgÃ y káº¿t thÃºc")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

        [Range(1, int.MaxValue, ErrorMessage = "Sá»‘ láº§n sá»­ dá»¥ng tá»‘i Ä‘a pháº£i lá»›n hÆ¡n 0")]
        [Display(Name = "Sá»‘ láº§n sá»­ dá»¥ng tá»‘i Ä‘a")]
        public int? MaxUses { get; set; }

        [Display(Name = "Sá»‘ láº§n Ä‘Ã£ sá»­ dá»¥ng")]
        public int UsesCount { get; set; } = 0;

        [Range(1, int.MaxValue, ErrorMessage = "Sá»‘ láº§n sá»­ dá»¥ng tá»‘i Ä‘a má»—i user pháº£i lá»›n hÆ¡n 0")]
        [Display(Name = "Sá»‘ láº§n sá»­ dá»¥ng tá»‘i Ä‘a má»—i ngÆ°á»i")]
        public int? MaxUsesPerUser { get; set; }

        [Display(Name = "Tráº¡ng thÃ¡i hoáº¡t Ä‘á»™ng")]
        public bool IsActive { get; set; } = true;

        // For managing product and category scopes
        public List<int> SelectedProductIds { get; set; } = new List<int>();
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();

        // For display purposes
        public List<Product> AvailableProducts { get; set; } = new List<Product>();
        public List<Categories> AvailableCategories { get; set; } = new List<Categories>();
        public List<Product> SelectedProducts { get; set; } = new List<Product>();
        public List<Categories> SelectedCategories { get; set; } = new List<Categories>();

        // Computed properties
        public bool IsExpired => EndDate < DateTime.Today;
        public bool IsUpcoming => StartDate > DateTime.Today;
        public bool IsRunning => StartDate <= DateTime.Today && EndDate >= DateTime.Today && IsActive;
        public double UsagePercentage => MaxUses.HasValue && MaxUses.Value > 0 ? (double)UsesCount / MaxUses.Value * 100 : 0;
        public int DaysRemaining => Math.Max(0, (EndDate - DateTime.Today).Days);
        public int TotalDays => (EndDate - StartDate).Days + 1;

        // Custom validation - YÃŠU Cáº¦U CHá»ŒN Cáº¢ PRODUCTS VÃ€ CATEGORIES
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndDate <= StartDate)
            {
                yield return new ValidationResult("NgÃ y káº¿t thÃºc pháº£i sau ngÃ y báº¯t Ä‘áº§u", new[] { nameof(EndDate) });
            }

            if (StartDate < DateTime.Today.AddDays(-1))
            {
                yield return new ValidationResult("NgÃ y báº¯t Ä‘áº§u khÃ´ng thá»ƒ quÃ¡ xa trong quÃ¡ khá»©", new[] { nameof(StartDate) });
            }

            if ((EndDate - StartDate).Days > 365)
            {
                yield return new ValidationResult("Thá»i gian hiá»‡u lá»±c voucher khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 365 ngÃ y", new[] { nameof(EndDate) });
            }

            if (MaxUsesPerUser.HasValue && MaxUses.HasValue && MaxUsesPerUser.Value > MaxUses.Value)
            {
                yield return new ValidationResult("Sá»‘ láº§n sá»­ dá»¥ng tá»‘i Ä‘a má»—i ngÆ°á»i khÃ´ng Ä‘Æ°á»£c lá»›n hÆ¡n tá»•ng sá»‘ láº§n sá»­ dá»¥ng", new[] { nameof(MaxUsesPerUser) });
            }

            if (DiscountValue < 0.01m)
            {
                yield return new ValidationResult("GiÃ¡ trá»‹ giáº£m giÃ¡ pháº£i Ã­t nháº¥t 0.01%", new[] { nameof(DiscountValue) });
            }

            if (DiscountValue > 100m)
            {
                yield return new ValidationResult("GiÃ¡ trá»‹ giáº£m giÃ¡ khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 100%", new[] { nameof(DiscountValue) });
            }

            // LOGIC CÅ¨: Báº®T BUá»˜C PHáº¢I CHá»ŒN Cáº¢ PRODUCTS VÃ€ CATEGORIES
            if (!SelectedProductIds.Any() || !SelectedCategoryIds.Any())
            {
                yield return new ValidationResult("Vui lÃ²ng chá»n Ã­t nháº¥t má»™t sáº£n pháº©m VÃ€ má»™t danh má»¥c Ä‘á»ƒ Ã¡p dá»¥ng voucher", new[] { nameof(SelectedProductIds), nameof(SelectedCategoryIds) });
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

