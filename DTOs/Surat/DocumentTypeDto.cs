using System.ComponentModel.DataAnnotations;
using Pm.DTOs.Common;

namespace Pm.DTOs
{
    // ===== CREATE =====
    public class DocumentTypeCreateDto
    {
        [Required(ErrorMessage = "Code is required")]
        [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
        public required string Code { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public required string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
    }

    // =====UPDATE =====
    public class DocumentTypeUpdateDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public required string Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ===== RESPONSE =====
    public class DocumentTypeResponseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public UserInfoDto? CreatedByUser { get; set; }
        public UserInfoDto? UpdatedByUser { get; set; }
    }

    // ===== LIST (for dropdown/simple list) =====
    public class DocumentTypeListDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    // ===== QUERY (for filtering/pagination) =====
    public class DocumentTypeQueryDto : BaseQueryDto
    {
        public bool? IsActive { get; set; }
    }
}
