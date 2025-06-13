using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Models
{
    public class PromotionCategoryScope
    {
        public int PromotionID { get; set; }
        public int CategoryID { get; set; }

        // Navigation properties
        public virtual Promotion Promotion { get; set; } = null!;
        public virtual Categories Category { get; set; } = null!;
    }
}

