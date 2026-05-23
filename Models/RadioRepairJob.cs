using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pm.Enums;

namespace Pm.Models
{
    [Table("RadioRepairJobs")]
    public class RadioRepairJob
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string JobNumber { get; set; } = null!;

        [Required, MaxLength(50)]
        public string HelpdeskTicketNumber { get; set; } = null!;

        public int? RadioId { get; set; }
        [ForeignKey(nameof(RadioId))]
        public Radio? Radio { get; set; }

        [Required, MaxLength(100)]
        public string RadioSerialNumber { get; set; } = null!;

        [MaxLength(100)]
        public string? BatterySerialNumber { get; set; }

        [MaxLength(100)]
        public string? EquipmentName { get; set; }

        [MaxLength(100)]
        public string? UnitNumber { get; set; }

        [MaxLength(200)]
        public string? RadioOwnerLabel { get; set; }

        [MaxLength(100)]
        public string? OwnerDivision { get; set; }

        [MaxLength(100)]
        public string? OwnerDepartment { get; set; }

        [Required, MaxLength(2000)]
        public string DamageDescription { get; set; } = null!;

        public RadioRepairJobStatus Status { get; set; } = RadioRepairJobStatus.Received;

        public int AssignedTechnicianUserId { get; set; }
        [ForeignKey(nameof(AssignedTechnicianUserId))]
        public User AssignedTechnician { get; set; } = null!;

        public int OpenedByUserId { get; set; }
        [ForeignKey(nameof(OpenedByUserId))]
        public User OpenedBy { get; set; } = null!;

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public int? CurrentHandoverId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }

        public ICollection<RadioRepairJobStatusLog> StatusLogs { get; set; } = new List<RadioRepairJobStatusLog>();
        public ICollection<RadioHandover> Handovers { get; set; } = new List<RadioHandover>();
    }
}
