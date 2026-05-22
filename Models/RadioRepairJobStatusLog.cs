using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pm.Enums;

namespace Pm.Models
{
    [Table("RadioRepairJobStatusLogs")]
    public class RadioRepairJobStatusLog
    {
        [Key]
        public int Id { get; set; }

        public int JobId { get; set; }
        [ForeignKey(nameof(JobId))]
        public RadioRepairJob Job { get; set; } = null!;

        public RadioRepairJobStatus? FromStatus { get; set; }
        public RadioRepairJobStatus ToStatus { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public DateTime At { get; set; } = DateTime.UtcNow;
    }
}
