namespace Pm.Models.NEC
{
public class NecRslHistory
    {
        public int Id { get; set; }
        public int NecLinkId { get; set; }
        public NecLink NecLink { get; set; } = null!;

        public DateTime Date { get; set; }
        public decimal RslNearEnd { get; set; }
        public decimal? RslFarEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}