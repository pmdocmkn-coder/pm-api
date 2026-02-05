using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    public class RadioTrunkingHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RadioTrunkingId { get; set; }

        // Previous values
        [MaxLength(50)]
        public string? PreviousUnitNumber { get; set; }

        [MaxLength(100)]
        public string? PreviousDept { get; set; }

        [MaxLength(50)]
        public string? PreviousFleet { get; set; }

        // New values
        [MaxLength(50)]
        public string? NewUnitNumber { get; set; }

        [MaxLength(100)]
        public string? NewDept { get; set; }

        [MaxLength(50)]
        public string? NewFleet { get; set; }

        [Required]
        [MaxLength(50)]
        public string ChangeType { get; set; } = "Update"; // Create, Update, Transfer, Scrap

        public string? Notes { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public int? ChangedBy { get; set; }

        // Navigation properties
        [ForeignKey("RadioTrunkingId")]
        public RadioTrunking RadioTrunking { get; set; } = null!;

        [ForeignKey("ChangedBy")]
        public User? ChangedByUser { get; set; }
    }
}
