using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Areas.Admin.Models
{
    public class Promotion
    {
        [Key]
        public int PromotionID { get; set; }

        [Required]
        [StringLength(150)]
        public string PromotionName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(50)]
        public string? CouponCode { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderValue { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public int? MaxUses { get; set; }

        public int UsesCount { get; set; } = 0;

        public int? MaxUsesPerUser { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<PromotionProductScope> PromotionProductScopes { get; set; } = new List<PromotionProductScope>();
        public virtual ICollection<PromotionCategoryScope> PromotionCategoryScopes { get; set; } = new List<PromotionCategoryScope>();
        public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();
        public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();
    }
}
