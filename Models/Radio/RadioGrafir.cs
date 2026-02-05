using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    public class RadioGrafir
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string NoAsset { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string SerialNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? TypeRadio { get; set; }

        [MaxLength(50)]
        public string? Div { get; set; }

        [MaxLength(100)]
        public string? Dept { get; set; }

        [MaxLength(50)]
        public string? FleetId { get; set; }

        public DateTime? Tanggal { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CreatedBy")]
        public User? CreatedByUser { get; set; }

        [ForeignKey("UpdatedBy")]
        public User? UpdatedByUser { get; set; }

        // Linked radios
        public ICollection<RadioTrunking> TrunkingRadios { get; set; } = new List<RadioTrunking>();
        public ICollection<RadioConventional> ConventionalRadios { get; set; } = new List<RadioConventional>();
    }
}
