using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Models
{
    public class ProductConfigurationOption
    {
        [Key]
        public int ConfigOptionID { get; set; }

        [Required]
        public int ConfigGroupID { get; set; }

        [Required]
        public int OptionProductID { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAdjustment { get; set; } = 0.00m;

        public bool IsDefault { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        // Navigation properties
        public virtual ProductConfigurationGroup ConfigGroup { get; set; } = null!;
        public virtual Product OptionProduct { get; set; } = null!;
    }
}

