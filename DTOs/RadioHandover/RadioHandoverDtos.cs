using System.ComponentModel.DataAnnotations;
using Pm.DTOs.Common;
using Pm.Enums;

namespace Pm.DTOs.RadioHandover
{
    public class RadioHandoverQueryDto : BaseQueryDto
    {
        public RadioHandoverType? HandoverType { get; set; }
        public int? JobId { get; set; }
        public int? ReceivedByUserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        /// <summary>Hanya dengan permission radio.handover.view.archive.</summary>
        public bool IncludeDeleted { get; set; }
    }

    public class HandoverAccessoryItemDto
    {
        [Required, MaxLength(200)]
        public string ItemName { get; set; } = null!;
        [Range(1, 9999)]
        public int Quantity { get; set; } = 1;
        [MaxLength(20)]
        public string? Unit { get; set; }
        [MaxLength(500)]
        public string? Description { get; set; }
        [MaxLength(100)]
        public string? SerialNumber { get; set; }
    }

    public class CreateRadioHandoverDto
    {
        [Required]
        public RadioHandoverType HandoverType { get; set; }

        public string? HelpdeskTicketNumber { get; set; }
        public int? RadioId { get; set; }
        [Required, MaxLength(100)]
        public string RadioSerialNumber { get; set; } = null!;
        [MaxLength(100)]
        public string? BatterySerialNumber { get; set; }
        /// <summary>Wajib jika RadioId kosong (belum di master).</summary>
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
        public string? DamageDescription { get; set; }
        public int? RadioRepairJobId { get; set; }

        /// <summary>Tag kuning (rusak) atau hijau (baik). Default rusak untuk HD→Tek.</summary>
        public EquipmentTagType EquipmentTagType { get; set; } = EquipmentTagType.Damaged;

        /// <summary>Status garansi radio. Hanya berlaku untuk tipe HD → Teknisi. Default: tidak warranty.</summary>
        public bool IsWarranty { get; set; } = false;

        [MaxLength(100)]
        public string? NoJobErp { get; set; }

        [MaxLength(200)]
        public string? OriginFrom { get; set; }
        [MaxLength(2000)]
        public string? RepairDataDescription { get; set; }
        [MaxLength(200)]
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
        [MaxLength(500)]
        public string? PhysicalCondition { get; set; }
        [MaxLength(500)]
        public string? DisplayCondition { get; set; }

        [Required]
        public int ReceivedByUserId { get; set; }

        public int? WorkshopTechnicianId { get; set; }
        public int? HandedOverByWorkshopTechnicianId { get; set; }

        /// <summary>Legacy single photo; use <see cref="RadioPhotos"/> for multiple.</summary>
        public string? RadioPhotoBase64 { get; set; }
        public List<string> RadioPhotos { get; set; } = [];
        [Required]
        public string HandedOverSignatureBase64 { get; set; } = null!;
        /// <summary>Wajib untuk Tek→WH / WH→HD. Opsional untuk HD→Tek (dapat dilengkapi belakangan).</summary>
        public string? ReceiverSignatureBase64 { get; set; }

        public List<HandoverAccessoryItemDto> Accessories { get; set; } = [];
        [MaxLength(1000)]
        public string? Remarks { get; set; }
        
        [MaxLength(200)]
        public string? PicReceiverName { get; set; }
    }

    public class RadioHandoverListDto
    {
        public int Id { get; set; }
        public string HandoverNumber { get; set; } = null!;
        public string HandoverType { get; set; } = null!;
        public int RadioRepairJobId { get; set; }
        public string HelpdeskTicketNumber { get; set; } = null!;
        public string? NoJobErp { get; set; }
        public string RadioSerialNumber { get; set; } = null!;
        public string? EquipmentName { get; set; }
        public string? UnitNumber { get; set; }
        public string? RadioOwnerLabel { get; set; }
        public string? OwnerDivision { get; set; }
        public string? OwnerDepartment { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int ReceivedByUserId { get; set; }
        public string HandedOverByName { get; set; } = null!;
        public string ReceivedByName { get; set; } = null!;
        public int? WorkshopTechnicianId { get; set; }
        public string? WorkshopTechnicianName { get; set; }
        public int? HandedOverByWorkshopTechnicianId { get; set; }
        public string? HandedOverByWorkshopTechnicianName { get; set; }
        public DateTime HandoverAt { get; set; }
        public DateTime? SignedAt { get; set; }
        public string EquipmentTagType { get; set; } = "Damaged";
        public bool HasRadioPhoto { get; set; }
        public bool HasHandedOverSignature { get; set; }
        public bool HasReceiverSignature { get; set; }
        public string Status { get; set; } = null!;
        public int PhotoCount { get; set; }
        public string? PreviewPhotoBase64 { get; set; }
        public string? PicReceiverName { get; set; }
    }

    public class UpdateRadioHandoverDto
    {
        public string? HelpdeskTicketNumber { get; set; }
        public int? RadioId { get; set; }
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
        public string? DamageDescription { get; set; }

        public EquipmentTagType EquipmentTagType { get; set; } = EquipmentTagType.Damaged;

        /// <summary>Status garansi radio. Default: tidak warranty.</summary>
        public bool IsWarranty { get; set; } = false;

        [MaxLength(100)]
        public string? NoJobErp { get; set; }

        [MaxLength(200)]
        public string? OriginFrom { get; set; }
        [MaxLength(2000)]
        public string? RepairDataDescription { get; set; }
        [MaxLength(200)]
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
        [MaxLength(500)]
        public string? PhysicalCondition { get; set; }
        [MaxLength(500)]
        public string? DisplayCondition { get; set; }

        [Required]
        public int ReceivedByUserId { get; set; }

        public int? WorkshopTechnicianId { get; set; }
        public int? HandedOverByWorkshopTechnicianId { get; set; }

        public string? RadioPhotoBase64 { get; set; }
        public List<string> RadioPhotos { get; set; } = [];
        public string? HandedOverSignatureBase64 { get; set; }
        public string? ReceiverSignatureBase64 { get; set; }

        public List<HandoverAccessoryItemDto> Accessories { get; set; } = [];
        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [MaxLength(200)]
        public string? PicReceiverName { get; set; }
    }

    public class CompleteReceiverSignatureDto
    {
        [Required]
        public string ReceiverSignatureBase64 { get; set; } = null!;
        [MaxLength(200)]
        public string? PicReceiverName { get; set; }
        [MaxLength(1000)]
        public string? Remarks { get; set; }
    }

    public class RadioHandoverDetailDto : RadioHandoverListDto
    {
        public int? RadioId { get; set; }
        public string? RadioMasterRadioId { get; set; }
        public string? RadioFleet { get; set; }
        // RadioOwnerLabel, OwnerDivision, OwnerDepartment sudah ada di RadioHandoverListDto — tidak perlu redeclare
        public string? BatterySerialNumber { get; set; }
        public string? DamageDescription { get; set; }
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
        public string? RadioPhotoBase64 { get; set; }
        public List<string> RadioPhotos { get; set; } = [];
        public string? HandedOverSignatureBase64 { get; set; }
        public string? ReceiverSignatureBase64 { get; set; }
        public string? Remarks { get; set; }
        public List<HandoverAccessoryItemDto> Accessories { get; set; } = [];
        // HelpdeskTicketNumber sudah ada di RadioHandoverListDto — tidak perlu redeclare
        public string JobStatus { get; set; } = null!;
        public bool IsWarranty { get; set; }
    }

    public class UserOptionDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Username { get; set; } = null!;
    }
}
