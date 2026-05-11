using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pm.Models.PmSchedule
{
    public class PmSchedule
    {
        public int Id { get; set; }
        
        public int Year { get; set; }
        
        public int PmSiteId { get; set; }
        public PmSite PmSite { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string DeviceName { get; set; } = null!;

        public ICollection<PmScheduleTask> Tasks { get; set; } = new List<PmScheduleTask>();
    }
}
