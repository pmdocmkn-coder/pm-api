using Pm.DTOs.CallRecord;
using Pm.DTOs.Common;

namespace Pm.Services
{
    public interface ICallRecordService
    {
        Task<UploadCsvResponseDto> ImportCsvAsync(Stream csvStream, string fileName);
        Task<byte[]> ExportCallRecordsToCsvAsync(DateTime startDate, DateTime endDate);
        Task<PagedResultDto<CallRecordDto>> GetCallRecordsAsync(CallRecordQueryDto query);
        Task<DailySummaryDto> GetDailySummaryAsync(DateTime date);
        Task<OverallSummaryDto> GetOverallSummaryAsync(DateTime startDate, DateTime endDate);
        Task<List<HourlySummaryDto>> GetHourlySummaryAsync(DateTime date);
        Task<bool> RegenerateSummariesAsync(DateTime startDate, DateTime endDate);
        Task<bool> DeleteCallRecordsAsync(DateTime date);
        Task<bool> IsFileAlreadyImported(string fileName);

        Task ResetAllDataAsync();

        // Fleet Statistics - Updated signature
        Task<FleetStatisticsDto> GetFleetStatisticsAsync(
            DateTime? startDate,
            DateTime? endDate,
            int top = 10,
            FleetStatisticType? type = null,
            string sortOrder = "DESC",
            string? callerSearch = null,
            string? calledSearch = null);

        // New: Get unique callers/called details for a specific fleet
        Task<List<UniqueCallerDetailDto>> GetUniqueCallersForFleetAsync(
            string calledFleet,
            DateTime? startDate,
            DateTime? endDate);

        Task<List<UniqueCalledDetailDto>> GetUniqueCalledFleetsForCallerAsync(
            string callerFleet,
            DateTime? startDate,
            DateTime? endDate);

        Task BulkInsertFleetStatisticsAsync(List<Models.FleetStatistic> stats);

        // Rebuild FleetStatistics from raw CallRecords (for data correction)
        Task<int> RebuildFleetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
