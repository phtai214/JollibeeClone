using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Models
{
    public class Payments
    {
        [Key]
        public int PaymentID { get; set; }

        [Required]
        public int OrderID { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public int PaymentMethodID { get; set; }

        [StringLength(255)]
        public string? TransactionCode { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = string.Empty;

        public string? Notes { get; set; }

        // Navigation properties
        public virtual Orders Order { get; set; } = null!;
        public virtual PaymentMethods PaymentMethod { get; set; } = null!;
    }
}

