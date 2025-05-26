using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Areas.Admin.Models
{
    public class ProductConfigurationGroup
    {
        [Key]
        public int ConfigGroupID { get; set; }

        [Required]
        public int MainProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string GroupName { get; set; } = string.Empty;

        public int MinSelections { get; set; } = 1;

        public int MaxSelections { get; set; } = 1;

        public int DisplayOrder { get; set; } = 0;

        // Navigation properties
        public virtual Product MainProduct { get; set; } = null!;
        public virtual ICollection<ProductConfigurationOption> ProductConfigurationOptions { get; set; } = new List<ProductConfigurationOption>();
    }
}
