using Microsoft.AspNetCore.Http;
using Pm.DTOs;
using Pm.DTOs.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pm.Services
{
    public interface ISwrSignalService
    {
        // Monthly & Yearly Summary
        Task<SwrMonthlyHistoryResponseDto> GetMonthlyAsync(int year, int month);
        Task<SwrYearlySummaryDto> GetYearlyAsync(int year);
        Task<List<SwrYearlyPivotDto>> GetYearlyPivotAsync(int year, string? siteName = null);

        // Import & Export
        Task<SwrImportResultDto> ImportFromExcelAsync(IFormFile file, int userId);
        Task<byte[]> ExportYearlyToExcelAsync(int year, List<string>? sites = null, string? type = null, string? search = null, int? userId = null);

        // CRUD Site
        Task<List<SwrSiteListDto>> GetSitesAsync();
        Task<SwrSiteListDto> CreateSiteAsync(SwrSiteCreateDto dto, int userId);
        Task<SwrSiteListDto> UpdateSiteAsync(SwrSiteUpdateDto dto, int userId);
        Task DeleteSiteAsync(int id, int userId);

        // CRUD Channel
        Task<List<SwrChannelListDto>> GetChannelsAsync();
        Task<SwrChannelListDto> CreateChannelAsync(SwrChannelCreateDto dto, int userId);
        Task<SwrChannelListDto> UpdateChannelAsync(SwrChannelUpdateDto dto, int userId);
        Task DeleteChannelAsync(int id, int userId);

        // CRUD History
        Task<PagedResultDto<SwrHistoryItemDto>> GetHistoriesAsync(SwrHistoryQueryDto query);
        Task<SwrHistoryItemDto?> GetHistoryByIdAsync(int id);
        Task<SwrHistoryItemDto> CreateHistoryAsync(SwrHistoryCreateDto dto, int userId);
        Task<SwrHistoryItemDto> UpdateHistoryAsync(int id, SwrHistoryUpdateDto dto, int userId);
        Task DeleteHistoryAsync(int id, int userId);
    }
}