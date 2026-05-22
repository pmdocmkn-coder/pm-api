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
    }

    public class RadioRepairJobListDto
    {
        public int Id { get; set; }
        public string JobNumber { get; set; } = null!;
        public string HelpdeskTicketNumber { get; set; } = null!;
        public string RadioSerialNumber { get; set; } = null!;
        public int? RadioId { get; set; }
        public string? RadioCategory { get; set; }
        public string DamageDescription { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int AssignedTechnicianUserId { get; set; }
        public string AssignedTechnicianName { get; set; } = null!;
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    public class RadioRepairJobStatusLogDto
    {
        public int Id { get; set; }
        public string? FromStatus { get; set; }
        public string ToStatus { get; set; } = null!;
        public string? Note { get; set; }
        public string UserName { get; set; } = null!;
        public DateTime At { get; set; }
    }

    public class RadioRepairJobHandoverSummaryDto
    {
        public int Id { get; set; }
        public string HandoverNumber { get; set; } = null!;
        public string HandoverType { get; set; } = null!;
        public DateTime HandoverAt { get; set; }
        public string HandedOverByName { get; set; } = null!;
        public string ReceivedByName { get; set; } = null!;
        public bool HasRadioPhoto { get; set; }
        public bool HasHandedOverSignature { get; set; }
        public bool HasReceiverSignature { get; set; }
    }

    public class RadioRepairJobDetailDto : RadioRepairJobListDto
    {
        public string? BatterySerialNumber { get; set; }
        public string OpenedByName { get; set; } = null!;
        public List<RadioRepairJobStatusLogDto> StatusLogs { get; set; } = [];
        public List<RadioRepairJobHandoverSummaryDto> Handovers { get; set; } = [];
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
    }

    public class ApproveMaterialDto
    {
        public RadioRepairJobStatus ResumeStatus { get; set; } = RadioRepairJobStatus.InProgress;
        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
