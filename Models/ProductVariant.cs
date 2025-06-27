using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JollibeeClone.Models
{
    public class ProductVariant
    {
        [Key]
        public int VariantID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string VariantName { get; set; } = string.Empty; // VD: "Size lớn", "Thêm phô mai"

        [StringLength(50)]
        public string? VariantType { get; set; } // VD: "Size", "Topping"

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAdjustment { get; set; } = 0.00m; // Giá cộng thêm

        public bool IsDefault { get; set; } = false; // Biến thể mặc định

        public bool IsAvailable { get; set; } = true; // Có sẵn hay không

        public int DisplayOrder { get; set; } = 0; // Thứ tự hiển thị

        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual ICollection<ProductConfigurationOption> ProductConfigurationOptions { get; set; } = new List<ProductConfigurationOption>();
    }
} 