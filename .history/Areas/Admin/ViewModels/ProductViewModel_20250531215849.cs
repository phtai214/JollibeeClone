using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using JollibeeClone.Areas.Admin.Models;

namespace JollibeeClone.Areas.Admin.ViewModels
{
    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        [Display(Name = "Giá sản phẩm")]
        public decimal Price { get; set; }

        [Display(Name = "Giá gốc")]
        public decimal? OriginalPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryID { get; set; }

        [Display(Name = "Có thể tùy chỉnh")]
        public bool IsConfigurable { get; set; }

        [Display(Name = "Có sẵn")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Hình ảnh")]
        public IFormFile? ImageFile { get; set; }

        // For dropdown
        public List<SelectListItem> Categories { get; set; } = new();
    }

    public class ProductEditViewModel : ProductCreateViewModel
    {
        public int ProductID { get; set; }
        
        [Display(Name = "Hình ảnh hiện tại")]
        public string? CurrentImageUrl { get; set; }
    }

    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new();
        public string? SearchString { get; set; }
        public int? CategoryFilter { get; set; }
        public bool? AvailabilityFilter { get; set; }
        public string? PriceRange { get; set; }
        public string? SortBy { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<SelectListItem> Categories { get; set; } = new();
    }

    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = new();
        public List<ProductConfigurationGroup> ConfigurationGroups { get; set; } = new();
        public int TotalOrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
} 