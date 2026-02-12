namespace Pm.Models
{
    public class GatepassItem
    {
        public int Id { get; set; }
        public int GatepassId { get; set; }
        public required string ItemName { get; set; }
        public int Quantity { get; set; } = 1;
        public string Unit { get; set; } = "unit";
        public string? Description { get; set; }
        public string? SerialNumber { get; set; }

        // Navigation property
        public Gatepass? Gatepass { get; set; }
    }
}
