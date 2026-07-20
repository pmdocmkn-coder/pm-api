using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("BhpPaymentChecklists")]
    public class BhpPaymentChecklist
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OperationalDocumentId { get; set; }

        [ForeignKey("OperationalDocumentId")]
        public OperationalDocument? OperationalDocument { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public bool IsPaid { get; set; } = false;

        [MaxLength(100)]
        public string? InvoiceNumber { get; set; }

        public DateTime? PaidAt { get; set; }

        [MaxLength(255)]
        public string? PaidByUserName { get; set; }
    }
}
