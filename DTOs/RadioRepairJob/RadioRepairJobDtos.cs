using System.ComponentModel.DataAnnotations;
using Pm.DTOs.Common;
using Pm.Enums;

namespace Pm.DTOs.RadioRepairJob
{
    public class RadioRepairJobQueryDto : BaseQueryDto
    {
        public string? Status { get; set; }
        public int? TechnicianUserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        /// <summary>Hanya dengan permission radio.repair.view.archive.</summary>
        public bool IncludeDeleted { get; set; }
    }

    public class RadioRepairJobListDto
    {
        public int Id { get; set; }
        public string HelpdeskTicketNumber { get; set; } = null!;
        public string RadioSerialNumber { get; set; } = null!;
        public int? RadioId { get; set; }
        /// <summary>ID Radio dari master (kolom RadioId di tabel Radios).</summary>
        public string? RadioMasterRadioId { get; set; }
        public string? RadioFleet { get; set; }
        public string? RadioCategory { get; set; }
        public string? EquipmentName { get; set; }
        public string? UnitNumber { get; set; }
        public string? RadioOwnerLabel { get; set; }
        public string? OwnerDivision { get; set; }
        public string? OwnerDepartment { get; set; }
        public string? PreviewPhotoBase64 { get; set; }
        public string DamageDescription { get; set; } = null!;
        public string? EquipmentTagType { get; set; }
        public string? OriginFrom { get; set; }
        public string? RepairDataDescription { get; set; }
        public string? RepairedByName { get; set; }
        public string? FrequencyError { get; set; }
        public string? AfReading { get; set; }
        public string? PowerReading { get; set; }
        public string? VoltageOutNoLoad { get; set; }
        public string? VoltageOutWithLoad { get; set; }
        public string? PhysicalCondition { get; set; }
        public string? DisplayCondition { get; set; }
        public string Status { get; set; } = null!;
        public int AssignedTechnicianUserId { get; set; }
        public string AssignedTechnicianName { get; set; } = null!;
        public int? WorkshopTechnicianId { get; set; }
        public string? WorkshopTechnicianName { get; set; }
        /// <summary>ID status custom jika job sedang di status custom.</summary>
        public int? CustomStatusId { get; set; }
        /// <summary>Label status custom untuk ditampilkan di UI.</summary>
        public string? CustomStatusLabel { get; set; }
        /// <summary>Warna status custom (Tailwind class atau hex).</summary>
        public string? CustomStatusColor { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? FirstInProgressAt { get; set; }
        public DateTime? WorkshopCompletedAt { get; set; }
        public int AccumulatedProgressDurationMinutes { get; set; }
        public DateTime? CurrentProgressStartedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool HasBorrowRequest { get; set; }
        public bool HasActiveBorrowedPart { get; set; }
        public bool HasReturnedBorrowedPart { get; set; }
        public string? PendingHandoverType { get; set; }
    }

    public class RadioRepairTicketGroupDto
    {
        public string HelpdeskTicketNumber { get; set; } = null!;
        public int RadioCount { get; set; }
        public List<RadioRepairJobListDto> Radios { get; set; } = [];
    }

    public class RadioRepairJobStatusLogDto
    {
        public int Id { get; set; }
        public string? FromStatus { get; set; }
        public string ToStatus { get; set; } = null!;
        public string? Note { get; set; }
        public string UserName { get; set; } = null!;
        public string? WorkshopTechnicianName { get; set; }
        public DateTime At { get; set; }
    }

    public class RadioRepairHandoverAccessoryDto
    {
        public string ItemName { get; set; } = null!;
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
        public string? SerialNumber { get; set; }
    }

    public class RadioRepairPrimaryHandoverDto
    {
        public int Id { get; set; }
        public string HandoverNumber { get; set; } = null!;
        public DateTime HandoverAt { get; set; }
        public string HandedOverByName { get; set; } = null!;
        public string ReceivedByName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? EquipmentName { get; set; }
        public string? UnitNumber { get; set; }
        public string? RadioOwnerLabel { get; set; }
        public string? OwnerDivision { get; set; }
        public string? OwnerDepartment { get; set; }
        public string RadioSerialNumber { get; set; } = null!;
        public string? BatterySerialNumber { get; set; }
        public string DamageDescription { get; set; } = null!;
        public List<RadioRepairHandoverAccessoryDto> Accessories { get; set; } = [];
    }

    public class RadioRepairJobHandoverSummaryDto
    {
        public int Id { get; set; }
        public string HandoverNumber { get; set; } = null!;
        public string HandoverType { get; set; } = null!;
        public DateTime HandoverAt { get; set; }
        public DateTime? SignedAt { get; set; }
        public string EquipmentTagType { get; set; } = "Damaged";
        public string HandedOverByName { get; set; } = null!;
        public string ReceivedByName { get; set; } = null!;
        public bool HasRadioPhoto { get; set; }
        public bool HasHandedOverSignature { get; set; }
        public bool HasReceiverSignature { get; set; }
    }

    public class RadioRepairJobDetailDto : RadioRepairJobListDto
    {
        public string? BatterySerialNumber { get; set; }
        // EquipmentName dan Owner properties sudah ada di RadioRepairJobListDto
        public string OpenedByName { get; set; } = null!;
        public List<RadioRepairJobStatusLogDto> StatusLogs { get; set; } = [];
        public List<RadioRepairJobHandoverSummaryDto> Handovers { get; set; } = [];
        public RadioRepairPrimaryHandoverDto? PrimaryHandover { get; set; }
    }

    public class RadioRepairDashboardDto
    {
        public int Total { get; set; }
        public int Received { get; set; }
        public int InProgress { get; set; }
        public int Monitoring { get; set; }
        public int WaitingMaterialApproval { get; set; }
        public int RepairCompleted { get; set; }
        public int HandedToWarehouse { get; set; }
        public int ReturnedToHelpdesk { get; set; }
        public int Cancelled { get; set; }
    }

    public class UpdateRadioRepairJobStatusDto
    {
        [Required]
        public RadioRepairJobStatus Status { get; set; }
        [MaxLength(500)]
        public string? Note { get; set; }
        /// <summary>
        /// Jika diisi, job akan masuk ke status custom ini (tetap InProgress di enum).
        /// Jika null, status custom dihapus dan job kembali ke status enum murni.
        /// </summary>
        public int? CustomStatusId { get; set; }
        public int? WorkshopTechnicianId { get; set; }
    }

    public class ApproveMaterialDto
    {
        public RadioRepairJobStatus ResumeStatus { get; set; } = RadioRepairJobStatus.InProgress;
        [MaxLength(500)]
        public string? Note { get; set; }
        public int? WorkshopTechnicianId { get; set; }
    }

    public class UpdateRadioRepairJobDto
    {
        [Required, MaxLength(50)]
        public string HelpdeskTicketNumber { get; set; } = null!;
        [Required, MaxLength(100)]
        public string RadioSerialNumber { get; set; } = null!;
        [MaxLength(100)]
        public string? BatterySerialNumber { get; set; }
        [Required, MaxLength(2000)]
        public string DamageDescription { get; set; } = null!;
        [Required]
        public int AssignedTechnicianUserId { get; set; }
        public int? WorkshopTechnicianId { get; set; }
        public int? RadioId { get; set; }
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
    }

    /// <summary>
    /// DTO untuk teknisi — hanya boleh ubah keterangan kerusakan.
    /// Perubahan dicatat di StatusLogs agar terlihat siapa yang mengubah apa.
    /// </summary>
    public class TechnicianUpdateRepairJobDto
    {
        [MaxLength(2000)]
        public string? DamageDescription { get; set; }

        public EquipmentTagType? EquipmentTagType { get; set; }
        [MaxLength(100)] public string? OriginFrom { get; set; }
        [MaxLength(2000)] public string? RepairDataDescription { get; set; }
        [MaxLength(100)] public string? RepairedByName { get; set; }
        [MaxLength(100)] public string? FrequencyError { get; set; }
        [MaxLength(100)] public string? AfReading { get; set; }
        [MaxLength(100)] public string? PowerReading { get; set; }
        [MaxLength(100)] public string? VoltageOutNoLoad { get; set; }
        [MaxLength(100)] public string? VoltageOutWithLoad { get; set; }
        [MaxLength(100)] public string? PhysicalCondition { get; set; }
        [MaxLength(100)] public string? DisplayCondition { get; set; }
    }

    public class ApproveScrapDto
    {
        [Required]
        public DateTime DateScrapped { get; set; }
        
        [MaxLength(100)]
        public string? ScrapJobNumber { get; set; }
        
        [MaxLength(1000)]
        public string? Remarks { get; set; }
    }
}