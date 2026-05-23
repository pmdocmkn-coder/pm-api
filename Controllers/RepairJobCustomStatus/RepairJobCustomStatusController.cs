using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.RepairJobCustomStatus;
using Pm.Helper;
using Pm.Services.RepairJobCustomStatus;

namespace Pm.Controllers.RepairJobCustomStatus
{
    [ApiController]
    [Route("api/repair-job-custom-statuses")]
    [Authorize]
    public class RepairJobCustomStatusController : ControllerBase
    {
        private readonly IRepairJobCustomStatusService _service;

        public RepairJobCustomStatusController(IRepairJobCustomStatusService service)
            => _service = service;

        private int CurrentUserId =>
            int.Parse(User.FindFirst("UserId")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException());

        /// <summary>Semua user yang bisa lihat dashboard perbaikan bisa lihat daftar status custom.</summary>
        [HttpGet]
        [Authorize(Policy = "RadioRepairView")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _service.GetAllAsync();
                return ApiResponse.Success(data);
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPost]
        [Authorize(Policy = "RadioRepairSupervise")]
        public async Task<IActionResult> Create([FromBody] CreateRepairJobCustomStatusDto dto)
        {
            try
            {
                var data = await _service.CreateAsync(dto, CurrentUserId);
                return ApiResponse.Success(data, "Status berhasil ditambahkan");
            }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("status", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}")]
        [Authorize(Policy = "RadioRepairSupervise")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRepairJobCustomStatusDto dto)
        {
            try
            {
                var data = await _service.UpdateAsync(id, dto);
                return ApiResponse.Success(data, "Status diperbarui");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("status", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RadioRepairSupervise")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return ApiResponse.Success(null, "Status dihapus");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("status", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }
    }
}
