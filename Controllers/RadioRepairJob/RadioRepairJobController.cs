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

        private bool HasPermission(string name) =>
            User.HasClaim("Permission", name);

        private IActionResult? GuardArchiveQuery(bool includeDeleted)
        {
            if (includeDeleted && !HasPermission("radio.repair.view.archive"))
                return ApiResponse.Forbidden();
            return null;
        }

        [HttpGet]
        [Authorize(Policy = "RadioRepairView")]
        public async Task<IActionResult> GetAll([FromQuery] RadioRepairJobQueryDto query)
        {
            var guard = GuardArchiveQuery(query.IncludeDeleted);
            if (guard != null) return guard;
            try
            {
                var data = await _service.GetAllAsync(query, CurrentUserId, RoleName);
                return ApiResponse.Success(data);
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpGet("by-ticket")]
        [Authorize(Policy = "RadioRepairView")]
        public async Task<IActionResult> GetByTicket([FromQuery] RadioRepairJobQueryDto query, [FromQuery] bool includeDeleted = false)
        {
            var guard = GuardArchiveQuery(includeDeleted);
            if (guard != null) return guard;
            try
            {
                var data = await _service.GetGroupedByTicketAsync(query, CurrentUserId, RoleName, includeDeleted);
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
        public async Task<IActionResult> GetById(int id, [FromQuery] bool includeDeleted = false)
        {
            var guard = GuardArchiveQuery(includeDeleted);
            if (guard != null) return guard;
            try
            {
                var data = await _service.GetByIdAsync(id, CurrentUserId, RoleName);
                if (data == null) return ApiResponse.NotFound("Pekerjaan tidak ditemukan");
                if (data.IsDeleted && !includeDeleted)
                    return ApiResponse.NotFound("Pekerjaan tidak ditemukan");
                return ApiResponse.Success(data);
            }
            catch (UnauthorizedAccessException) { return ApiResponse.Forbidden(); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}")]
        [Authorize(Policy = "RadioRepairEdit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRadioRepairJobDto dto)
        {
            try
            {
                var data = await _service.UpdateAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data, "Pekerjaan diperbarui");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (ArgumentException ex) { return ApiResponse.BadRequest("job", new[] { ex.Message }); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("job", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        /// <summary>
        /// Update oleh teknisi — hanya keterangan kerusakan yang boleh diubah.
        /// Perubahan dicatat di timeline (StatusLogs) dengan nama teknisi yang mengubah.
        /// </summary>
        [HttpPatch("{id}/notes")]
        [Authorize(Policy = "RadioRepairUpdate")]
        public async Task<IActionResult> TechnicianUpdate(int id, [FromBody] TechnicianUpdateRepairJobDto dto)
        {
            try
            {
                var data = await _service.TechnicianUpdateAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data, "Keterangan diperbarui");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("job", new[] { ex.Message }); }
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
        public async Task<IActionResult> SoftDelete(int id)
        {
            try
            {
                await _service.SoftDeleteAsync(id, CurrentUserId);
                return ApiResponse.Success(null, "Pekerjaan dipindah ke arsip");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("job", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/restore")]
        [Authorize(Policy = "RadioRepairViewArchive")]
        public async Task<IActionResult> Restore(int id)
        {
            try
            {
                await _service.RestoreAsync(id, CurrentUserId);
                return ApiResponse.Success(null, "Pekerjaan dipulihkan");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpDelete("{id}/permanent")]
        [Authorize(Policy = "RadioRepairDeletePermanent")]
        public async Task<IActionResult> DeletePermanent(int id)
        {
            try
            {
                await _service.DeletePermanentAsync(id, CurrentUserId);
                return ApiResponse.Success(null, "Pekerjaan dihapus permanen");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("job", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }
    }
}
