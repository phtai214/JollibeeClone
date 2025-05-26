using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Areas.Admin.Models
{
    public class PromotionProductScope
    {
        public int PromotionID { get; set; }
        public int ProductID { get; set; }

        // Navigation properties
        public virtual Promotion Promotion { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
