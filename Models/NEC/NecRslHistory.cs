using System;
using System.ComponentModel.DataAnnotations.Schema;
using Pm.Enums;

namespace Pm.Models.NEC
{
    public class NecRslHistory
    {
        public int Id { get; set; }
        public int NecLinkId { get; set; }
        public NecLink NecLink { get; set; } = null!;

        public DateTime Date { get; set; }
        public decimal? RslNearEnd { get; set; }
        public decimal? RslFarEnd { get; set; }

        [Column(TypeName = "text")]
        public string? Notes { get; set; }

        [NotMapped]
        public string StatusString => Status.ToString();
        public NecOperationalStatus Status { get; set; } = NecOperationalStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}