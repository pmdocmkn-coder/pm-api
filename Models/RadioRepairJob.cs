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

        /// <summary>Kunci internal unik (tiket::SN). Tidak ditampilkan ke pengguna.</summary>
        [Required, MaxLength(200)]
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

        public EquipmentTagType? EquipmentTagType { get; set; }

        /// <summary>
        /// Status garansi radio. Diisi saat serah terima Helpdesk → Teknisi.
        /// True = dalam garansi, False = tidak dalam garansi (default).
        /// </summary>
        public bool IsWarranty { get; set; } = false;

        [MaxLength(100)]
        public string? OriginFrom { get; set; }
        
        [MaxLength(2000)]
        public string? RepairDataDescription { get; set; }
        
        [MaxLength(100)]
        public string? RepairedByName { get; set; }
        
        [MaxLength(100)]
        public string? FrequencyError { get; set; }
        
        [MaxLength(100)]
        public string? AfReading { get; set; }
        
        [MaxLength(100)]
        public string? PowerReading { get; set; }
        
        [MaxLength(100)]
        public string? VoltageOutNoLoad { get; set; }
        
        [MaxLength(100)]
        public string? VoltageOutWithLoad { get; set; }
        
        [MaxLength(100)]
        public string? PhysicalCondition { get; set; }
        
        [MaxLength(100)]
        public string? DisplayCondition { get; set; }

        public RadioRepairJobStatus Status { get; set; } = RadioRepairJobStatus.Received;

        public int AssignedTechnicianUserId { get; set; }
        [ForeignKey(nameof(AssignedTechnicianUserId))]
        public User AssignedTechnician { get; set; } = null!;

        public int OpenedByUserId { get; set; }
        [ForeignKey(nameof(OpenedByUserId))]
        public User OpenedBy { get; set; } = null!;

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public int? WorkshopTechnicianId { get; set; }
        [ForeignKey(nameof(WorkshopTechnicianId))]
        public WorkshopTechnician? WorkshopTechnician { get; set; }

        public int? CurrentHandoverId { get; set; }

        /// <summary>
        /// Status custom dari supervisor (nullable).
        /// Jika terisi, status "efektif" yang ditampilkan adalah label custom ini.
        /// Status enum tetap InProgress sebagai induk saat job di status custom.
        /// </summary>
        public int? CustomStatusId { get; set; }
        [ForeignKey(nameof(CustomStatusId))]
        public RepairJobCustomStatus? CustomStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }

        public int AccumulatedProgressDurationMinutes { get; set; } = 0;
        public DateTime? CurrentProgressStartedAt { get; set; }

        public ICollection<RadioRepairJobStatusLog> StatusLogs { get; set; } = new List<RadioRepairJobStatusLog>();
        public ICollection<RadioHandover> Handovers { get; set; } = new List<RadioHandover>();
        [InverseProperty("RelatedRepairJob")]
        public ICollection<WarehousePartBorrow> PartBorrows { get; set; } = new List<WarehousePartBorrow>();
    }
}
