namespace Pm.DTOs.WarehousePartBorrow
{
    public class WarehousePartCatalogDto
    {
        public int Id { get; set; }
        public string PartCode { get; set; } = null!;
        public string PartName { get; set; } = null!;
        public string? OwnerId { get; set; }
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
    }

    public class CreateUpdateWarehousePartCatalogDto
    {
        public string PartCode { get; set; } = null!;
        public string PartName { get; set; } = null!;
        public string? OwnerId { get; set; }
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
    }

    public class WarehousePartImportResultDto
    {
        public int ImportedCount { get; set; }
        public int UpdatedCount { get; set; }
        public string Message { get; set; } = "Import selesai";
    }
}
