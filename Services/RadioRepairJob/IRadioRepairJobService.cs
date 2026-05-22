using Pm.DTOs.Common;
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
        Task CancelAsync(int id, int userId, string? roleName);
    }
}
