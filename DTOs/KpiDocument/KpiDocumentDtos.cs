using System;
using Pm.DTOs.Common;

namespace Pm.DTOs.KpiDocument
{
    public class KpiDocumentQueryDto : BaseQueryDto
    {
        public string? PeriodMonth { get; set; }
        public string? AreaGroup { get; set; }
    }

    public class KpiDocumentDto
    {
        public int Id { get; set; }
        public DateTime PeriodMonth { get; set; }
        public required string AreaGroup { get; set; }
        public required string DocumentName { get; set; }
        public required string DataSource { get; set; }
        public string? GroupTag { get; set; }
        public DateTime? DateReceived { get; set; }
        public DateTime? DateSubmittedToReviewer { get; set; }
        public DateTime? DateApproved { get; set; }
        public DateTime? DateSubmittedToRqm { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public required string Status { get; set; }
    }

    public class CreateKpiDocumentDto
    {
        public DateTime PeriodMonth { get; set; }
        public required string AreaGroup { get; set; }
        public required string DocumentName { get; set; }
        public required string DataSource { get; set; }
        public string? GroupTag { get; set; }
    }

    public class UpdateKpiDocumentDto
    {
        public required string AreaGroup { get; set; }
        public required string DocumentName { get; set; }
        public required string DataSource { get; set; }
        public string? GroupTag { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateKpiDocumentDatesDto
    {
        public DateTime? DateReceived { get; set; }
        public DateTime? DateSubmittedToReviewer { get; set; }
        public DateTime? DateApproved { get; set; }
        public DateTime? DateSubmittedToRqm { get; set; }
        public string? Remarks { get; set; }
    }
}
