using System.Text.Json.Serialization;

namespace Pm.DTOs
{
    public class SihepiApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("rows")]
        public List<SihepiTicketDto>? Rows { get; set; }
    }

    public class SihepiTicketDto
    {
        [JsonPropertyName("ticket_no")]
        public string? TicketNo { get; set; }

        [JsonPropertyName("wo_no")]
        public string? WoNo { get; set; }

        [JsonPropertyName("requested_by")]
        public string? RequestedBy { get; set; }

        [JsonPropertyName("division")]
        public string? Division { get; set; }

        [JsonPropertyName("department")]
        public string? Department { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("creation_date")]
        public string? CreationDate { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        [JsonPropertyName("problem_code")]
        public string? ProblemCode { get; set; }
    }
}
