using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JollibeeClone.Models
{
    public class UserRewardProgress
    {
        [Key]
        public int UserRewardProgressID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Mốc chi tiêu")]
        public decimal RewardThreshold { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tổng chi tiêu hiện tại")]
        public decimal CurrentSpending { get; set; } = 0;

        [Required]
        [Display(Name = "Đã nhận voucher")]
        public bool VoucherClaimed { get; set; } = false;

        [Display(Name = "Voucher ID đã sinh")]
        public int? GeneratedPromotionID { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime LastUpdatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Ngày nhận voucher")]
        public DateTime? VoucherClaimedDate { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("GeneratedPromotionID")]
        public virtual Promotion? GeneratedPromotion { get; set; }
    }
} 