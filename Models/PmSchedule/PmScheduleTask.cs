namespace Pm.Models.PmSchedule
{
    public class PmScheduleTask
    {
        public int Id { get; set; }

        public int PmScheduleId { get; set; }
        public PmSchedule PmSchedule { get; set; } = null!;

        public int Month { get; set; } // 1-12
        public int Week { get; set; }  // 1-4

        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
        public int? CompletedByUserId { get; set; }
        public User? CompletedByUser { get; set; }
        public string? Remarks { get; set; }
    }
}
