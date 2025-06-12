using JollibeeClone.Areas.Admin.Models;

namespace JollibeeClone.Areas.Admin.ViewModels
{
    public class VoucherStatisticsViewModel
    {
        public List<UserVoucherUsageViewModel> UserUsages { get; set; } = new List<UserVoucherUsageViewModel>();
        public List<VoucherUsageStatsViewModel> VoucherStats { get; set; } = new List<VoucherUsageStatsViewModel>();
        public int TotalUsers { get; set; }
        public int TotalVouchersUsed { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public string? SearchTerm { get; set; }
        public string? FilterBy { get; set; } = "all"; // all, active, inactive
    }

    public class UserVoucherUsageViewModel
    {
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalVouchersUsed { get; set; }
        public decimal TotalSaved { get; set; }
        public DateTime? LastUsedDate { get; set; }
        public List<VoucherUsageDetailViewModel> UsageDetails { get; set; } = new List<VoucherUsageDetailViewModel>();
    }

    public class VoucherUsageDetailViewModel
    {
        public int UserPromotionID { get; set; }
        public string VoucherName { get; set; } = string.Empty;
        public string? CouponCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime UsedDate { get; set; }
        public string? OrderCode { get; set; }
        public int? OrderID { get; set; }
    }

    public class VoucherUsageStatsViewModel
    {
        public int PromotionID { get; set; }
        public string PromotionName { get; set; } = string.Empty;
        public string? CouponCode { get; set; }
        public int TotalUses { get; set; }
        public int MaxUses { get; set; }
        public int UniqueUsers { get; set; }
        public decimal TotalDiscountGiven { get; set; }
        public decimal UsagePercentage { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
} 