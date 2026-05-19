using Pm.DTOs.Common;

namespace Pm.DTOs.Radio
{
    public class RadioQueryDto : BaseQueryDto
    {
        public string? Category { get; set; }
        public bool IsScrap { get; set; } = false;

        // Filter tambahan (server-side)
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? Type { get; set; }
        public string? Fleet { get; set; }
        public string? Jenis { get; set; }   // "trunking" | "konvensional"
        public bool? IsDuplicate { get; set; }
        public bool? IsNoGrafir { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        // Override default page size ke 50
        public new int PageSize { get; set; } = 50;
    }
}
