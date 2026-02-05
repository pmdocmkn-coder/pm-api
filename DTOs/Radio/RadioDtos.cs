namespace Pm.DTOs.Radio
{
    // ===========================================
    // Radio Trunking DTOs
    // ===========================================

    public class RadioTrunkingDto
    {
        public int Id { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public string? Dept { get; set; }
        public string? Fleet { get; set; }
        public string RadioId { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public DateTime? DateProgram { get; set; }
        public string? RadioType { get; set; }
        public string? JobNumber { get; set; }
        public string Status { get; set; } = "Active";
        public string? Initiator { get; set; }
        public string? Firmware { get; set; }
        public string? ChannelApply { get; set; }
        public int? GrafirId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Related Grafir info
        public RadioGrafirBasicDto? GrafirInfo { get; set; }
    }

    public class CreateRadioTrunkingDto
    {
        public required string UnitNumber { get; set; }
        public string? Dept { get; set; }
        public string? Fleet { get; set; }
        public required string RadioId { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? DateProgram { get; set; }
        public string? RadioType { get; set; }
        public string? JobNumber { get; set; }
        public string Status { get; set; } = "Active";
        public string? Initiator { get; set; }
        public string? Firmware { get; set; }
        public string? ChannelApply { get; set; }
        public int? GrafirId { get; set; }
    }

    public class UpdateRadioTrunkingDto
    {
        public string? UnitNumber { get; set; }
        public string? Dept { get; set; }
        public string? Fleet { get; set; }
        public string? RadioId { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? DateProgram { get; set; }
        public string? RadioType { get; set; }
        public string? JobNumber { get; set; }
        public string? Status { get; set; }
        public string? Initiator { get; set; }
        public string? Firmware { get; set; }
        public string? ChannelApply { get; set; }
        public int? GrafirId { get; set; }
        public string? Notes { get; set; } // For history tracking
    }

    // ===========================================
    // Radio Conventional DTOs
    // ===========================================

    public class RadioConventionalDto
    {
        public int Id { get; set; }
        public string UnitNumber { get; set; } = string.Empty;
        public string RadioId { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? Dept { get; set; }
        public string? Fleet { get; set; }
        public string? RadioType { get; set; }
        public string? Frequency { get; set; }
        public string Status { get; set; } = "Active";
        public int? GrafirId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public RadioGrafirBasicDto? GrafirInfo { get; set; }
    }

    public class CreateRadioConventionalDto
    {
        public required string UnitNumber { get; set; }
        public required string RadioId { get; set; }
        public string? SerialNumber { get; set; }
        public string? Dept { get; set; }
        public string? Fleet { get; set; }
        public string? RadioType { get; set; }
        public string? Frequency { get; set; }
        public string Status { get; set; } = "Active";
        public int? GrafirId { get; set; }
    }

    public class UpdateRadioConventionalDto
    {
        public string? UnitNumber { get; set; }
        public string? RadioId { get; set; }
        public string? SerialNumber { get; set; }
        public string? Dept { get; set; }
        public string? Fleet { get; set; }
        public string? RadioType { get; set; }
        public string? Frequency { get; set; }
        public string? Status { get; set; }
        public int? GrafirId { get; set; }
        public string? Notes { get; set; }
    }

    // ===========================================
    // Radio Grafir DTOs
    // ===========================================

    public class RadioGrafirDto
    {
        public int Id { get; set; }
        public string NoAsset { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string? TypeRadio { get; set; }
        public string? Div { get; set; }
        public string? Dept { get; set; }
        public string? FleetId { get; set; }
        public DateTime? Tanggal { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Linked radios count
        public int TrunkingCount { get; set; }
        public int ConventionalCount { get; set; }
    }

    public class RadioGrafirBasicDto
    {
        public int Id { get; set; }
        public string NoAsset { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string? TypeRadio { get; set; }
        public string? Div { get; set; }
    }

    public class CreateRadioGrafirDto
    {
        public required string NoAsset { get; set; }
        public required string SerialNumber { get; set; }
        public string? TypeRadio { get; set; }
        public string? Div { get; set; }
        public string? Dept { get; set; }
        public string? FleetId { get; set; }
        public DateTime? Tanggal { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class UpdateRadioGrafirDto
    {
        public string? NoAsset { get; set; }
        public string? SerialNumber { get; set; }
        public string? TypeRadio { get; set; }
        public string? Div { get; set; }
        public string? Dept { get; set; }
        public string? FleetId { get; set; }
        public DateTime? Tanggal { get; set; }
        public string? Status { get; set; }
    }

    // ===========================================
    // Radio Scrap DTOs
    // ===========================================

    public class RadioScrapDto
    {
        public int Id { get; set; }
        public string ScrapCategory { get; set; } = "Trunking";
        public string? TypeRadio { get; set; }
        public string? SerialNumber { get; set; }
        public string? JobNumber { get; set; }
        public DateTime DateScrap { get; set; }
        public string? Remarks { get; set; }
        public int? SourceTrunkingId { get; set; }
        public int? SourceConventionalId { get; set; }
        public int? SourceGrafirId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Source info
        public string? SourceRadioId { get; set; }
        public string? SourceUnitNumber { get; set; }
    }

    public class CreateRadioScrapDto
    {
        public required string ScrapCategory { get; set; } // Trunking, Conventional
        public string? TypeRadio { get; set; }
        public string? SerialNumber { get; set; }
        public string? JobNumber { get; set; }
        public required DateTime DateScrap { get; set; }
        public string? Remarks { get; set; }
    }

    public class ScrapFromRadioDto
    {
        public string? JobNumber { get; set; }
        public required DateTime DateScrap { get; set; }
        public string? Remarks { get; set; }
    }

    // ===========================================
    // History DTOs
    // ===========================================

    public class RadioHistoryDto
    {
        public int Id { get; set; }
        public int RadioId { get; set; }
        public string? PreviousUnitNumber { get; set; }
        public string? NewUnitNumber { get; set; }
        public string? PreviousDept { get; set; }
        public string? NewDept { get; set; }
        public string? PreviousFleet { get; set; }
        public string? NewFleet { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? ChangedByName { get; set; }
    }

    // ===========================================
    // Yearly Summary DTOs
    // ===========================================

    public class YearlyScrapSummaryDto
    {
        public int Year { get; set; }
        public ScrapCategorySummaryDto Trunking { get; set; } = new();
        public ScrapCategorySummaryDto Conventional { get; set; } = new();
        public int GrandTotal { get; set; }
    }

    public class ScrapCategorySummaryDto
    {
        public int Total { get; set; }
        public int[] Monthly { get; set; } = new int[12]; // Jan to Dec
    }
}
