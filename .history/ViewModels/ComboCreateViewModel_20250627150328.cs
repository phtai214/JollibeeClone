using System.ComponentModel.DataAnnotations;
using JollibeeClone.Models;

namespace JollibeeClone.ViewModels
{
    public class ComboCreateViewModel
    {
        // Thông tin combo
        [Required(ErrorMessage = "Tên combo là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên combo không được vượt quá 200 ký tự")]
        [Display(Name = "Tên combo")]
        public string ComboName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "Giá combo là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá combo")]
        public decimal ComboPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryID { get; set; }

        [Display(Name = "Ảnh combo")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Ảnh thumbnail")]
        public IFormFile? ThumbnailFile { get; set; }

        // Danh sách nhóm cấu hình
        public List<ComboGroupViewModel> ConfigGroups { get; set; } = new List<ComboGroupViewModel>();

        // Dropdown lists
        public List<Categories> Categories { get; set; } = new List<Categories>();
        public List<Product> AvailableProducts { get; set; } = new List<Product>();
    }

    public class ComboGroupViewModel
    {
        [Required(ErrorMessage = "Tên nhóm là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên nhóm không được vượt quá 100 ký tự")]
        [Display(Name = "Tên nhóm")]
        public string GroupName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Số lựa chọn tối thiểu phải lớn hơn 0")]
        [Display(Name = "Số lựa chọn tối thiểu")]
        public int MinSelections { get; set; } = 1;

        [Range(1, int.MaxValue, ErrorMessage = "Số lựa chọn tối đa phải lớn hơn 0")]
        [Display(Name = "Số lựa chọn tối đa")]
        public int MaxSelections { get; set; } = 1;

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        // Danh sách option trong nhóm
        public List<ComboOptionViewModel> Options { get; set; } = new List<ComboOptionViewModel>();
    }

    public class ComboOptionViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        [Display(Name = "Sản phẩm")]
        public int ProductID { get; set; }

        [Display(Name = "Biến thể")]
        public int? VariantID { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Giá cộng thêm")]
        public decimal PriceAdjustment { get; set; } = 0.00m;

        [Display(Name = "Ảnh custom")]
        public IFormFile? CustomImageFile { get; set; }

        [Display(Name = "Lựa chọn mặc định")]
        public bool IsDefault { get; set; } = false;

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        // Thông tin hiển thị
        public string? ProductName { get; set; }
        public string? VariantName { get; set; }
        public decimal ProductPrice { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? ProductThumbnailUrl { get; set; }

        // Dropdown data
        public List<ProductVariant> AvailableVariants { get; set; } = new List<ProductVariant>();
    }

    // ViewModel để hiển thị combo đã tạo
    public class ComboDetailViewModel
    {
        public Product ComboProduct { get; set; } = null!;
        public List<ComboGroupDetailViewModel> Groups { get; set; } = new List<ComboGroupDetailViewModel>();
    }

    public class ComboGroupDetailViewModel
    {
        public ProductConfigurationGroup Group { get; set; } = null!;
        public List<ComboOptionDetailViewModel> Options { get; set; } = new List<ComboOptionDetailViewModel>();
    }

    public class ComboOptionDetailViewModel
    {
        public ProductConfigurationOption Option { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public ProductVariant? Variant { get; set; }
        public string DisplayImageUrl => !string.IsNullOrEmpty(Option.CustomImageUrl) 
            ? Option.CustomImageUrl 
            : (!string.IsNullOrEmpty(Product.ThumbnailUrl) ? Product.ThumbnailUrl : Product.ImageUrl);
    }
} 