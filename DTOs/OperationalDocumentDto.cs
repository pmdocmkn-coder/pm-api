using System.ComponentModel.DataAnnotations;
using Pm.DTOs.Common;

namespace Pm.DTOs
{
    public class OperationalDocumentQueryDto : BaseQueryDto
    {
        public string? Type { get; set; }
        public string? FollowUpStatus { get; set; }
        public string? ExpiryStatus { get; set; }
        public string? GroupName { get; set; }
        protected override string[] AllowedSortFields => ["ValidUntil", "Name", "CreatedAt", "GroupName"];
    }

    public class OperationalDocumentCreateDto
    {
        [Required(ErrorMessage = "Nama dokumen wajib diisi")]
        [MaxLength(255)]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Tipe dokumen wajib diisi")]
        [MaxLength(100)]
        public required string Type { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        /// <summary>
        /// Opsional. Jika diisi, dokumen dengan grup yang sama akan dikelompokkan
        /// dalam satu notifikasi WA. Contoh: "ISR Link Backbone 2027 KPC"
        /// </summary>
        [MaxLength(200)]
        public string? GroupName { get; set; }

        [Required(ErrorMessage = "Tanggal berlaku wajib diisi")]
        public DateTime ValidFrom { get; set; }

        [Required(ErrorMessage = "Tanggal berakhir wajib diisi")]
        public DateTime ValidUntil { get; set; }

        [MaxLength(255)]
        public string? PicName { get; set; }

        [MaxLength(50)]
        [RegularExpression(@"^62[0-9]{8,15}$", ErrorMessage = "Nomor WhatsApp harus diawali dengan 62 dan hanya berisi angka")]
        public string? PicPhone { get; set; }

        [MaxLength(1000)]
        [Url(ErrorMessage = "Format link tidak valid")]
        public string? FileLink { get; set; }
    }

    public class OperationalDocumentUpdateDto : OperationalDocumentCreateDto
    {
    }

    public class OperationalDocumentResponseDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Type { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? GroupName { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public string? PicName { get; set; }
        public string? PicPhone { get; set; }
        public string? FileLink { get; set; }
        public required string FollowUpStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Calculated field for frontend convenience
        public int DaysRemaining => (int)(ValidUntil.Date - DateTime.UtcNow.Date).TotalDays;
        
        public string ExpiryStatus 
        {
            get 
            {
                if (DaysRemaining < 0) return "Expired";
                if (DaysRemaining <= 30) return "Warning";
                return "Aman";
            }
        }
    }

    public class OperationalDocumentSummaryDto
    {
        public int TotalDocuments { get; set; }
        public int ExpiringSoon { get; set; }
        public int Expired { get; set; }
    }
}
