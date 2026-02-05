using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    public class RadioScrap
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ScrapCategory { get; set; } = "Trunking"; // Trunking, Conventional

        [MaxLength(100)]
        public string? TypeRadio { get; set; }

        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        [MaxLength(50)]
        public string? JobNumber { get; set; }

        [Required]
        public DateTime DateScrap { get; set; }

        public string? Remarks { get; set; }

        // Source references (nullable - where this scrap came from)
        public int? SourceTrunkingId { get; set; }
        public int? SourceConventionalId { get; set; }
        public int? SourceGrafirId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("SourceTrunkingId")]
        public RadioTrunking? SourceTrunking { get; set; }

        [ForeignKey("SourceConventionalId")]
        public RadioConventional? SourceConventional { get; set; }

        [ForeignKey("SourceGrafirId")]
        public RadioGrafir? SourceGrafir { get; set; }

        [ForeignKey("CreatedBy")]
        public User? CreatedByUser { get; set; }
    }
}
