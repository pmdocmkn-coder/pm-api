using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    /// <summary>
    /// Status pekerjaan custom yang ditambahkan oleh supervisor.
    /// Status sistem (enum RadioRepairJobStatus) tetap tidak berubah.
    /// Job yang sedang menggunakan status ini tidak bisa dihapus.
    /// </summary>
    [Table("RepairJobCustomStatuses")]
    public class RepairJobCustomStatus
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Label yang ditampilkan di UI, contoh: "Menunggu Spare Part"</summary>
        [Required, MaxLength(100)]
        public string Label { get; set; } = null!;

        /// <summary>Warna tombol dalam format Tailwind atau hex, contoh: "bg-cyan-500" atau "#06b6d4"</summary>
        [MaxLength(50)]
        public string Color { get; set; } = "bg-slate-500";

        /// <summary>Urutan tampil di antara tombol status</summary>
        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public int CreatedByUserId { get; set; }
        [ForeignKey(nameof(CreatedByUserId))]
        public User CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Jobs yang sedang menggunakan status ini — dipakai untuk cek sebelum hapus.</summary>
        public ICollection<RadioRepairJob> ActiveJobs { get; set; } = new List<RadioRepairJob>();
    }
}
