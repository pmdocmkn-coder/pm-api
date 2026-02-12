using Pm.DTOs.CallRecord;

namespace Pm.Services
{
    public interface IExcelExportService
    {
        Task<byte[]> ExportDailySummaryToExcelAsync(DateTime date, DailySummaryDto summary);
        Task<byte[]> ExportOverallSummaryToExcelAsync(DateTime startDate, DateTime endDate, OverallSummaryDto summary);
        Task<byte[]> ExportMultipleDailySummariesToExcelAsync(DateTime startDate, DateTime endDate, OverallSummaryDto summary);
        Task<byte[]> ExportUniqueCallersToExcelAsync(string calledFleet, DateTime startDate, DateTime endDate, List<UniqueCallerDetailDto> details);
        Task<byte[]> ExportRadioDataToExcelAsync<T>(List<T> data, string sheetName);
    }
}