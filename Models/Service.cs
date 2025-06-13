using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JollibeeClone.Models
{
    public class Service
    {
        [Key]
        public int ServiceID { get; set; }

        [Required(ErrorMessage = "TÃªn dá»‹ch vá»¥ lÃ  báº¯t buá»™c")]
        [StringLength(255, ErrorMessage = "TÃªn dá»‹ch vá»¥ khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 255 kÃ½ tá»±")]
        [Display(Name = "TÃªn dá»‹ch vá»¥")]
        public string ServiceName { get; set; }

        [StringLength(500, ErrorMessage = "MÃ´ táº£ ngáº¯n khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 500 kÃ½ tá»±")]
        [Display(Name = "MÃ´ táº£ ngáº¯n")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Ná»™i dung")]
        [Column(TypeName = "nvarchar(max)")]
        public string? Content { get; set; }

        [Display(Name = "HÃ¬nh áº£nh")]
        [Column(TypeName = "nvarchar(max)")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Thá»© tá»± hiá»ƒn thá»‹")]
        [Range(0, int.MaxValue, ErrorMessage = "Thá»© tá»± hiá»ƒn thá»‹ pháº£i lÃ  sá»‘ dÆ°Æ¡ng")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Äang hoáº¡t Ä‘á»™ng")]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        [Display(Name = "áº¢nh táº£i lÃªn")]
        public IFormFile? ImageFile { get; set; }
    }
} 
