namespace Pm.DTOs.CallRecord
{
    /// <summary>
    /// Detail information about a unique caller for a specific called fleet
    /// </summary>
    public class UniqueCallerDetailDto
    {
        public string CallerFleet { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public int TotalDurationSeconds { get; set; }
        public string TotalDurationFormatted { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detail information about a unique called fleet for a specific caller
    /// </summary>
    public class UniqueCalledDetailDto
    {
        public string CalledFleet { get; set; } = string.Empty;
        public int CallCount { get; set; }
        public int TotalDurationSeconds { get; set; }
        public string TotalDurationFormatted { get; set; } = string.Empty;
    }
}
