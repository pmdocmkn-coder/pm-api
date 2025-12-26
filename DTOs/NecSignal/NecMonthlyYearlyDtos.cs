using System.Collections.Generic;

namespace Pm.DTOs
{
    public class NecMonthlyHistoryResponseDto
    {
        public string Period { get; set; } = null!; // e.g., "Januari 2025"
        public List<NecTowerMonthlyDto> Data { get; set; } = new();
    }

    public class NecTowerMonthlyDto
    {
        public string TowerName { get; set; } = null!;
        public List<NecLinkMonthlyDto> Links { get; set; } = new();
    }

    public class NecLinkMonthlyDto
    {
        public string LinkName { get; set; } = null!;
        public decimal AvgRsl { get; set; }
        public string Status { get; set; } = null!; // "normal", "warning_high", "warning_low"
        public string? WarningMessage { get; set; }
    }

    public class NecYearlySummaryDto
    {
        public int Year { get; set; }
        public List<NecTowerYearlyDto> Towers { get; set; } = new();
    }

    public class NecTowerYearlyDto
    {
        public string TowerName { get; set; } = null!;
        public Dictionary<string, NecLinkYearlyDto> Links { get; set; } = new();
    }

    public class NecLinkYearlyDto
    {
        public Dictionary<string, decimal> MonthlyAvg { get; set; } = new(); // "Jan": -45.2
        public decimal YearlyAvg { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}