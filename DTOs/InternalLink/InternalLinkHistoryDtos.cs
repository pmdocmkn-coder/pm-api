using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Pm.DTOs.Common;
using Pm.Enums;

namespace Pm.DTOs.InternalLink
{
    public class InternalLinkHistoryItemDto
    {
        public int Id { get; set; }
        public int InternalLinkId { get; set; }
        public string LinkName { get; set; } = null!;
        public DateTime Date { get; set; }
        public decimal? RslNearEnd { get; set; }
        public int? Uptime { get; set; }
        public string? Notes { get; set; }
        public bool HasScreenshot { get; set; } // flag instead of full base64 in list
        public InternalLinkStatus Status { get; set; } = InternalLinkStatus.Active;
        public string StatusString => Status.ToString();
        public int No { get; set; }

        public static void ApplyListNumbers(List<InternalLinkHistoryItemDto> items, int startIndex)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].No = startIndex + i + 1;
            }
        }
    }

    // Detail DTO includes screenshot (lazy-loaded)
    public class InternalLinkHistoryDetailDto : InternalLinkHistoryItemDto
    {
        public string? ScreenshotBase64 { get; set; }
    }

    public class InternalLinkHistoryCreateDto
    {
        [Required]
        public int InternalLinkId { get; set; }

        [Required]
        public DateTime Date { get; set; }
        public decimal? RslNearEnd { get; set; }
        public int? Uptime { get; set; }
        public string? Notes { get; set; }
        public string? ScreenshotBase64 { get; set; }
        public string? Status { get; set; }
    }

    public class InternalLinkHistoryUpdateDto
    {
        public DateTime? Date { get; set; }
        public decimal? RslNearEnd { get; set; }
        public int? Uptime { get; set; }
        public string? Notes { get; set; }
        public string? ScreenshotBase64 { get; set; }
        public bool? RemoveScreenshot { get; set; } // flag to explicitly remove screenshot
        public string? Status { get; set; }
    }

    public class InternalLinkHistoryQueryDto : BaseQueryDto
    {
        public int? InternalLinkId { get; set; }
        public string? FiltersJson { get; set; }

        protected override string[] AllowedSortFields => new[]
        {
            "Date", "Uptime", "LinkName", "Status"
        };
    }
}
