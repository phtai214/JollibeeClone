using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Models
{
    public class UserAddress
    {
        [Key]
        public int AddressID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Address { get; set; }

        [StringLength(255)]
        public string? Note { get; set; }

        public bool IsDefault { get; set; } = false;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();
    }
}

