using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace JollibeeClone.Models
{
    public class UserAddress
    {
        [Key]
        public int AddressID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Ghi chú không được vượt quá 255 ký tự")]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Display(Name = "Địa chỉ mặc định")]
        public bool IsDefault { get; set; } = false;

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();
    }
}

