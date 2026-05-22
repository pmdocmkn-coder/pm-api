using System;

namespace Pm.DTOs.Radio
{
    public class RadioDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = null!; // "Internal", "Contractor", "Unit", "LegacyScrap"
        
        public string? SerialNumber { get; set; }
        public string? Type { get; set; }
        public string? Department { get; set; }
        public string? Division { get; set; }
        public string? Company { get; set; }
        public string? Channel { get; set; }
        public DateTime? Tanggal { get; set; }
        
        public string? NomorAset { get; set; }
        public string? NomorUnit { get; set; }
        public string? NomorLv { get; set; }
        
        public bool IsTrunking { get; set; }
        public bool IsConventional { get; set; }
        public string? Fleet { get; set; }
        public string? RadioId { get; set; }
        
        // Scrap details
        public bool IsScrap { get; set; }
        public string? ScrapJobNumber { get; set; }
        public DateTime? DateScrapped { get; set; }
        
        public string? Remarks { get; set; }
        public string? Mark { get; set; }
        
        // Additional properties for UI
        public bool IsDuplicateId { get; set; }
    }

    public class CreateRadioDto
    {
        public string Category { get; set; } = null!;
        public string? SerialNumber { get; set; }
        public string? Type { get; set; }
        public string? Department { get; set; }
        public string? Division { get; set; }
        public string? Company { get; set; }
        public string? Channel { get; set; }
        public DateTime? Tanggal { get; set; }
        
        public string? NomorAset { get; set; }
        public string? NomorUnit { get; set; }
        public string? NomorLv { get; set; }
        
        public bool IsTrunking { get; set; }
        public bool IsConventional { get; set; }
        public string? Fleet { get; set; }
        public string? RadioId { get; set; }
        
        public bool IsScrap { get; set; }
        public string? ScrapJobNumber { get; set; }
        public DateTime? DateScrapped { get; set; }
        
        public string? Remarks { get; set; }
        public string? Mark { get; set; }
    }

    public class UpdateRadioDto : CreateRadioDto
    {
    }

    public class ScrapRadioDto
    {
        public string ScrapJobNumber { get; set; } = null!;
        public DateTime DateScrapped { get; set; }
        public string? Remarks { get; set; }
    }

    public class RadioHistoryDto
    {
        public int Id { get; set; }
        public int RadioId { get; set; }
        public string Action { get; set; } = null!;
        public string? Changes { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class DuplicateSnDto
    {
        public string SerialNumber { get; set; } = null!;
        public int Count { get; set; }
        public List<DuplicateSnItemDto> Occurrences { get; set; } = new();
    }

    public class DuplicateSnItemDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = null!;
        public string? NomorAset { get; set; }
        public string? NomorUnit { get; set; }
        public string? NomorLv { get; set; }
        public string? Company { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
    }

    public class RadioLookupDto
    {
        public int Id { get; set; }
        public string Category { get; set; } = null!;
        public string? SerialNumber { get; set; }
        public string? Type { get; set; }
        public string? Division { get; set; }
        public string? Department { get; set; }
        public string? NomorAset { get; set; }
        public string Label { get; set; } = null!;
    }
}
