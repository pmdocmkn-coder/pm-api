using System.Collections.Generic;

namespace Pm.Models.SWR
{
    public class SwrChannel
    {
        public int Id { get; set; }
        public string ChannelName { get; set; } = null!; // e.g., "Channel 004", "C01 (FN)"
        
        public int SwrSiteId { get; set; }
        public SwrSite SwrSite { get; set; } = null!;

        /// <summary>
        /// Maximum acceptable VSWR (default 1.5)
        /// Below this = Good, Above = Bad
        /// </summary>
        public decimal ExpectedSwrMax { get; set; } = 1.5m;

        /// <summary>
        /// ✅ NEW: Minimum expected PWR in Watts (default 100W)
        /// Below this = Good, Above = Bad
        /// </summary>
        public decimal? ExpectedPwrMax { get; set; } = 100m;

        

        // Navigation properties
        public ICollection<SwrHistory> Histories { get; set; } = new List<SwrHistory>();
    }
}