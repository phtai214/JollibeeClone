using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using JollibeeClone.Models;

namespace JollibeeClone.ViewModels
{
    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "TÃªn sáº£n pháº©m lÃ  báº¯t buá»™c")]
        [StringLength(200, ErrorMessage = "TÃªn sáº£n pháº©m khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 200 kÃ½ tá»±")]
        [Display(Name = "TÃªn sáº£n pháº©m")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "MÃ´ táº£ ngáº¯n khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 500 kÃ½ tá»±")]
        [Display(Name = "MÃ´ táº£ ngáº¯n")]
        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "GiÃ¡ sáº£n pháº©m lÃ  báº¯t buá»™c")]
        [Range(0.01, double.MaxValue, ErrorMessage = "GiÃ¡ sáº£n pháº©m pháº£i lá»›n hÆ¡n 0")]
        [Display(Name = "GiÃ¡ sáº£n pháº©m")]
        public decimal Price { get; set; }

        [Display(Name = "GiÃ¡ gá»‘c")]
        public decimal? OriginalPrice { get; set; }

        [Required(ErrorMessage = "Vui lÃ²ng chá»n danh má»¥c")]
        [Display(Name = "Danh má»¥c")]
        public int CategoryID { get; set; }

        [Display(Name = "CÃ³ thá»ƒ tÃ¹y chá»‰nh")]
        public bool IsConfigurable { get; set; }

        [Display(Name = "CÃ³ sáºµn")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "HÃ¬nh áº£nh")]
        public IFormFile? ImageFile { get; set; }

        // For dropdown
        public List<SelectListItem> Categories { get; set; } = new();
    }

    public class ProductEditViewModel : ProductCreateViewModel
    {
        public int ProductID { get; set; }
        
        [Display(Name = "HÃ¬nh áº£nh hiá»‡n táº¡i")]
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

