using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Models
{
    public class PaymentMethods
    {
        [Key]
        public int PaymentMethodID { get; set; }

        [Required]
        [StringLength(100)]
        public string MethodName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();
        public virtual ICollection<Payments> Payments { get; set; } = new List<Payments>();
    }
}

