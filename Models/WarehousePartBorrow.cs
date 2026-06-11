using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pm.Enums;

namespace Pm.Models
{
    [Table("WarehousePartBorrows")]
    public class WarehousePartBorrow
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(30)]
        public string BorrowNumber { get; set; } = null!;

        public int BorrowedByUserId { get; set; }
        [ForeignKey(nameof(BorrowedByUserId))]
        public User BorrowedBy { get; set; } = null!;

        [MaxLength(200)]
        public string? BorrowerName { get; set; }

        public ICollection<WarehousePartBorrowItem> Items { get; set; } = new List<WarehousePartBorrowItem>();

        [MaxLength(1000)]
        public string? Purpose { get; set; }

        public int? RelatedRepairJobId { get; set; }
        [ForeignKey(nameof(RelatedRepairJobId))]
        public RadioRepairJob? RelatedRepairJob { get; set; }

        [MaxLength(100)]
        public string? TicketNumber { get; set; }

        public WarehousePartBorrowStatus Status { get; set; } = WarehousePartBorrowStatus.PendingApproval;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        [MaxLength(500)]
        public string? ApprovalNote { get; set; }

        public int? RejectedByUserId { get; set; }
        public DateTime? RejectedAt { get; set; }
        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime? IssuedAt { get; set; }
        public int? IssuedByUserId { get; set; }

        [Column(TypeName = "longtext")]
        public string? IssuerSignatureBase64 { get; set; }

        [Column(TypeName = "longtext")]
        public string? ReceiverSignatureBase64 { get; set; }

        [Column(TypeName = "longtext")]
        public string? ReturnIssuerSignatureBase64 { get; set; }

        [Column(TypeName = "longtext")]
        public string? ReturnReceiverSignatureBase64 { get; set; }

        public DateTime? ReturnedAt { get; set; }
        [MaxLength(200)]
        public string? ReturnCondition { get; set; }
        [MaxLength(500)]
        public string? ReturnNote { get; set; }

        /// <summary>Nama orang yang mengembalikan (jika berbeda dari peminjam asli)</summary>
        [MaxLength(200)]
        public string? ReturnedByName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsActive { get; set; } = true;

        public ICollection<WarehousePartBorrowStatusLog> StatusLogs { get; set; } = new List<WarehousePartBorrowStatusLog>();
    }
}
