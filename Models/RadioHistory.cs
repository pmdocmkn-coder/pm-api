using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("RadioHistories")]
    public class RadioHistory
    {
        [Key]
        public int Id { get; set; }

        public int RadioId { get; set; }
        [ForeignKey("RadioId")]
        public Radio Radio { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = null!; // "Created", "Updated", "Scrapped"

        [MaxLength(2000)]
        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }
    }
}
