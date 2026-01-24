namespace Pm.Models
{
    public class DocumentType
    {
        public int Id { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation properties
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }
        public ICollection<LetterNumber> LetterNumbers { get; set; } = new List<LetterNumber>();
    }
}
