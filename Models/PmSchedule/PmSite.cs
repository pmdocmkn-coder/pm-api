using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pm.Models.PmSchedule
{
    public class PmSite
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = null!;
        
        public int OrderIndex { get; set; }

        public ICollection<PmSchedule> Schedules { get; set; } = new List<PmSchedule>();
    }
}
