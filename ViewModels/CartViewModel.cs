using System.ComponentModel.DataAnnotations;

namespace JollibeeClone.ViewModels
{
    public class CartViewModel
    {
        public Guid CartID { get; set; }
        public int? UserID { get; set; }
        public string? SessionID { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
        public string? PromotionCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }

    public class CartItemViewModel
    {
        public int CartItemID { get; set; }
        public Guid CartID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int Quantity { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsConfigurable { get; set; }
        public List<CartConfigurationViewModel> Configurations { get; set; } = new List<CartConfigurationViewModel>();
    }

    public class CartConfigurationViewModel
    {
        public string GroupName { get; set; } = string.Empty;
        public List<CartOptionViewModel> Options { get; set; } = new List<CartOptionViewModel>();
    }

    public class CartOptionViewModel
    {
        public int ConfigOptionID { get; set; }
        public int OptionProductID { get; set; }
        public string OptionProductName { get; set; } = string.Empty;
        public string? OptionProductImage { get; set; }
        public decimal PriceAdjustment { get; set; }
        public int Quantity { get; set; }
        public string? VariantName { get; set; }
        public string? VariantType { get; set; }
    }

    public class AddToCartRequest
    {
        [Required]
        public int ProductID { get; set; }
        
        [Required]
        [Range(1, 999)]
        public int Quantity { get; set; } = 1;
        
        public List<SelectedConfigurationOption> SelectedOptions { get; set; } = new List<SelectedConfigurationOption>();
        
        public string? SessionID { get; set; }
    }

    public class SelectedConfigurationOption
    {
        public int ConfigGroupID { get; set; }
        public int ConfigOptionID { get; set; }
        public int OptionProductID { get; set; }
        public int? VariantID { get; set; }
    }

    public class UpdateCartItemRequest
    {
        [Required]
        public int CartItemID { get; set; }
        
        [Required]
        [Range(1, 999)]
        public int Quantity { get; set; }
    }

    public class RemoveCartItemRequest
    {
        [Required]
        public int CartItemID { get; set; }
    }

    public class UpdateCartItemConfigurationRequest
    {
        [Required]
        public int CartItemID { get; set; }
        
        [Required]
        [Range(1, 999)]
        public int Quantity { get; set; }
        
        public List<SelectedConfigurationOption> SelectedOptions { get; set; } = new List<SelectedConfigurationOption>();
    }

    public class CartSummaryViewModel
    {
        public int TotalItems { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PromotionCode { get; set; }
        public string? PromotionDescription { get; set; }
    }
} 