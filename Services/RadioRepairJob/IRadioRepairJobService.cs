using Pm.DTOs.Common;
using Pm.DTOs.RadioHandover;
using Pm.DTOs.RadioRepairJob;

namespace Pm.Services.RadioRepairJob
{
    public interface IRadioRepairJobService
    {
        Task<PagedResultDto<RadioRepairJobListDto>> GetAllAsync(RadioRepairJobQueryDto query, int currentUserId, string? roleName);
        Task<RadioRepairDashboardDto> GetDashboardAsync(int currentUserId, string? roleName);
        Task<RadioRepairJobDetailDto?> GetByIdAsync(int id, int currentUserId, string? roleName);
        Task<RadioRepairJobDetailDto> UpdateStatusAsync(int id, UpdateRadioRepairJobStatusDto dto, int userId, string? roleName);
        Task<RadioRepairJobDetailDto> ApproveMaterialAsync(int id, ApproveMaterialDto dto, int userId);
        Task<RadioRepairJobDetailDto> UpdateAsync(int id, UpdateRadioRepairJobDto dto, int userId);
        Task<RadioRepairJobDetailDto> TechnicianUpdateAsync(int id, TechnicianUpdateRepairJobDto dto, int userId);
        Task<RadioRepairJobDetailDto> ApproveScrapAsync(int id, ApproveScrapDto dto, int userId, string? roleName);
        Task<RadioRepairJobDetailDto> CancelScrapAsync(int id, int userId, string? roleName);
        Task SoftDeleteAsync(int id, int userId);
        Task RestoreAsync(int id, int userId);
        Task DeletePermanentAsync(int id, int userId);
        Task<List<RadioRepairTicketGroupDto>> GetGroupedByTicketAsync(
            RadioRepairJobQueryDto query, int currentUserId, string? roleName, bool includeDeleted);
        Task ResetTestingDataAsync(int userId);
        Task PurgeJobAsync(int jobId, int userId);
        Task<List<UserOptionDto>> GetTechnicianOptionsAsync();
    }
}
