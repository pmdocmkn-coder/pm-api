using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("OperationalDocuments")]
    public class OperationalDocument
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public required string Name { get; set; }

        [Required, MaxLength(100)]
        public required string Type { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Nama grup opsional. Dokumen dengan GroupName sama + ValidUntil sama
        /// akan digabung menjadi 1 notifikasi WA (grouped notification).
        /// Contoh: "ISR Link Backbone 2027 KPC"
        /// </summary>
        [MaxLength(200)]
        public string? GroupName { get; set; }


        [Required]
        public DateTime ValidFrom { get; set; }

        [Required]
        public DateTime ValidUntil { get; set; }

        [MaxLength(255)]
        public string? PicName { get; set; }

        [MaxLength(200)]
        public string? PicTelegramId { get; set; }

        [MaxLength(1000)]
        public string? FileLink { get; set; }

        [Required, MaxLength(50)]
        public string FollowUpStatus { get; set; } = "Tidak Ada";

        public string? FollowUpRemark { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<OperationalDocumentNotificationHistory> NotificationHistories { get; set; } = [];
    }
}
