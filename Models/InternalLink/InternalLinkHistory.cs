using System;
using System.ComponentModel.DataAnnotations.Schema;
using Pm.Enums;

namespace Pm.Models.InternalLink
{
    public class InternalLinkHistory
    {
        public int Id { get; set; }
        public int InternalLinkId { get; set; }
        public InternalLink InternalLink { get; set; } = null!;

        public DateTime Date { get; set; }

        public decimal? RslNearEnd { get; set; } // dBm value, mapped to decimal(10,2)

        public int? Uptime { get; set; } // days

        [Column(TypeName = "text")]
        public string? Notes { get; set; }

        [Column(TypeName = "longtext")]
        public string? ScreenshotBase64 { get; set; } // compressed JPEG base64

        [NotMapped]
        public string StatusString => Status.ToString();
        public InternalLinkStatus Status { get; set; } = InternalLinkStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
