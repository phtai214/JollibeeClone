using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Areas.Admin.Models
{
    public class DeliveryMethods
        {
        [Key]
        public int DeliveryMethodID { get; set; }

        [Required]
        [StringLength(100)]
        public string MethodName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();

    }
}

