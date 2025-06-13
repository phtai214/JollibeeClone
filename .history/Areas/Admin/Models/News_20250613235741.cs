using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JollibeeClone.Areas.Admin.Models
{
    public class News
    {
        [Key]
        public int NewsID { get; set; }

        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDescription { get; set; }

        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        [Display(Name = "Nội dung")]
        [Column(TypeName = "nvarchar(max)")]
        public string Content { get; set; }

        [Display(Name = "Hình ảnh")]
        [Column(TypeName = "nvarchar(max)")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Ngày xuất bản")]
        [DataType(DataType.DateTime)]
        public DateTime PublishedDate { get; set; } = DateTime.Now;

        [Display(Name = "Đã xuất bản")]
        public bool IsPublished { get; set; } = true;

        [Required(ErrorMessage = "Loại tin tức là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Loại tin tức")]
        public string NewsType { get; set; } = "News";

        [Display(Name = "Tác giả")]
        public int? AuthorID { get; set; }

        // Navigation properties
        [ForeignKey("AuthorID")]
        public virtual User? Author { get; set; }

        [NotMapped]
        [Display(Name = "Ảnh tải lên")]
        public IFormFile? ImageFile { get; set; }
    }
} 