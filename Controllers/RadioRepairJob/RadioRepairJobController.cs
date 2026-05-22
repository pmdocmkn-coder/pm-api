using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.RadioRepairJob;
using Pm.Helper;
using Pm.Services.RadioRepairJob;

namespace Pm.Controllers.RadioRepairJob
{
    [ApiController]
    [Route("api/radio-repair-jobs")]
    [Authorize]
    public class RadioRepairJobController : ControllerBase
    {
        private readonly IRadioRepairJobService _service;

        public RadioRepairJobController(IRadioRepairJobService service) => _service = service;

        private int CurrentUserId =>
            int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException());

        private string? RoleName => User.FindFirst("RoleName")?.Value;

        [HttpGet]
        [Authorize(Policy = "RadioRepairView")]
        public async Task<IActionResult> GetAll([FromQuery] RadioRepairJobQueryDto query)
        {
            try
            {
                var data = await _service.GetAllAsync(query, CurrentUserId, RoleName);
                return ApiResponse.Success(data);
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpGet("dashboard")]
        [Authorize(Policy = "RadioRepairView")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var data = await _service.GetDashboardAsync(CurrentUserId, RoleName);
                return ApiResponse.Success(data);
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "RadioRepairView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _service.GetByIdAsync(id, CurrentUserId, RoleName);
                if (data == null) return ApiResponse.NotFound("Job tidak ditemukan");
                return ApiResponse.Success(data);
            }
            catch (UnauthorizedAccessException) { return ApiResponse.Forbidden(); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/status")]
        [Authorize(Policy = "RadioRepairUpdate")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRadioRepairJobStatusDto dto)
        {
            try
            {
                var data = await _service.UpdateStatusAsync(id, dto, CurrentUserId, RoleName);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (UnauthorizedAccessException) { return ApiResponse.Forbidden(); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("status", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/approve-material")]
        [Authorize(Policy = "RadioRepairSupervise")]
        public async Task<IActionResult> ApproveMaterial(int id, [FromBody] ApproveMaterialDto dto)
        {
            try
            {
                var data = await _service.ApproveMaterialAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("status", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RadioRepairDelete")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _service.CancelAsync(id, CurrentUserId, RoleName);
                return ApiResponse.Success(null, "Job dibatalkan");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("job", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }
    }
}
