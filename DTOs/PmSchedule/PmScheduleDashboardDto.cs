using System.Collections.Generic;

namespace Pm.DTOs
{
    public class PmComplianceDashboardDto
    {
        public int TotalScheduled { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalOverdue { get; set; }
        public double CompliancePercentage { get; set; }
        public List<PmTrendDto> Trend6Months { get; set; } = new List<PmTrendDto>();
        public PmCurrentMonthDto CurrentMonth { get; set; } = new PmCurrentMonthDto();
    }

    public class PmTrendDto
    {
        public string MonthName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public int Completed { get; set; }
        public int Overdue { get; set; }
        public double CompliancePercentage { get; set; }
    }

    public class PmCurrentMonthDto
    {
        public int TotalScheduled { get; set; }
        public int Completed { get; set; }
        public int Overdue { get; set; }
        public double ProgressPercentage { get; set; }
    }
}
