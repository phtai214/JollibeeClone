using System.ComponentModel.DataAnnotations;

namespace JollibeeClone.ViewModels
{
    public class EditProfileViewModel
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Số điện thoại không thể thay đổi - chỉ hiển thị
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }

        [Display(Name = "Thành phố")]
        public string? City { get; set; }

        // Đổi mật khẩu
        [Display(Name = "Mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string? ConfirmPassword { get; set; }

        // Checkbox để xác định có muốn đổi mật khẩu không
        public bool ChangePassword { get; set; } = false;
    }
} 