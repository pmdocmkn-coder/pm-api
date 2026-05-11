namespace Pm.Models.PmSchedule
{
    public class PmScheduleTask
    {
        public int Id { get; set; }

        public int PmScheduleId { get; set; }
        public PmSchedule PmSchedule { get; set; } = null!;

        public int Month { get; set; } // 1-12
        public int Week { get; set; }  // 1-4
    }
}
