using Pm.Enums;

namespace Pm.Models
{
    public class Quotation
    {
        public int Id { get; set; }
        public required string FormattedNumber { get; set; }
        public int SequenceNumber { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int CustomerId { get; set; }
        public required string CustomerName { get; set; }
        public required string Description { get; set; }
        public DateTime QuotationDate { get; set; }
        public string? Notes { get; set; }
        public QuotationStatus Status { get; set; } = QuotationStatus.Draft;
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation properties
        public Company? Customer { get; set; }
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }
    }
}
