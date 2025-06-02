using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using JollibeeClone.Areas.Admin.Models;

namespace JollibeeClone.Areas.Admin.ViewModels
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "Tên danh mục là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị phải là số dương")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Danh mục cha")]
        public int? ParentCategoryID { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Hình ảnh")]
        public IFormFile? ImageFile { get; set; }

        // For dropdown
        public List<SelectListItem> ParentCategories { get; set; } = new();
    }

    public class CategoryEditViewModel : CategoryCreateViewModel
    {
        public int CategoryID { get; set; }
        
        [Display(Name = "Hình ảnh hiện tại")]
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