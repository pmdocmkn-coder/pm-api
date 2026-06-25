using System.ComponentModel.DataAnnotations;
using Pm.DTOs.Common;

namespace Pm.DTOs.WarehousePartBorrow
{
    public class WarehousePartBorrowQueryDto : BaseQueryDto
    {
        public string? Status { get; set; }
        public int? BorrowedByUserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class WarehousePartBorrowItemDto
    {
        public int? Id { get; set; }
        [Required, MaxLength(500)]
        public string PartDescription { get; set; } = null!;
        [MaxLength(100)]
        public string? PartCode { get; set; }
        [Range(1, 999)]
        public int Quantity { get; set; } = 1;
    }

    public class CreateWarehousePartBorrowDto
    {
        [Required, MinLength(1)]
        public List<WarehousePartBorrowItemDto> Items { get; set; } = new();
        
        [MaxLength(1000)]
        public string? Purpose { get; set; }
        public int? RelatedRepairJobId { get; set; }
        [MaxLength(100)]
        public string? TicketNumber { get; set; }
        
        [MaxLength(200)]
        public string? BorrowerName { get; set; }
    }

    public class WarehousePartBorrowListDto
    {
        public int Id { get; set; }
        public string BorrowNumber { get; set; } = null!;
        
        public List<WarehousePartBorrowItemDto> Items { get; set; } = new();
        public int TotalItems => Items.Count;
        public string Status { get; set; } = null!;
        public string BorrowedByName { get; set; } = null!;
        public DateTime RequestedAt { get; set; }
        /// <summary>Waktu barang benar-benar diserahkan (Issued). Dipakai untuk hitung durasi peminjaman.</summary>
        public DateTime? IssuedAt { get; set; }
        public string? RelatedJobNumber { get; set; }
        public string? TicketNumber { get; set; }
        public string? BorrowerName { get; set; }
        public string? Purpose { get; set; }
    }

    public class WarehousePartBorrowStatusLogDto
    {
        public int Id { get; set; }
        public string? FromStatus { get; set; }
        public string ToStatus { get; set; } = null!;
        public string? Note { get; set; }
        public string UserName { get; set; } = null!;
        public DateTime At { get; set; }
    }

    public class WarehousePartBorrowDetailDto : WarehousePartBorrowListDto
    {
        public new string? Purpose { get; set; }
        public int? RelatedRepairJobId { get; set; }
        public string? ApprovalNote { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public new DateTime? IssuedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string? ReturnCondition { get; set; }
        public string? ReturnNote { get; set; }
        public string? ReturnedByName { get; set; }
        public string? IssuerSignatureBase64 { get; set; }
        public string? ReceiverSignatureBase64 { get; set; }
        public string? ReturnIssuerSignatureBase64 { get; set; }
        public string? ReturnReceiverSignatureBase64 { get; set; }
        public List<WarehousePartBorrowStatusLogDto> StatusLogs { get; set; } = [];
    }

    public class ApproveBorrowDto
    {
        [MaxLength(500)]
        public string? Note { get; set; }
    }

    public class IssueBorrowDto
    {
        public string? IssuerSignatureBase64 { get; set; }
        public string? ReceiverSignatureBase64 { get; set; }
    }

    public class RejectBorrowDto
    {
        [Required, MaxLength(500)]
        public string Reason { get; set; } = null!;
    }

    public class ReturnBorrowDto
    {
        [MaxLength(200)]
        public string? ReturnCondition { get; set; }
        [MaxLength(500)]
        public string? ReturnNote { get; set; }
        public string? ReturnIssuerSignatureBase64 { get; set; }
        public string? ReturnReceiverSignatureBase64 { get; set; }
        /// <summary>Nama orang yang mengembalikan (jika diwakilkan)</summary>
        [MaxLength(200)]
        public string? ReturnedByName { get; set; }
    }

    public class SignReceiverBorrowDto
    {
        [Required]
        public string ReceiverSignatureBase64 { get; set; } = null!;
    }

    public class SignReturnReceiverBorrowDto
    {
        [Required]
        public string ReturnReceiverSignatureBase64 { get; set; } = null!;
        [MaxLength(200)]
        public string? ReturnCondition { get; set; }
        [MaxLength(500)]
        public string? ReturnNote { get; set; }
    }
}
