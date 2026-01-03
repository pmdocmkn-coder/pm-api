using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Pm.Enums;

namespace Pm.DTOs
{
    public class SwrHistoryItemDto
    {
        public int Id { get; set; }
        public int SwrChannelId { get; set; }
        public string ChannelName { get; set; } = null!;
        public string SiteName { get; set; } = null!;
        public string SiteType { get; set; } = null!;
        public DateTime Date { get; set; }
        public decimal? Fpwr { get; set; }
        public decimal Vswr { get; set; }
        public string? Notes { get; set; }
        
        public SwrOperationalStatus Status { get; set; } = SwrOperationalStatus.Active;
        public string StatusString => Status.ToString();
        
        public int No { get; set; }

        public static void ApplyListNumbers(List<SwrHistoryItemDto> items, int startIndex)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].No = startIndex + i + 1;
            }
        }
    }

    public class SwrHistoryCreateDto
    {
        [Required(ErrorMessage = "Channel ID wajib diisi")]
        public int SwrChannelId { get; set; }
        
        [Required(ErrorMessage = "Tanggal wajib diisi")]
        public DateTime Date { get; set; }
        
        // FPWR optional (hanya untuk Trunking)
        [Range(0, 200, ErrorMessage = "FPWR harus antara 0-200")]
        public decimal? Fpwr { get; set; }
        
        [Required(ErrorMessage = "VSWR wajib diisi")]
        [Range(1.0, 3.0, ErrorMessage = "VSWR harus antara 1.0-3.0")]
        public decimal Vswr { get; set; }
        
        public string? Notes { get; set; }
        
        public string? Status { get; set; }
    }

    public class SwrHistoryUpdateDto
    {
        [Range(0, 200, ErrorMessage = "FPWR harus antara 0-200")]
        public decimal? Fpwr { get; set; }
        
        [Required(ErrorMessage = "VSWR wajib diisi")]
        [Range(1.0, 3.0, ErrorMessage = "VSWR harus antara 1.0-3.0")]
        public decimal Vswr { get; set; }
        
        public string? Notes { get; set; }
        
        public string? Status { get; set; }
    }

    public class SwrHistoryQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public string? Search { get; set; }
        public int? SwrChannelId { get; set; }
        public int? SwrSiteId { get; set; }
        public string? SiteType { get; set; } // "Trunking" or "Conventional"
        public string? SortBy { get; set; }
        public string? SortDir { get; set; } = "desc";
        public string? FiltersJson { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validSortFields = new[] { "Date", "Fpwr", "Vswr", "ChannelName", "SiteName" };
            if (!string.IsNullOrWhiteSpace(SortBy) && !validSortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
            {
                yield return new ValidationResult($"SortBy harus salah satu dari: {string.Join(", ", validSortFields)}");
            }
        }
    }
}