using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JollibeeClone.Models
{
    public class UserPromotion
    {
        [Key]
        public int UserPromotionID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int PromotionID { get; set; }

        [Required]
        public DateTime UsedDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        public int? OrderID { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("PromotionID")]
        public virtual Promotion Promotion { get; set; } = null!;

        [ForeignKey("OrderID")]
        public virtual Orders? Order { get; set; }
    }
} 
