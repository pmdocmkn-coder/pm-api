using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("OperationalDocumentNotificationHistories")]
    public class OperationalDocumentNotificationHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OperationalDocumentId { get; set; }

        [ForeignKey("OperationalDocumentId")]
        public OperationalDocument? OperationalDocument { get; set; }

        [Required]
        public DateTime NotifiedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int DaysRemaining { get; set; }
    }
}
