using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Areas.Admin.Models
{
    public class Product
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        public string ProductName { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự")]
        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }

        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryID { get; set; }

        public bool IsConfigurable { get; set; } = false;
        public bool IsAvailable { get; set; } = true;

        // Navigation properties
        public virtual Categories Category { get; set; } = null!;
        public virtual ICollection<ProductConfigurationGroup> ProductConfigurationGroups { get; set; } = new List<ProductConfigurationGroup>();
        public virtual ICollection<ProductConfigurationOption> ProductConfigurationOptions { get; set; } = new List<ProductConfigurationOption>();
        public virtual ICollection<PromotionProductScope> PromotionProductScopes { get; set; } = new List<PromotionProductScope>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItems> OrderItems { get; set; } = new List<OrderItems>();
    }
}
