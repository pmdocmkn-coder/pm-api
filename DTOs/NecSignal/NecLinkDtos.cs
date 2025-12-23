namespace Pm.DTOs
{
    public class NecLinkListDto
    {
        public int Id { get; set; }
        public string LinkName { get; set; } = null!;
        public string NearEndTower { get; set; } = null!;
        public string FarEndTower { get; set; } = null!;

        public int NearEndTowerId { get; set; }
        public int FarEndTowerId { get; set; }
        public decimal ExpectedRslMin { get; set; }
        public decimal ExpectedRslMax { get; set; }
    }

    public class NecLinkCreateDto
    {
        public string LinkName { get; set; } = null!;
        public int NearEndTowerId { get; set; }
        public int FarEndTowerId { get; set; }
        public decimal ExpectedRslMin { get; set; } = -60m;
        public decimal ExpectedRslMax { get; set; } = -30m;
    }

    public class NecLinkUpdateDto
    {
        public int Id { get; set; }
        public string LinkName { get; set; } = null!;
        public int NearEndTowerId { get; set; }
        public int FarEndTowerId { get; set; }
        public decimal ExpectedRslMin { get; set; }
        public decimal ExpectedRslMax { get; set; }
    }
}