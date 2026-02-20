using System.ComponentModel.DataAnnotations;
using Pm.DTOs.Common;

namespace Pm.DTOs
{
    // ===== CREATE =====
    public class DivisionCreateDto
    {
        [Required(ErrorMessage = "Code is required")]
        [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
        public required string Code { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public required string Name { get; set; }
    }

    // ===== UPDATE =====
    public class DivisionUpdateDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public required string Name { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // ===== RESPONSE =====
    public class DivisionResponseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public UserInfoDto? CreatedByUser { get; set; }
        public UserInfoDto? UpdatedByUser { get; set; }
    }

    // ===== LIST (for dropdown/simple list) =====
    public class DivisionListDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    // ===== QUERY (for filtering/pagination) =====
    public class DivisionQueryDto : BaseQueryDto
    {
        public bool? IsActive { get; set; }
    }
}
