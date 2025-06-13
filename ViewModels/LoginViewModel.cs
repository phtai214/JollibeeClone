using System.ComponentModel.DataAnnotations;

namespace JollibeeClone.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email lÃ  báº¯t buá»™c")]
        [EmailAddress(ErrorMessage = "Email khÃ´ng há»£p lá»‡")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Máº­t kháº©u lÃ  báº¯t buá»™c")]
        [DataType(DataType.Password)]
        [Display(Name = "Máº­t kháº©u")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhá»› Ä‘Äƒng nháº­p")]
        public bool RememberMe { get; set; }
    }
} 

