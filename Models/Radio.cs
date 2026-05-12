using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("Radios")]
    public class Radio
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// "Internal", "Contractor", "Unit", or "LegacyScrap"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = null!;

        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Radio Type or LV Type
        /// </summary>
        [MaxLength(100)]
        public string? Type { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Division { get; set; }

        [MaxLength(100)]
        public string? Company { get; set; }

        [MaxLength(100)]
        public string? Channel { get; set; }

        public DateTime? Tanggal { get; set; }

        [MaxLength(100)]
        public string? NomorAset { get; set; }

        [MaxLength(100)]
        public string? NomorUnit { get; set; }

        [MaxLength(100)]
        public string? NomorLv { get; set; }

        public bool IsTrunking { get; set; }
        public bool IsConventional { get; set; }

        [MaxLength(200)]
        public string? Fleet { get; set; }

        [MaxLength(100)]
        public string? RadioId { get; set; }

        // Scrap details
        public bool IsScrap { get; set; }

        [MaxLength(100)]
        public string? ScrapJobNumber { get; set; }

        public DateTime? DateScrapped { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }
        
        [MaxLength(1000)]
        public string? Mark { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
