using Pm.Enums;

namespace Pm.Models
{
    public class Gatepass
    {
        public int Id { get; set; }
        public required string FormattedNumber { get; set; }
        public int SequenceNumber { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public required string Destination { get; set; }
        public required string PicName { get; set; }
        public string? PicContact { get; set; }
        public DateTime GatepassDate { get; set; }
        public string? SignatureQRCode { get; set; } // QR code data for verification
        public string? Notes { get; set; }
        public GatepassStatus Status { get; set; } = GatepassStatus.Draft;

        // Digital Signature
        public int? SignedByUserId { get; set; }
        public DateTime? SignedAt { get; set; }
        public string? VerificationToken { get; set; }  // GUID for verification URL

        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        // Navigation properties
        public User? CreatedByUser { get; set; }
        public User? UpdatedByUser { get; set; }
        public User? SignedByUser { get; set; }
        public ICollection<GatepassItem> Items { get; set; } = new List<GatepassItem>();
    }
}
