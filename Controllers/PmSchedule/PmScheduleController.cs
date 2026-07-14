using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pm.Helper;
using Pm.DTOs;
using Pm.Services.PmSchedule;

namespace Pm.Controllers.PmSchedule
{
    [ApiController]
    [Route("api/pm-schedules")]
    [Authorize] // Assuming you want this to be protected
    public class PmScheduleController : ControllerBase
    {
        private readonly IPmScheduleService _pmScheduleService;
        private readonly ILogger<PmScheduleController> _logger;

        public PmScheduleController(IPmScheduleService pmScheduleService, ILogger<PmScheduleController> logger)
        {
            _pmScheduleService = pmScheduleService;
            _logger = logger;
        }

        private int CurrentUserId
        {
            get
            {
                var claim = User.FindFirst("UserId")?.Value
                           ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(claim, out int id))
                    throw new UnauthorizedAccessException("User ID tidak ditemukan di token.");

                return id;
            }
        }

        [HttpGet("yearly")]
        [Authorize(Policy = "PmScheduleView")]
        public async Task<IActionResult> GetYearlySchedule([FromQuery] int year)
        {
            if (year <= 0)
                return ApiResponse.BadRequest("Invalid parameter", "Invalid year");

            try
            {
                var result = await _pmScheduleService.GetYearlyScheduleAsync(year);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting yearly PM schedule for {Year}", year);
                return ApiResponse.InternalServerError("Gagal mengambil jadwal PM tahunan: " + ex.Message);
            }
        }

        [HttpPost("upsert")]
        [Authorize(Policy = "PmScheduleUpdate")]
        public async Task<IActionResult> UpsertSchedule(PmScheduleUpsertDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var success = await _pmScheduleService.UpsertScheduleAsync(dto, CurrentUserId);
                if (!success)
                    return ApiResponse.InternalServerError("Gagal menyimpan update jadwal PM");

                return ApiResponse.Success(new { }, "Jadwal PM berhasil diupdate");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting PM schedule");
                return ApiResponse.InternalServerError("Gagal mengupdate jadwal PM: " + ex.Message);
            }
        }

        [HttpDelete("{year}/{pmSiteId}/{deviceName}")]
        [Authorize(Policy = "PmScheduleUpdate")]
        public async Task<IActionResult> DeleteSchedule(int year, int pmSiteId, string deviceName)
        {
            try
            {
                var success = await _pmScheduleService.DeleteScheduleAsync(year, pmSiteId, Uri.UnescapeDataString(deviceName), CurrentUserId);
                if (!success)
                    return ApiResponse.NotFound("Jadwal PM tidak ditemukan");

                return ApiResponse.Success(new { }, "Jadwal PM berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PM schedule");
                return ApiResponse.InternalServerError("Gagal menghapus jadwal PM: " + ex.Message);
            }
        }

        [HttpPost("tasks/{taskId}/toggle-complete")]
        [Authorize(Policy = "PmScheduleUpdate")]
        public async Task<IActionResult> ToggleTaskCompletion(int taskId, [FromBody] PmTaskToggleDto dto)
        {
            try
            {
                var success = await _pmScheduleService.ToggleTaskCompletionAsync(taskId, dto.Remarks, dto.CompletedAt, CurrentUserId);
                if (!success)
                    return ApiResponse.NotFound("Tugas PM tidak ditemukan");

                return ApiResponse.Success(new { }, "Status tugas PM berhasil diubah");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling PM schedule task completion for task {TaskId}", taskId);
                return ApiResponse.InternalServerError("Gagal mengubah status tugas PM: " + ex.Message);
            }
        }
    }
}
