using System.ComponentModel.DataAnnotations;

namespace JollibeeClone.ViewModels
{
    public class NewsPromotionViewModel
    {
        public int NewsID { get; set; }
        
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;
        
        [Display(Name = "Mô tả ngắn")]
        public string? ShortDescription { get; set; }
        
        [Display(Name = "Nội dung")]
        public string? Content { get; set; }
        
        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }
        
        [Display(Name = "Ngày xuất bản")]
        public DateTime PublishedDate { get; set; }
        
        [Display(Name = "Tác giả")]
        public string AuthorName { get; set; } = "Admin";
        
        [Display(Name = "Ngày hiển thị")]
        public string DisplayDate => PublishedDate.ToString("dd/MM/yyyy");
    }
} 