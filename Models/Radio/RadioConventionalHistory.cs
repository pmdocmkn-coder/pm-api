using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    public class RadioConventionalHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RadioConventionalId { get; set; }

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
        public string ChangeType { get; set; } = "Update";

        public string? Notes { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public int? ChangedBy { get; set; }

        // Navigation properties
        [ForeignKey("RadioConventionalId")]
        public RadioConventional RadioConventional { get; set; } = null!;

        [ForeignKey("ChangedBy")]
        public User? ChangedByUser { get; set; }
    }
}
