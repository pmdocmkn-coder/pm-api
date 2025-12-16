// Models/ActivityLog.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    public class ActivityLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Module { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; } // ✅ Nama: UserId (bukan UserID)

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }

        // Relasi - TAMBAHKAN ForeignKey Attribute
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}