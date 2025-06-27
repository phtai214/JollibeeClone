using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Models
{
    public class ProductConfigurationOption
    {
        [Key]
        public int ConfigOptionID { get; set; }

        [Required]
        public int ConfigGroupID { get; set; }

        [Required]
        public int OptionProductID { get; set; }

        public int? VariantID { get; set; } // Biến thể sản phẩm (nếu có)

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; } = 1; // Số lượng cố định, không cho user chỉnh

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAdjustment { get; set; } = 0.00m;

        public string? CustomImageUrl { get; set; } // Ảnh custom cho option này (ưu tiên hơn thumbnail)

        public bool IsDefault { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        // Navigation properties
        public virtual ProductConfigurationGroup ConfigGroup { get; set; } = null!;
        public virtual Product OptionProduct { get; set; } = null!;
        public virtual ProductVariant? Variant { get; set; }
    }
}

