using Pm.Enums;

namespace Pm.Models
{
    public class LetterNumber
    {
        public int Id { get; set; }
        public required string FormattedNumber { get; set; }
        public int SequenceNumber { get; set; }
        public int DocumentTypeId { get; set; }
        public int CompanyId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime LetterDate { get; set; }
        public required string Subject { get; set; }
        public required string Recipient { get; set; }
        public string? AttachmentUrl { get; set; }
        public LetterStatus Status { get; set; } = LetterStatus.Draft;
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation properties
        public DocumentType? DocumentType { get; set; }
        public Company? Company { get; set; }
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }
    }
}
