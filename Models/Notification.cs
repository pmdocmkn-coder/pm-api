using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        public int? RecipientUserId { get; set; }
        
        [MaxLength(100)]
        public string? RecipientRoleName { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required, MaxLength(500)]
        public string Message { get; set; } = null!;

        [MaxLength(50)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? LinkUrl { get; set; }

        public int? ReferenceId { get; set; }

        [MaxLength(100)]
        public string? ReferenceType { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}
