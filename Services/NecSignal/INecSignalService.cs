using Microsoft.AspNetCore.Http;
using Pm.DTOs;
using Pm.DTOs.Common;

namespace Pm.Services
{
    public interface INecSignalService
    {
        Task<NecMonthlyHistoryResponseDto> GetMonthlyAsync(int year, int month);
        Task<NecYearlySummaryDto> GetYearlyAsync(int year);
        Task<NecSignalImportResultDto> ImportFromExcelAsync(IFormFile file, int userId);
        Task<byte[]> ExportYearlyToExcelAsync(int year, string? towerName = null, int? userId = null);

        Task<List<TowerListDto>> GetTowersAsync();
        Task<TowerListDto> CreateTowerAsync(TowerCreateDto dto, int userId);
        Task<TowerListDto> UpdateTowerAsync(TowerUpdateDto dto, int userId);
        Task DeleteTowerAsync(int id, int userId);

        Task<List<NecLinkListDto>> GetLinksAsync();
        Task<NecLinkListDto> CreateLinkAsync(NecLinkCreateDto dto, int userId);
        Task<NecLinkListDto> UpdateLinkAsync(NecLinkUpdateDto dto, int userId);
        Task DeleteLinkAsync(int id, int userId);

        // CRUD NecRslHistory
        Task<PagedResultDto<NecRslHistoryItemDto>> GetHistoriesAsync(NecRslHistoryQueryDto query);
        Task<NecRslHistoryItemDto?> GetHistoryByIdAsync(int id);
        Task<NecRslHistoryItemDto> CreateHistoryAsync(NecRslHistoryCreateDto dto, int userId);
        Task<NecRslHistoryItemDto> UpdateHistoryAsync(int id, NecRslHistoryUpdateDto dto, int userId);
        Task DeleteHistoryAsync(int id, int userId);
    }
}