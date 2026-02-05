using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    public class RadioTrunking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string UnitNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Dept { get; set; }

        [MaxLength(50)]
        public string? Fleet { get; set; }

        [Required]
        [MaxLength(50)]
        public string RadioId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        public DateTime? DateProgram { get; set; }

        [MaxLength(100)]
        public string? RadioType { get; set; }

        [MaxLength(50)]
        public string? JobNumber { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        [MaxLength(100)]
        public string? Initiator { get; set; }

        [MaxLength(100)]
        public string? Firmware { get; set; }

        [MaxLength(500)]
        public string? ChannelApply { get; set; }

        // Foreign key to Grafir (optional)
        public int? GrafirId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("GrafirId")]
        public RadioGrafir? Grafir { get; set; }

        [ForeignKey("CreatedBy")]
        public User? CreatedByUser { get; set; }

        [ForeignKey("UpdatedBy")]
        public User? UpdatedByUser { get; set; }

        public ICollection<RadioTrunkingHistory> Histories { get; set; } = new List<RadioTrunkingHistory>();
    }
}
