using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pm.Enums;

namespace Pm.Models.InternalLink
{
    public class InternalLink
    {
        public int Id { get; set; }
        public string LinkName { get; set; } = null!; // e.g. "AB to Surya"

        [MaxLength(255)]
        public string? LinkGroup { get; set; } // e.g. "M5 - Surya", used to group TX/RX

        [NotMapped]
        public string DirectionString => Direction.ToString();
        public InternalLinkDirection Direction { get; set; } = InternalLinkDirection.None; // TX or RX

        public string? IpAddress { get; set; }
        public string? Device { get; set; }
        public string? Type { get; set; }
        [MaxLength(50)]
        public string? UsedFrequency { get; set; } // e.g., "5.8 GHz"

        public decimal? RslNearEnd { get; set; } // dBm value, mapped to decimal(10,2)

        [NotMapped]
        public string ServiceTypeString => ServiceType.ToString();
        public InternalLinkServiceType ServiceType { get; set; } = InternalLinkServiceType.LinkInternal;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<InternalLinkHistory> Histories { get; set; } = new List<InternalLinkHistory>();
    }
}
