using Pm.DTOs.Common;

namespace Pm.DTOs
{
    public class ActivityLogQueryDto : BaseQueryDto
    {
        public string? Module { get; set; }
        public string? Action { get; set; }
        public int? UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
