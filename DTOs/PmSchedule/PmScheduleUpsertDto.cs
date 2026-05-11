using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class PmScheduleUpsertDto
    {
        [Required]
        public int Year { get; set; }

        [Required]
        public int PmSiteId { get; set; }

        [Required]
        [MaxLength(255)]
        public string DeviceName { get; set; } = null!;

        // Array of all active tasks for this device in this year
        // We will replace existing tasks with this list
        public List<PmScheduleTaskDto> Tasks { get; set; } = new List<PmScheduleTaskDto>();
    }
}
