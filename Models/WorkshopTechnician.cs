using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    [Table("WorkshopTechnicians")]
    public class WorkshopTechnician
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Relasi ke User yang ber-role "Teknisi WSK" (nullable - bisa diisi atau kosong)
        /// Jika diisi, teknisi ini terkait dengan akun user tertentu
        /// </summary>
        public int? UserId { get; set; }
        
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public int? DeletedByUserId { get; set; }
    }
}
