namespace Pm.DTOs.Radio
{
    public class ImportResultDto
    {
        public int TotalImported { get; set; }
        public int SheetCount { get; set; }
        public List<SheetImportDetail> SheetDetails { get; set; } = new();
    }

    public class SheetImportDetail
    {
        public string SheetName { get; set; } = string.Empty;
        public int RecordCount { get; set; }
    }
}
