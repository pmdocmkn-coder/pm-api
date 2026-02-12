using System.ComponentModel.DataAnnotations;
using Pm.Enums;
using Pm.DTOs.Common;

namespace Pm.DTOs
{
    // ===== CREATE =====
    public class GatepassCreateDto
    {
        [Required(ErrorMessage = "Destination is required")]
        [StringLength(200, ErrorMessage = "Destination cannot exceed 200 characters")]
        public required string Destination { get; set; }

        [Required(ErrorMessage = "PIC Name is required")]
        [StringLength(100, ErrorMessage = "PIC Name cannot exceed 100 characters")]
        public required string PicName { get; set; }

        [StringLength(50, ErrorMessage = "PIC Contact cannot exceed 50 characters")]
        public string? PicContact { get; set; }

        [Required(ErrorMessage = "Gatepass date is required")]
        public DateTime GatepassDate { get; set; }

        public string? SignatureQRCode { get; set; }
        public string? Notes { get; set; }
        public GatepassStatus Status { get; set; } = GatepassStatus.Draft;

        // Items
        public List<GatepassItemDto>? Items { get; set; }
    }

    // ===== UPDATE =====
    public class GatepassUpdateDto
    {
        [Required(ErrorMessage = "Destination is required")]
        [StringLength(200, ErrorMessage = "Destination cannot exceed 200 characters")]
        public required string Destination { get; set; }

        [Required(ErrorMessage = "PIC Name is required")]
        [StringLength(100, ErrorMessage = "PIC Name cannot exceed 100 characters")]
        public required string PicName { get; set; }

        [StringLength(50, ErrorMessage = "PIC Contact cannot exceed 50 characters")]
        public string? PicContact { get; set; }

        public string? SignatureQRCode { get; set; }
        public string? Notes { get; set; }
        public GatepassStatus Status { get; set; }

        // Items
        public List<GatepassItemDto>? Items { get; set; }
    }

    // ===== ITEM DTO =====
    public class GatepassItemDto
    {
        public int? Id { get; set; } // Nullable for create
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(200, ErrorMessage = "Item name cannot exceed 200 characters")]
        public required string ItemName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        public string Unit { get; set; } = "unit";

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters")]
        public string? SerialNumber { get; set; }
    }

    // ===== RESPONSE =====
    public class GatepassResponseDto
    {
        public int Id { get; set; }
        public string FormattedNumber { get; set; } = string.Empty;
        public int SequenceNumber { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string PicName { get; set; } = string.Empty;
        public string? PicContact { get; set; }
        public DateTime GatepassDate { get; set; }
        public string? SignatureQRCode { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Expanded
        public UserInfoDto? CreatedByUser { get; set; }
        public UserInfoDto? UpdatedByUser { get; set; }
        public List<GatepassItemResponseDto> Items { get; set; } = new List<GatepassItemResponseDto>();
    }

    // ===== ITEM RESPONSE =====
    public class GatepassItemResponseDto
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? SerialNumber { get; set; }
    }

    // ===== LIST (for grid) =====
    public class GatepassListDto
    {
        public int Id { get; set; }
        public string FormattedNumber { get; set; } = string.Empty;
        public DateTime GatepassDate { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string PicName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
        public int ItemCount { get; set; }
    }

    // ===== QUERY =====
    public class GatepassQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public GatepassStatus? Status { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }
    }
}
