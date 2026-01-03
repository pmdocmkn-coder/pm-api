using System.Collections.Generic;

namespace Pm.DTOs
{
    public class SwrMonthlyHistoryResponseDto
    {
        public string Period { get; set; } = null!; // "Januari 2025"
        public List<SwrSiteMonthlyDto> Data { get; set; } = new();
    }

    public class SwrSiteMonthlyDto
    {
        public string SiteName { get; set; } = null!;
        public string SiteType { get; set; } = null!;
        public List<SwrChannelMonthlyDto> Channels { get; set; } = new();
    }

    public class SwrChannelMonthlyDto
    {
        public string ChannelName { get; set; } = null!;
        public decimal? AvgFpwr { get; set; }
        public decimal AvgVswr { get; set; }
        public string Status { get; set; } = null!; // "good", "bad"
        public string? WarningMessage { get; set; }
    }

    public class SwrYearlySummaryDto
    {
        public int Year { get; set; }
        public List<SwrSiteYearlyDto> Sites { get; set; } = new();
    }

    public class SwrSiteYearlyDto
    {
        public string SiteName { get; set; } = null!;
        public string SiteType { get; set; } = null!;
        public Dictionary<string, SwrChannelYearlyDto> Channels { get; set; } = new();
    }

    public class SwrChannelYearlyDto
    {
        public Dictionary<string, decimal?> MonthlyAvgFpwr { get; set; } = new(); // "Jan": 75.2
        public Dictionary<string, decimal> MonthlyAvgVswr { get; set; } = new(); // "Jan": 1.3
        public decimal? YearlyAvgFpwr { get; set; }
        public decimal YearlyAvgVswr { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class SwrYearlyPivotDto
    {
        public string ChannelName { get; set; } = null!;
        public string SiteName { get; set; } = null!;
        public string SiteType { get; set; } = null!;
        public Dictionary<string, decimal?> MonthlyFpwr { get; set; } = new(); // "Jan-25": 75
        public Dictionary<string, decimal?> MonthlyVswr { get; set; } = new(); // "Jan-25": 1.3
        public decimal ExpectedSwrMax { get; set; }
        public Dictionary<string, string> Notes { get; set; } = new();
    }

    public class SwrImportRequestDto
    {
        public Microsoft.AspNetCore.Http.IFormFile ExcelFile { get; set; } = null!;
    }

    public class SwrImportResultDto
    {
        public int TotalRowsProcessed { get; set; }
        public int SuccessfulInserts { get; set; }
        public int FailedRows { get; set; }
        public List<string> Errors { get; set; } = new();
        public string Message { get; set; } = null!;
    }
}