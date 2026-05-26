using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pm.Enums;

namespace Pm.Models
{
    [Table("WarehousePartBorrowStatusLogs")]
    public class WarehousePartBorrowStatusLog
    {
        [Key]
        public int Id { get; set; }

        public int BorrowId { get; set; }
        [ForeignKey(nameof(BorrowId))]
        public WarehousePartBorrow Borrow { get; set; } = null!;

        public WarehousePartBorrowStatus? FromStatus { get; set; }
        public WarehousePartBorrowStatus ToStatus { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public DateTime At { get; set; } = DateTime.UtcNow;
    }
}
