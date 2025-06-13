using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JollibeeClone.Areas.Admin.Models
{
    public class Service
    {
        [Key]
        public int ServiceID { get; set; }

        [Required(ErrorMessage = "Tên dịch vụ là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên dịch vụ không được vượt quá 255 ký tự")]
        [Display(Name = "Tên dịch vụ")]
        public string ServiceName { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDescription { get; set; }

        [Display(Name = "Nội dung")]
        [Column(TypeName = "nvarchar(max)")]
        public string? Content { get; set; }

        [Display(Name = "Hình ảnh")]
        [Column(TypeName = "nvarchar(max)")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị phải là số dương")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        [Display(Name = "Ảnh tải lên")]
        public IFormFile? ImageFile { get; set; }
    }
} 