using System.Collections.Generic;

namespace Pm.DTOs
{
    public class NecYearlyPivotDto
    {
        public string LinkName { get; set; } = null!;
        public string Tower { get; set; } = null!;
        public Dictionary<string, decimal?> MonthlyValues { get; set; } = new(); // "Jan-25": -43.4
        public decimal ExpectedRslMin { get; set; }
        public decimal ExpectedRslMax { get; set; }
        public Dictionary<string, string> Notes { get; set; } = new(); // "Jan-25": "Note for January 2025"
    }
}