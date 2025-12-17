using Pm.DTOs.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    // === HISTORY CRUD ===
    public class NecRslHistoryQueryDto : BaseQueryDto
    {
        /// <summary>
        /// Filter opsional berdasarkan Link ID
        /// </summary>
        public int? NecLinkId { get; set; }

        /// <summary>
        /// Filter JSON untuk filtering lanjutan (AND/OR nested)
        /// </summary>
        public string? FiltersJson { get; set; }

        protected override string[] AllowedSortFields => new[]
        {
            "Date",
            "RslNearEnd",
            "RslFarEnd",
            "LinkName",
            "NearEndTower",
            "FarEndTower"
        };
    }

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

        // Nomor urut untuk tabel
        public int No { get; set; }

        public static List<NecRslHistoryItemDto> ApplyListNumbers(List<NecRslHistoryItemDto> items, int startIndex)
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].No = startIndex + i + 1;
            }
            return items;
        }
    }

    public class NecRslHistoryCreateDto
    {
        public int NecLinkId { get; set; }
        public DateTime Date { get; set; }
        public decimal RslNearEnd { get; set; }
        public decimal? RslFarEnd { get; set; }
    }

    public class NecRslHistoryUpdateDto
    {
        public decimal RslNearEnd { get; set; }
        public decimal? RslFarEnd { get; set; }
    }

    // === MONTHLY & YEARLY HISTORY ===
    public class NecLinkMonthlyDto
    {
        public string LinkName { get; set; } = null!;
        public decimal AvgRsl { get; set; }
        public string Status { get; set; } = "normal";
        public string? WarningMessage { get; set; }
    }

    public class NecTowerMonthlyDto
    {
        public string TowerName { get; set; } = null!;
        public List<NecLinkMonthlyDto> Links { get; set; } = new();
    }

    public class NecMonthlyHistoryResponseDto
    {
        public string Period { get; set; } = null!;
        public List<NecTowerMonthlyDto> Data { get; set; } = new();
    }

    public class NecLinkYearlyDto
    {
        public Dictionary<string, decimal> MonthlyAvg { get; set; } = new();
        public decimal YearlyAvg { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    public class NecTowerYearlyDto
    {
        public string TowerName { get; set; } = null!;
        public Dictionary<string, NecLinkYearlyDto> Links { get; set; } = new();
    }

    public class NecYearlySummaryDto
    {
        public int Year { get; set; }
        public List<NecTowerYearlyDto> Towers { get; set; } = new();
    }

    // === IMPORT ===
    public class NecSignalImportRequestDto
    {
        public IFormFile ExcelFile { get; set; } = null!;
    }

    public class NecSignalImportResultDto
    {
        public int TotalRowsProcessed { get; set; }
        public int SuccessfulInserts { get; set; }
        public int FailedRows { get; set; }
        public List<string> Errors { get; set; } = new();
        public string Message { get; set; } = "Import selesai.";
    }

    // === CRUD TOWER ===
    public class TowerListDto
    {
        public int Id { get; set; }
        public string? Name { get; set; } = null!;
        public string? Location { get; set; }  // Bisa null
        public int? LinkCount { get; set; }
    }

    public class TowerCreateDto
    {
        [Required(ErrorMessage = "Nama tower wajib diisi")]
        public string Name { get; set; } = null!;
        
        public string? Location { get; set; }
    }

    public class TowerUpdateDto : TowerCreateDto
    {
        [Required(ErrorMessage = "ID tower wajib diisi")]
        public int Id { get; set; }
        
        [StringLength(100, ErrorMessage = "Nama tower maksimal 100 karakter")]
        public new string? Name { get; set; }
        
        [StringLength(200, ErrorMessage = "Lokasi maksimal 200 karakter")]
        public new string? Location { get; set; }
    }

    // === CRUD LINK ===
    public class NecLinkListDto
    {
        public int Id { get; set; }
        public string LinkName { get; set; } = null!;
        public string NearEndTower { get; set; } = null!;
        public string FarEndTower { get; set; } = null!;
        public decimal ExpectedRslMin { get; set; }
        public decimal ExpectedRslMax { get; set; }
    }

    public class NecLinkCreateDto
    {
        public string LinkName { get; set; } = null!;
        public int NearEndTowerId { get; set; }
        public int FarEndTowerId { get; set; }
        public decimal ExpectedRslMin { get; set; } = -60m;
        public decimal ExpectedRslMax { get; set; } = -25m;
    }

    public class NecLinkUpdateDto : NecLinkCreateDto
    {
        public int Id { get; set; }
    }
}