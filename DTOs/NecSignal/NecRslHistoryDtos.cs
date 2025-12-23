// Pm.DTOs/NecRslHistoryDtos.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Pm.Enums;

namespace Pm.DTOs
{
    public class NecRslHistoryItemDto
    {
        public int Id { get; set; }
        public int NecLinkId { get; set; }
        public string LinkName { get; set; } = null!;
        public string NearEndTower { get; set; } = null!;
        public string FarEndTower { get; set; } = null!;
        public DateTime Date { get; set; }
        public decimal RslNearEnd { get; set; }
        public decimal? RslFarEnd { get; set; }
        public string? Notes { get; set; }
        
        // ✅ Return as enum (untuk serialization ke frontend)
        public NecOperationalStatus Status { get; set; } = NecOperationalStatus.Active;
        public string StatusString => Status.ToString();
        
        public int No { get; set; }

        public static void ApplyListNumbers(List<NecRslHistoryItemDto> items, int startIndex)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].No = startIndex + i + 1;
            }
        }
    }

    // ✅ UBAH: Accept string dari frontend
    public class NecRslHistoryCreateDto
    {
        [Required]
        public int NecLinkId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        public decimal RslNearEnd { get; set; }
        public decimal? RslFarEnd { get; set; }
        public string? Notes { get; set; }
        
        // ✅ Accept string: "active", "dismantled", "removed", "obstacle"
        public string? Status { get; set; }
    }

    // ✅ UBAH: Accept string dari frontend
    public class NecRslHistoryUpdateDto
    {
        public decimal RslNearEnd { get; set; }
        public decimal? RslFarEnd { get; set; }
        public string? Notes { get; set; }
        
        // ✅ Accept string: "active", "dismantled", "removed", "obstacle"
        public string? Status { get; set; }
    }

    public class NecRslHistoryQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public string? Search { get; set; }
        public int? NecLinkId { get; set; }
        public string? SortBy { get; set; }
        public string? SortDir { get; set; } = "desc";
        public string? FiltersJson { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var validSortFields = new[] { "Date", "RslNearEnd", "RslFarEnd", "LinkName", "NearEndTower", "FarEndTower" };
            if (!string.IsNullOrWhiteSpace(SortBy) && !validSortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
            {
                yield return new ValidationResult($"SortBy harus salah satu dari: {string.Join(", ", validSortFields)}");
            }
        }
    }
}