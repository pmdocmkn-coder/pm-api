using System.Collections.Generic;

namespace Pm.DTOs
{
    // The main response format for the grid view
    public class PmYearlyScheduleResponseDto
    {
        public int Year { get; set; }
        public List<PmSiteScheduleDto> Sites { get; set; } = new List<PmSiteScheduleDto>();
    }

    public class PmSiteScheduleDto
    {
        public int SiteId { get; set; }
        public string SiteName { get; set; } = null!;
        public int OrderIndex { get; set; }
        public List<PmDeviceScheduleDto> Devices { get; set; } = new List<PmDeviceScheduleDto>();
    }

    public class PmDeviceScheduleDto
    {
        public int ScheduleId { get; set; }
        public string DeviceName { get; set; } = null!;
        public List<PmScheduleTaskDto> Tasks { get; set; } = new List<PmScheduleTaskDto>();
    }

    public class PmScheduleTaskDto
    {
        public int Id { get; set; }
        public int Month { get; set; } // 1-12
        public int Week { get; set; }  // 1-4
        
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? CompletedByUserId { get; set; }
        public string? CompletedByUserName { get; set; }
        public string? Remarks { get; set; }
    }
}
