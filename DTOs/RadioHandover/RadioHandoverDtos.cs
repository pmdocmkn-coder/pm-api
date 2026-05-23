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

        [Required]
        public int ReceivedByUserId { get; set; }

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
    }

    public class RadioHandoverListDto
    {
        public int Id { get; set; }
        public string HandoverNumber { get; set; } = null!;
        public string HandoverType { get; set; } = null!;
        public int RadioRepairJobId { get; set; }
        public string JobNumber { get; set; } = null!;
        public string HelpdeskTicketNumber { get; set; } = null!;
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
        public DateTime HandoverAt { get; set; }
        public bool HasRadioPhoto { get; set; }
        public bool HasHandedOverSignature { get; set; }
        public bool HasReceiverSignature { get; set; }
        public string Status { get; set; } = null!;
        public int PhotoCount { get; set; }
        public string? PreviewPhotoBase64 { get; set; }
    }

    public class UpdateRadioHandoverDto
    {
        [MaxLength(1000)]
        public string? Remarks { get; set; }
    }

    public class CompleteReceiverSignatureDto
    {
        [Required]
        public string ReceiverSignatureBase64 { get; set; } = null!;
    }

    public class RadioHandoverDetailDto : RadioHandoverListDto
    {
        public int? RadioId { get; set; }
        // RadioOwnerLabel, OwnerDivision, OwnerDepartment sudah ada di RadioHandoverListDto — tidak perlu redeclare
        public string? BatterySerialNumber { get; set; }
        public string? DamageDescription { get; set; }
        public string? RadioPhotoBase64 { get; set; }
        public List<string> RadioPhotos { get; set; } = [];
        public string? HandedOverSignatureBase64 { get; set; }
        public string? ReceiverSignatureBase64 { get; set; }
        public string? Remarks { get; set; }
        public List<HandoverAccessoryItemDto> Accessories { get; set; } = [];
        // HelpdeskTicketNumber sudah ada di RadioHandoverListDto — tidak perlu redeclare
        public string JobStatus { get; set; } = null!;
    }

    public class UserOptionDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Username { get; set; } = null!;
    }
}
