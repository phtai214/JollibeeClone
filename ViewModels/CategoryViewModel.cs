using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using JollibeeClone.Models;

namespace JollibeeClone.ViewModels
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "TÃªn danh má»¥c lÃ  báº¯t buá»™c")]
        [StringLength(100, ErrorMessage = "TÃªn danh má»¥c khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 100 kÃ½ tá»±")]
        [Display(Name = "TÃªn danh má»¥c")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "MÃ´ táº£ khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 500 kÃ½ tá»±")]
        [Display(Name = "MÃ´ táº£")]
        public string? Description { get; set; }

        [Display(Name = "Thá»© tá»± hiá»ƒn thá»‹")]
        [Range(0, int.MaxValue, ErrorMessage = "Thá»© tá»± hiá»ƒn thá»‹ pháº£i lÃ  sá»‘ dÆ°Æ¡ng")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Danh má»¥c cha")]
        public int? ParentCategoryID { get; set; }

        [Display(Name = "KÃ­ch hoáº¡t")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "HÃ¬nh áº£nh")]
        public IFormFile? ImageFile { get; set; }

        // For dropdown
        public List<SelectListItem> ParentCategories { get; set; } = new();
    }

    public class CategoryEditViewModel : CategoryCreateViewModel
    {
        public int CategoryID { get; set; }
        
        [Display(Name = "HÃ¬nh áº£nh hiá»‡n táº¡i")]
        public string? CurrentImageUrl { get; set; }
    }

    public class CategoryListViewModel
    {
        public List<Categories> Categories { get; set; } = new();
        public string? SearchString { get; set; }
        public bool? StatusFilter { get; set; }
        public int? ParentCategoryFilter { get; set; }
        public string? SortBy { get; set; }
        public List<SelectListItem> ParentCategories { get; set; } = new();
    }

    public class CategoryDetailsViewModel
    {
        public Categories Category { get; set; } = new();
        public List<Categories> SubCategories { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public decimal TotalRevenue { get; set; }
        public CategoryStatistics Statistics { get; set; } = new();
    }

    public class CategoryStatistics
    {
        public int TotalSubCategories { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public decimal AverageProductPrice { get; set; }
        public decimal HighestProductPrice { get; set; }
        public decimal LowestProductPrice { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class CategoryHierarchyViewModel
    {
        public List<CategoryTreeNode> CategoryTree { get; set; } = new();
    }

    public class CategoryTreeNode
    {
        public Categories Category { get; set; } = new();
        public List<CategoryTreeNode> Children { get; set; } = new();
        public int Level { get; set; }
        public int ProductCount { get; set; }
        public bool IsExpanded { get; set; }
    }
} 

