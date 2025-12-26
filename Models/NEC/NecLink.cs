using System.Collections.Generic;

namespace Pm.Models.NEC
{
    public class NecLink
    {
        public int Id { get; set; }
        public string LinkName { get; set; } = null!;

        public int NearEndTowerId { get; set; }
        public Tower NearEndTower { get; set; } = null!;

        public int FarEndTowerId { get; set; }
        public Tower FarEndTower { get; set; } = null!;

        public decimal ExpectedRslMin { get; set; } = -60m;  // batas bawah normal
        public decimal ExpectedRslMax { get; set; } = -40m;  // batas atas normal

        public ICollection<NecRslHistory> Histories { get; set; } = new List<NecRslHistory>();
    }
}