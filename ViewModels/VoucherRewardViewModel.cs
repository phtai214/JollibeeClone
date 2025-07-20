using System.ComponentModel.DataAnnotations;

namespace JollibeeClone.ViewModels
{
    public class VoucherRewardViewModel
    {
        public bool HasRewardEligible { get; set; } = false;
        public bool IsNewRewardAchieved { get; set; } = false;
        public decimal CurrentSpending { get; set; } = 0;
        public decimal NextThreshold { get; set; } = 0;
        public decimal RequiredAmount { get; set; } = 0;
        public decimal AchievedThreshold { get; set; } = 0;
        public decimal VoucherPercentage { get; set; } = 0;
        public string VoucherCode { get; set; } = string.Empty;
        public int? GeneratedPromotionID { get; set; }
        public string ProgressBarClass { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; } = 0;
        public string StatusMessage { get; set; } = string.Empty;
        public string EncouragementMessage { get; set; } = string.Empty;
        public List<VoucherThresholdInfo> AllThresholds { get; set; } = new List<VoucherThresholdInfo>();
    }

    public class VoucherThresholdInfo
    {
        public decimal Threshold { get; set; }
        public decimal Percentage { get; set; }
        public bool IsAchieved { get; set; } = false;
        public bool IsCurrentTarget { get; set; } = false;
        public string Description => $"Chi tiêu {Threshold:N0}₫ → Nhận voucher {Percentage}%";
    }

    public class UserSpendingInfo
    {
        public decimal TotalSpending { get; set; }
        public int CompletedOrdersCount { get; set; }
        public DateTime CalculatedFrom { get; set; }
        public DateTime CalculatedTo { get; set; }
    }
} 