using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JollibeeClone.Models
{
    public class News
    {
        [Key]
        public int NewsID { get; set; }

        [Required(ErrorMessage = "TiÃªu Ä‘á» lÃ  báº¯t buá»™c")]
        [StringLength(255, ErrorMessage = "TiÃªu Ä‘á» khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 255 kÃ½ tá»±")]
        [Display(Name = "TiÃªu Ä‘á»")]
        public string Title { get; set; }

        [StringLength(500, ErrorMessage = "MÃ´ táº£ ngáº¯n khÃ´ng Ä‘Æ°á»£c vÆ°á»£t quÃ¡ 500 kÃ½ tá»±")]
        [Display(Name = "MÃ´ táº£ ngáº¯n")]
        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "Ná»™i dung lÃ  báº¯t buá»™c")]
        [Display(Name = "Ná»™i dung")]
        [Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; }

        [Display(Name = "HÃ¬nh áº£nh")]
        [Column(TypeName = "nvarchar(max)")]
        public string? ImageUrl { get; set; }

        [Display(Name = "NgÃ y xuáº¥t báº£n")]
        [DataType(DataType.DateTime)]
        public DateTime PublishedDate { get; set; } = DateTime.Now;

        [Display(Name = "ÄÃ£ xuáº¥t báº£n")]
        public bool IsPublished { get; set; } = true;

        [Required(ErrorMessage = "Loáº¡i tin tá»©c lÃ  báº¯t buá»™c")]
        [StringLength(50)]
        [Display(Name = "Loáº¡i tin tá»©c")]
        public string NewsType { get; set; } = "News";

        [Display(Name = "TÃ¡c giáº£")]
        public int? AuthorID { get; set; }

        // Navigation properties
        [ForeignKey("AuthorID")]
        public virtual User? Author { get; set; }

        [NotMapped]
        [Display(Name = "áº¢nh táº£i lÃªn")]
        public IFormFile? ImageFile { get; set; }
    }
} 
