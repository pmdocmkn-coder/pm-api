using Pm.DTOs.Common;
using Pm.DTOs.Radio;

namespace Pm.Services
{
    public interface IRadioTrunkingService
    {
        Task<PagedResultDto<RadioTrunkingDto>> GetAllAsync(RadioTrunkingQueryDto query);
        Task<RadioTrunkingDto?> GetByIdAsync(int id);
        Task<RadioTrunkingDto> CreateAsync(CreateRadioTrunkingDto dto, int userId);
        Task<RadioTrunkingDto?> UpdateAsync(int id, UpdateRadioTrunkingDto dto, int userId);
        Task<bool> DeleteAsync(int id);
        Task<int> ClearAllAsync(int userId);
        Task<List<RadioHistoryDto>> GetHistoryAsync(int radioId);

        // Import/Export
        Task<(int success, int failed, List<string> errors)> ImportCsvAsync(Stream stream, int userId);
        Task<byte[]> ExportCsvAsync(RadioTrunkingQueryDto? query);
        byte[] GetImportTemplate();
    }

    public interface IRadioConventionalService
    {
        Task<PagedResultDto<RadioConventionalDto>> GetAllAsync(RadioConventionalQueryDto query);
        Task<RadioConventionalDto?> GetByIdAsync(int id);
        Task<RadioConventionalDto> CreateAsync(CreateRadioConventionalDto dto, int userId);
        Task<RadioConventionalDto?> UpdateAsync(int id, UpdateRadioConventionalDto dto, int userId);
        Task<bool> DeleteAsync(int id);
        Task<int> ClearAllAsync(int userId);
        Task<List<RadioHistoryDto>> GetHistoryAsync(int radioId);

        // Import/Export
        Task<(int success, int failed, List<string> errors)> ImportCsvAsync(Stream stream, int userId);
        Task<byte[]> ExportCsvAsync(RadioConventionalQueryDto? query);
        byte[] GetImportTemplate();
    }

    public interface IRadioGrafirService
    {
        Task<PagedResultDto<RadioGrafirDto>> GetAllAsync(RadioGrafirQueryDto query);
        Task<RadioGrafirDto?> GetByIdAsync(int id);
        Task<RadioGrafirDto> CreateAsync(CreateRadioGrafirDto dto, int userId);
        Task<RadioGrafirDto?> UpdateAsync(int id, UpdateRadioGrafirDto dto, int userId);
        Task<bool> DeleteAsync(int id);
        Task<int> ClearAllAsync(int userId);
        Task<List<RadioTrunkingDto>> GetLinkedTrunkingAsync(int grafirId);
        Task<List<RadioConventionalDto>> GetLinkedConventionalAsync(int grafirId);

        // Import/Export
        Task<(int success, int failed, List<string> errors)> ImportCsvAsync(Stream stream, int userId);
        Task<byte[]> ExportCsvAsync(RadioGrafirQueryDto? query);
        byte[] GetImportTemplate();
    }

    public interface IRadioScrapService
    {
        Task<PagedResultDto<RadioScrapDto>> GetAllAsync(RadioScrapQueryDto query);
        Task<RadioScrapDto?> GetByIdAsync(int id);
        Task<RadioScrapDto> CreateAsync(CreateRadioScrapDto dto, int userId);
        Task<RadioScrapDto?> ScrapFromTrunkingAsync(int trunkingId, ScrapFromRadioDto dto, int userId);
        Task<RadioScrapDto?> ScrapFromConventionalAsync(int conventionalId, ScrapFromRadioDto dto, int userId);
        Task<RadioScrapDto?> UpdateAsync(int id, CreateRadioScrapDto dto);
        Task<bool> DeleteAsync(int id);

        // Yearly Summary
        Task<YearlyScrapSummaryDto> GetYearlySummaryAsync(int year);

        // Export
        Task<byte[]> ExportCsvAsync(RadioScrapQueryDto? query);
    }
}
