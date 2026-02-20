using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Models;
using Pm.Services;

namespace Pm.Controllers
{
    [Route("api/activity-logs")]
    [ApiController]
    [Produces("application/json")]

    public class ActivityLogController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;

        public ActivityLogController(IActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        [Authorize(Policy = "CanViewAuditLog")]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<ActivityLog>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActivityLogs([FromQuery] ActivityLogQueryDto dto)
        {
            var result = await _activityLogService.GetActivityLogsAsync(dto);
            return ApiResponse.Success(result, "Activity logs retrieved successfully");
        }
    }
}
