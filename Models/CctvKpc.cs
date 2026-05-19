using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("CctvKpcs")]
    public class CctvKpc
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Severity level: "Low", "Medium", "High"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "Low";

        [Required]
        [MaxLength(200)]
        public string Camera { get; set; } = null!;

        [MaxLength(50)]
        public string? IpCamera { get; set; }

        [MaxLength(200)]
        public string? Model { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(500)]
        public string? ExplicitLocation { get; set; }

        [MaxLength(1000)]
        public string? FotoKoordinat { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
