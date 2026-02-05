using Pm.DTOs.Common;

namespace Pm.DTOs.Radio
{
    public class RadioTrunkingQueryDto : BaseQueryDto
    {
        public string? Status { get; set; }
        public string? Dept { get; set; }
        public string? Fleet { get; set; }

        protected override string[] AllowedSortFields =>
            ["id", "unitNumber", "radioId", "dept", "fleet", "status", "createdAt", "dateProgram"];
    }

    public class RadioConventionalQueryDto : BaseQueryDto
    {
        public string? Status { get; set; }
        public string? Dept { get; set; }
        public string? Fleet { get; set; }

        protected override string[] AllowedSortFields =>
            ["id", "unitNumber", "radioId", "dept", "fleet", "status", "createdAt"];
    }

    public class RadioGrafirQueryDto : BaseQueryDto
    {
        public string? Status { get; set; }
        public string? Div { get; set; }
        public string? Dept { get; set; }

        protected override string[] AllowedSortFields =>
            ["id", "noAsset", "serialNumber", "typeRadio", "div", "dept", "status", "createdAt", "tanggal"];
    }

    public class RadioScrapQueryDto : BaseQueryDto
    {
        public string? ScrapCategory { get; set; } // Trunking, Conventional
        public int? Year { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        protected override string[] AllowedSortFields =>
            ["id", "scrapCategory", "typeRadio", "serialNumber", "dateScrap", "createdAt"];
    }
}
