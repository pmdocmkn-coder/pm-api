using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pm.Enums;

namespace Pm.Models
{
    [Table("RadioHandovers")]
    public class RadioHandover
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string HandoverNumber { get; set; } = null!;

        public RadioHandoverType HandoverType { get; set; }

        public int RadioRepairJobId { get; set; }
        [ForeignKey(nameof(RadioRepairJobId))]
        public RadioRepairJob RadioRepairJob { get; set; } = null!;

        public int? RadioId { get; set; }
        [ForeignKey(nameof(RadioId))]
        public Radio? Radio { get; set; }

        [Required, MaxLength(100)]
        public string RadioSerialNumber { get; set; } = null!;

        [MaxLength(100)]
        public string? BatterySerialNumber { get; set; }

        /// <summary>Tipe/nama alat (dari master Type atau input manual jika belum terdaftar).</summary>
        [MaxLength(100)]
        public string? EquipmentName { get; set; }

        /// <summary>Nomor unit — dari master atau input manual.</summary>
        [MaxLength(100)]
        public string? UnitNumber { get; set; }

        /// <summary>Pemilik radio (perusahaan / nama user) — master atau input manual.</summary>
        [MaxLength(200)]
        public string? RadioOwnerLabel { get; set; }

        [MaxLength(100)]
        public string? OwnerDivision { get; set; }

        [MaxLength(100)]
        public string? OwnerDepartment { get; set; }

        [Column(TypeName = "longtext")]
        public string? RadioPhotoBase64 { get; set; }

        [Column(TypeName = "longtext")]
        public string? HandedOverSignatureBase64 { get; set; }

        [Column(TypeName = "longtext")]
        public string? ReceiverSignatureBase64 { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        /// <summary>Tag kuning (rusak) atau hijau (baik).</summary>
        public EquipmentTagType EquipmentTagType { get; set; } = EquipmentTagType.Damaged;

        /// <summary>Menandakan apakah serah terima ini adalah alur khusus radio scrap.</summary>
        public bool IsScrapFlow { get; set; } = false;

        /// <summary>Menandakan apakah serah terima ini parsial (tidak semua item diserahkan).</summary>
        public bool IsPartial { get; set; } = false;

        /// <summary>Menandakan apakah unit radio utama diserahkan pada serah terima ini.</summary>
        public bool ContainsMainRadioUnit { get; set; } = true;

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

        public int HandedOverByUserId { get; set; }
        [ForeignKey(nameof(HandedOverByUserId))]
        public User HandedOverBy { get; set; } = null!;

        public int ReceivedByUserId { get; set; }
        [ForeignKey(nameof(ReceivedByUserId))]
        public User ReceivedBy { get; set; } = null!;

        /// <summary>Nama PIC / penerima fisik sesungguhnya (terutama dari Warehouse ke Helpdesk atau pengambilan oleh User)</summary>
        [MaxLength(200)]
        public string? PicReceiverName { get; set; }

        public int? WorkshopTechnicianId { get; set; }
        [ForeignKey(nameof(WorkshopTechnicianId))]
        public WorkshopTechnician? WorkshopTechnician { get; set; }

        public int? HandedOverByWorkshopTechnicianId { get; set; }
        [ForeignKey(nameof(HandedOverByWorkshopTechnicianId))]
        public WorkshopTechnician? HandedOverByWorkshopTechnician { get; set; }

        public DateTime HandoverAt { get; set; } = DateTime.UtcNow;
        public DateTime? SignedAt { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Completed";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }

        public ICollection<RadioHandoverAccessory> Accessories { get; set; } = new List<RadioHandoverAccessory>();
        public ICollection<RadioHandoverPhoto> Photos { get; set; } = new List<RadioHandoverPhoto>();
    }
}
