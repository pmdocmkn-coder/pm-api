using System.Collections.Generic;
using Pm.Enums;

namespace Pm.Models.SWR
{
    public class SwrSite
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Location { get; set; }
        
        /// <summary>
        /// Trunking atau Conventional
        /// </summary>
        public SwrSiteType Type { get; set; } = SwrSiteType.Trunking;

        // Navigation properties
        public ICollection<SwrChannel> Channels { get; set; } = new List<SwrChannel>();
    }
}