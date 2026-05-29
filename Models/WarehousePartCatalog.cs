using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("WarehousePartCatalog")]
    public class WarehousePartCatalog
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string PartCode { get; set; } = null!;

        [Required, MaxLength(250)]
        public string PartName { get; set; } = null!;

        [MaxLength(100)]
        public string? OwnerId { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
