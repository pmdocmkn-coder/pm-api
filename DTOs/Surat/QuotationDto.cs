using System.ComponentModel.DataAnnotations;
using Pm.Enums;
using Pm.DTOs.Common;

namespace Pm.DTOs
{
    // ===== CREATE =====
    public class QuotationCreateDto
    {
        [Required(ErrorMessage = "Customer ID is required")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public required string Description { get; set; }

        [Required(ErrorMessage = "Quotation date is required")]
        public DateTime QuotationDate { get; set; }

        public string? Notes { get; set; }
        public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
    }

    // ===== UPDATE =====
    public class QuotationUpdateDto
    {
        [Required(ErrorMessage = "Description is required")]
        public required string Description { get; set; }

        public string? Notes { get; set; }
        public QuotationStatus Status { get; set; }

        // Optional: can edit customer
        public int? CustomerId { get; set; }

        // Optional: admin-only date edit
        public DateTime? QuotationDate { get; set; }
    }

    // ===== RESPONSE =====
    public class QuotationResponseDto
    {
        public int Id { get; set; }
        public string FormattedNumber { get; set; } = string.Empty;
        public int SequenceNumber { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime QuotationDate { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Expanded
        public CompanyListDto? Customer { get; set; }
        public UserInfoDto? CreatedByUser { get; set; }
        public UserInfoDto? UpdatedByUser { get; set; }
    }

    // ===== LIST (for grid) =====
    public class QuotationListDto
    {
        public int Id { get; set; }
        public string FormattedNumber { get; set; } = string.Empty;
        public DateTime QuotationDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
    }

    // ===== QUERY =====
    public class QuotationQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? CustomerId { get; set; }
        public QuotationStatus? Status { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }
    }
}
