using System;
using System.ComponentModel.DataAnnotations.Schema;
using Pm.Enums;

namespace Pm.Models.SWR
{
    public class SwrHistory
    {
        public int Id { get; set; }
        public int SwrChannelId { get; set; }
        public SwrChannel SwrChannel { get; set; } = null!;

        public DateTime Date { get; set; }
        
        /// <summary>
        /// Forward Power - Only for Trunking type (nullable)
        /// </summary>
        public decimal? Fpwr { get; set; }
        
        /// <summary>
        /// VSWR (Voltage Standing Wave Ratio)
        /// </summary>
        public decimal Vswr { get; set; }

        [Column(TypeName = "text")]
        public string? Notes { get; set; }

        [NotMapped]
        public string StatusString => Status.ToString();
        
        public SwrOperationalStatus Status { get; set; } = SwrOperationalStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}