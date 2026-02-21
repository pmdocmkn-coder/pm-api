using Pm.Enums;

namespace Pm.DTOs.InternalLink
{
    public class InternalLinkListDto
    {
        public int Id { get; set; }
        public string LinkName { get; set; } = null!;
        public string? LinkGroup { get; set; }
        public InternalLinkDirection Direction { get; set; }
        public string DirectionString => Direction.ToString();
        public string? IpAddress { get; set; }
        public string? Device { get; set; }
        public string? Type { get; set; }
        public string? UsedFrequency { get; set; }
        public decimal? RslNearEnd { get; set; }
        public InternalLinkServiceType ServiceType { get; set; }
        public string ServiceTypeString => ServiceType.ToString();
        public bool IsActive { get; set; }
        public int HistoryCount { get; set; }
    }

    public class InternalLinkDetailDto : InternalLinkListDto
    {
        public DateTime CreatedAt { get; set; }
    }

    public class InternalLinkCreateDto
    {
        public string LinkName { get; set; } = null!;
        public string? LinkGroup { get; set; }
        public string? Direction { get; set; } // string from frontend, parsed to enum
        public string? IpAddress { get; set; }
        public string? Device { get; set; }
        public string? Type { get; set; }
        public string? UsedFrequency { get; set; }
        public decimal? RslNearEnd { get; set; }
        public string? ServiceType { get; set; } // string from frontend, parsed to enum
        public bool IsActive { get; set; } = true;
    }

    public class InternalLinkUpdateDto
    {
        public int Id { get; set; }
        public string LinkName { get; set; } = null!;
        public string? LinkGroup { get; set; }
        public string? Direction { get; set; }
        public string? IpAddress { get; set; }
        public string? Device { get; set; }
        public string? Type { get; set; }
        public string? UsedFrequency { get; set; }
        public decimal? RslNearEnd { get; set; }
        public string? ServiceType { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
