using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JollibeeClone.Models
{
    public class OrderStatusHistory
    {
        [Key]
        public int OrderStatusHistoryID { get; set; }

        [Required]
        public int OrderID { get; set; }
        
        [Required]
        public int OrderStatusID { get; set; }
        
        [Required]
        public DateTime UpdatedAt { get; set; }
        
        [StringLength(50)]
        public string? UpdatedBy { get; set; } // "Admin", "System", hoặc AdminID
        
        [StringLength(500)]
        public string? Note { get; set; } // Ghi chú cho việc thay đổi trạng thái
        
        // Navigation Properties
        [ForeignKey("OrderID")]
        public virtual Orders Order { get; set; }
        
        [ForeignKey("OrderStatusID")]
        public virtual OrderStatuses OrderStatus { get; set; }
    }
} 