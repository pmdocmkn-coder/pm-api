using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("WarehousePartBorrowItems")]
    public class WarehousePartBorrowItem
    {
        [Key]
        public int Id { get; set; }

        public int BorrowId { get; set; }
        [ForeignKey(nameof(BorrowId))]
        public WarehousePartBorrow Borrow { get; set; } = null!;

        [Required, MaxLength(500)]
        public string PartDescription { get; set; } = null!;

        [MaxLength(100)]
        public string? PartCode { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        public int Quantity { get; set; } = 1;
    }
}
