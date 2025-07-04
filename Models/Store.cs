using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace JollibeeClone.Models
{
    public class Store
    {
        [Key]
        public int StoreID { get; set; }

        [Required]
        [StringLength(150)]
        public string StoreName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string StreetAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Ward { get; set; }

        [Required]
        [StringLength(100)]
        public string District { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        public string? OpeningHours { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(9,6)")]
        public decimal? Longitude { get; set; }

        public string? ImageUrl { get; set; }

        [StringLength(500)]
        public string? GoogleMapsUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();
    }
}

