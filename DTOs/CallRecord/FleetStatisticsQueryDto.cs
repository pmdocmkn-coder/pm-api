namespace Pm.DTOs.CallRecord
{
    public class FleetStatisticsQueryDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Top { get; set; } = 10; // default top 10
        public FleetStatisticType? Type { get; set; } // dropdown: Both, Caller, Called
        public string SortOrder { get; set; } = "DESC"; // ASC or DESC
        public string? CallerSearch { get; set; }
        public string? CalledSearch { get; set; }
    }

    public enum FleetStatisticType
    {
        All,
        Caller,
        Called
    }
}
