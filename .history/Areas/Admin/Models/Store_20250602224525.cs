using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Areas.Admin.Models
{
    public class Store
    {
        [Key]
        public int StoreID { get; set; }

        [Required]
        [StringLength(150)]
        public string StoreName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string StreetAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Ward { get; set; }

        [Required]
        [StringLength(100)]
        public string District { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        public string? OpeningHours { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();
    }
}
