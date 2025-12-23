namespace Pm.DTOs
{
    public class TowerListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Location { get; set; }
        public int LinkCount { get; set; }
    }

    public class TowerCreateDto
    {
        public string Name { get; set; } = null!;
        public string? Location { get; set; }
    }

    public class TowerUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Location { get; set; }
    }
}