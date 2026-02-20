using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;

namespace Pm.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(string module, int? entityId, string action, int userId, string description);
        Task<PagedResultDto<ActivityLog>> GetActivityLogsAsync(ActivityLogQueryDto dto);
    }
}