using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("KpiDocuments")]
    public class KpiDocument
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime PeriodMonth { get; set; }
        
        [Required, MaxLength(100)]
        public required string AreaGroup { get; set; }
        
        [Required, MaxLength(255)]
        public required string DocumentName { get; set; }
        
        [Required, MaxLength(255)]
        public required string DataSource { get; set; }

        /// <summary>
        /// Optional group tag. Documents with the same GroupTag within the same AreaGroup
        /// will be visually merged (rowspan) in the monitoring table.
        /// Leave null for standalone rows.
        /// </summary>
        [MaxLength(100)]
        public string? GroupTag { get; set; }

        public DateTime? DateReceived { get; set; }
        public DateTime? DateSubmittedToReviewer { get; set; }
        public DateTime? DateApproved { get; set; }
        public DateTime? DateSubmittedToRqm { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
