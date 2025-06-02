using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using JollibeeClone.Areas.Admin.Models;

namespace JollibeeClone.Areas.Admin.ViewModels
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6-100 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Vui lòng chọn ít nhất một vai trò")]
        [Display(Name = "Vai trò")]
        public List<int> SelectedRoleIds { get; set; } = new();

        // For dropdown
        public List<SelectListItem> Roles { get; set; } = new();
    }

    public class UserEditViewModel
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ít nhất một vai trò")]
        [Display(Name = "Vai trò")]
        public List<int> SelectedRoleIds { get; set; } = new();

        // For dropdown
        public List<SelectListItem> Roles { get; set; } = new();
        public List<Role> CurrentRoles { get; set; } = new();
    }

    public class UserListViewModel
    {
        public List<User> Users { get; set; } = new();
        public string? SearchString { get; set; }
        public bool? StatusFilter { get; set; }
        public int? RoleFilter { get; set; }
        public DateTime? RegisteredFromDate { get; set; }
        public DateTime? RegisteredToDate { get; set; }
        public string? SortBy { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<SelectListItem> Roles { get; set; } = new();
    }

    public class UserDetailsViewModel
    {
        public User User { get; set; } = new();
        public List<Role> UserRoles { get; set; } = new();
        public List<UserAddress> UserAddresses { get; set; } = new();
        public List<Orders> RecentOrders { get; set; } = new();
        public UserStatistics Statistics { get; set; } = new();
        public List<UserActivity> RecentActivities { get; set; } = new();
    }

    public class UserStatistics
    {
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int PendingOrders { get; set; }
        public DateTime LastOrderDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public int DaysSinceRegistration { get; set; }
        public string CustomerSegment { get; set; } = string.Empty;
        public List<MonthlyOrderData> MonthlyOrders { get; set; } = new();
        public List<CategoryPreference> CategoryPreferences { get; set; } = new();
    }

    public class UserChangePasswordViewModel
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có từ 6-100 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmNewPassword { get; set; } = string.Empty;

        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;
    }

    public class UserRoleAssignmentViewModel
    {
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<RoleSelectionItem> Roles { get; set; } = new();
    }

    public class RoleSelectionItem
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public string? Description { get; set; }
    }

    public class UserActivity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Description { get; set; } = string.Empty;
        public ActivityType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class MonthlyOrderData
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MM/yyyy");
    }

    public class CategoryPreference
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public double Percentage { get; set; }
    }

    public class UserBulkActionViewModel
    {
        public List<int> SelectedUserIds { get; set; } = new();
        public string Action { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public bool SendNotification { get; set; }
    }

    public class UserExportViewModel
    {
        public string? SearchString { get; set; }
        public bool? StatusFilter { get; set; }
        public int? RoleFilter { get; set; }
        public DateTime? RegisteredFromDate { get; set; }
        public DateTime? RegisteredToDate { get; set; }
        public List<string> SelectedFields { get; set; } = new();
        public string ExportFormat { get; set; } = "Excel";
        public bool IncludeStatistics { get; set; }
        public bool IncludeAddresses { get; set; }
        public bool IncludeOrders { get; set; }
    }

    public enum ActivityType
    {
        Login,
        Logout,
        OrderPlaced,
        OrderCancelled,
        OrderCreated,
        ProfileUpdated,
        PasswordChanged,
        AddressAdded,
        AddressUpdated,
        AddressDeleted,
        RoleAssigned,
        RoleRemoved,
        AccountActivated,
        AccountDeactivated,
        ProductAdded,
        ProductUpdated,
        UserRegistered,
        CategoryCreated,
        Other
    }
} 