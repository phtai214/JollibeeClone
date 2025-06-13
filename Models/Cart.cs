using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Models
{
    public class Cart
    {
        [Key]
        public Guid CartID { get; set; } = Guid.NewGuid();

        public int? UserID { get; set; }

        [StringLength(255)]
        public string? SessionID { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime LastUpdatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}

