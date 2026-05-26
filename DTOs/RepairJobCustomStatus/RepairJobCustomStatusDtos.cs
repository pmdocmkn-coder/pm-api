using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs.RepairJobCustomStatus
{
    public class RepairJobCustomStatusDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = null!;
        public string Color { get; set; } = null!;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public int ActiveJobCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateRepairJobCustomStatusDto
    {
        [Required, MaxLength(100)]
        public string Label { get; set; } = null!;
        [MaxLength(50)]
        public string Color { get; set; } = "bg-slate-500";
        public int SortOrder { get; set; } = 0;
    }

    public class UpdateRepairJobCustomStatusDto
    {
        [Required, MaxLength(100)]
        public string Label { get; set; } = null!;
        [MaxLength(50)]
        public string Color { get; set; } = "bg-slate-500";
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }
}
