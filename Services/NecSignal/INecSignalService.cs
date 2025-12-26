using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Pm.DTOs;
using Pm.DTOs.Common;

namespace Pm.Services
{
    public interface INecSignalService
    {
        // Monthly & Yearly Summary
        Task<NecMonthlyHistoryResponseDto> GetMonthlyAsync(int year, int month);
        Task<NecYearlySummaryDto> GetYearlyAsync(int year);
        Task<List<NecYearlyPivotDto>> GetYearlyPivotAsync(int year, string? towerName = null);

        // Import & Export
        Task<NecSignalImportResultDto> ImportFromPivotExcelAsync(IFormFile file, int userId);
        Task<byte[]> ExportYearlyToExcelAsync(int year, string? tower = null, int? userId = null);

        // CRUD Tower
        Task<List<TowerListDto>> GetTowersAsync();
        Task<TowerListDto> CreateTowerAsync(TowerCreateDto dto, int userId);
        Task<TowerListDto> UpdateTowerAsync(TowerUpdateDto dto, int userId);
        Task DeleteTowerAsync(int id, int userId);

        // CRUD Link
        Task<List<NecLinkListDto>> GetLinksAsync();
        Task<NecLinkListDto> CreateLinkAsync(NecLinkCreateDto dto, int userId);
        Task<NecLinkListDto> UpdateLinkAsync(NecLinkUpdateDto dto, int userId);
        Task DeleteLinkAsync(int id, int userId);

        // CRUD History RSL
        Task<PagedResultDto<NecRslHistoryItemDto>> GetHistoriesAsync(NecRslHistoryQueryDto query);
        Task<NecRslHistoryItemDto?> GetHistoryByIdAsync(int id);
        Task<NecRslHistoryItemDto> CreateHistoryAsync(NecRslHistoryCreateDto dto, int userId);
        Task<NecRslHistoryItemDto> UpdateHistoryAsync(int id, NecRslHistoryUpdateDto dto, int userId);
        Task DeleteHistoryAsync(int id, int userId);
    }
}