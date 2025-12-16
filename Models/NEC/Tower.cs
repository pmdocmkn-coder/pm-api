namespace Pm.Models.NEC
{
    public class Tower
    {
       public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Location { get; set; }

        public ICollection<NecLink> NearEndLinks { get; set; } = new List<NecLink>();
        public ICollection<NecLink> FarEndLinks { get; set; } = new List<NecLink>();
    }
}