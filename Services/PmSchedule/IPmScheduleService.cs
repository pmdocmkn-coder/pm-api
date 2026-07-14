using System.Threading.Tasks;
using Pm.DTOs;

namespace Pm.Services.PmSchedule
{
    public interface IPmScheduleService
    {
        Task<PmYearlyScheduleResponseDto> GetYearlyScheduleAsync(int year);
        Task<bool> UpsertScheduleAsync(PmScheduleUpsertDto dto, int userId);
        Task<bool> DeleteScheduleAsync(int year, int pmSiteId, string deviceName, int userId);
        Task<bool> ToggleTaskCompletionAsync(int taskId, string? remarks, DateTime? completedAt, int userId);
        Task<PmComplianceDashboardDto> GetComplianceDashboardAsync(int year);
    }
}
