using System.ComponentModel.DataAnnotations;
using Pm.DTOs.Common;
using Pm.Enums;

namespace Pm.DTOs
{
    // ===== CREATE (Generate Letter Number) =====
    public class LetterNumberCreateDto
    {
        [Required(ErrorMessage = "Company ID is required")]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Document Type ID is required")]
        public int DocumentTypeId { get; set; }

        [Required(ErrorMessage = "Letter date is required")]
        public DateTime LetterDate { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(500, ErrorMessage = "Subject cannot exceed 500 characters")]
        public required string Subject { get; set; }

        [Required(ErrorMessage = "Recipient is required")]
        [StringLength(200, ErrorMessage = "Recipient cannot exceed 200 characters")]
        public required string Recipient { get; set; }

        [StringLength(1000, ErrorMessage = "Attachment URL cannot exceed 1000 characters")]
        public string? AttachmentUrl { get; set; }

        public LetterStatus Status { get; set; } = LetterStatus.Draft;
    }

    // ===== UPDATE =====
    public class LetterNumberUpdateDto
    {
        [Required(ErrorMessage = "Subject is required")]
        [StringLength(500, ErrorMessage = "Subject cannot exceed 500 characters")]
        public required string Subject { get; set; }

        [Required(ErrorMessage = "Recipient is required")]
        [StringLength(200, ErrorMessage = "Recipient cannot exceed 200 characters")]
        public required string Recipient { get; set; }

        [StringLength(1000, ErrorMessage = "Attachment URL cannot exceed 1000 characters")]
        public string? AttachmentUrl { get; set; }

        public LetterStatus Status { get; set; }
    }

    // ===== RESPONSE =====
    public class LetterNumberResponseDto
    {
        public int Id { get; set; }
        public string FormattedNumber { get; set; } = string.Empty;
        public int SequenceNumber { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime LetterDate { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Expanded properties
        public DocumentTypeListDto? DocumentType { get; set; }
        public CompanyListDto? Company { get; set; }
    }

    // ===== LIST (simplified for grid/list views) =====
    public class LetterNumberListDto
    {
        public int Id { get; set; }
        public string FormattedNumber { get; set; } = string.Empty;
        public DateTime LetterDate { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DocumentTypeCode { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;
    }

    // ===== QUERY (for filtering/pagination) =====
    public class LetterNumberQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public int? DocumentTypeId { get; set; }
        public int? CompanyId { get; set; }
        public LetterStatus? Status { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }
    }
}
