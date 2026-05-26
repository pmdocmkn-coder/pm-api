using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.RadioHandover;
using Pm.Helper;
using Pm.Services.RadioHandover;

namespace Pm.Controllers.RadioHandover
{
    [ApiController]
    [Route("api/radio-handovers")]
    [Authorize]
    public class RadioHandoverController : ControllerBase
    {
        private readonly IRadioHandoverService _service;

        public RadioHandoverController(IRadioHandoverService service) => _service = service;

        private int CurrentUserId =>
            int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException());

        private string? RoleName => User.FindFirst("RoleName")?.Value;

        private IActionResult? GuardArchiveQuery(bool includeDeleted)
        {
            if (includeDeleted && !User.HasClaim("Permission", "radio.handover.view.archive"))
                return ApiResponse.Forbidden();
            return null;
        }

        [HttpGet]
        [Authorize(Policy = "RadioHandoverView")]
        public async Task<IActionResult> GetAll([FromQuery] RadioHandoverQueryDto query)
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

        [HttpGet("{id}")]
        [Authorize(Policy = "RadioHandoverView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _service.GetByIdAsync(id);
                if (data == null) return ApiResponse.NotFound("Serah terima tidak ditemukan");
                return ApiResponse.Success(data);
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateRadioHandoverDto dto)
        {
            var allowed = dto.HandoverType switch
            {
                Enums.RadioHandoverType.HelpdeskToTechnician => HandoverPermissionHelper.CanCreateHelpdeskToTechnician(User),
                Enums.RadioHandoverType.TechnicianToWarehouse => HandoverPermissionHelper.CanCreateTechnicianToWarehouse(User),
                Enums.RadioHandoverType.WarehouseToHelpdesk => HandoverPermissionHelper.CanCreateWarehouseToHelpdesk(User),
                _ => false
            };
            if (!allowed)
                return ApiResponse.Forbidden();

            try
            {
                var data = await _service.CreateAsync(dto, CurrentUserId);
                return ApiResponse.Created(data, "Serah terima berhasil dibuat");
            }
            catch (ArgumentException ex) { return ApiResponse.BadRequest("handover", new[] { ex.Message }); }
            catch (UnauthorizedAccessException) { return ApiResponse.Forbidden(); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("handover", new[] { ex.Message }); }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpGet("technicians")]
        [Authorize(Policy = "RadioHandoverCreateHd")]
        public async Task<IActionResult> GetTechnicians()
        {
            try
            {
                return ApiResponse.Success(await _service.GetTechniciansAsync());
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpGet("warehouse-receivers")]
        [Authorize(Policy = "RadioHandoverCreateTekWh")]
        public async Task<IActionResult> GetWarehouseReceivers()
        {
            try
            {
                return ApiResponse.Success(await _service.GetWarehouseReceiversAsync());
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/complete-receiver-signature")]
        [Authorize]
        public async Task<IActionResult> CompleteReceiverSignature(int id, [FromBody] CompleteReceiverSignatureDto dto)
        {
            try
            {
                var data = await _service.CompleteReceiverSignatureAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data, "TTD penerima berhasil disimpan");
            }
            catch (UnauthorizedAccessException) { return ApiResponse.Forbidden(); }
            catch (ArgumentException ex) { return ApiResponse.BadRequest("signature", new[] { ex.Message }); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("handover", new[] { ex.Message }); }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpGet("helpdesk-receivers")]
        [Authorize(Policy = "RadioHandoverCreateWhHd")]
        public async Task<IActionResult> GetHelpdeskReceivers()
        {
            try
            {
                return ApiResponse.Success(await _service.GetHelpdeskReceiversAsync());
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}")]
        [Authorize(Policy = "RadioHandoverEdit")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRadioHandoverDto dto)
        {
            try
            {
                var data = await _service.UpdateAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data, "Serah terima diperbarui");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RadioHandoverDelete")]
        public async Task<IActionResult> SoftDelete(int id)
        {
            try
            {
                await _service.SoftDeleteAsync(id, CurrentUserId);
                return ApiResponse.Success(null, "Serah terima dipindah ke arsip");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("handover", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/restore")]
        [Authorize(Policy = "RadioHandoverViewArchive")]
        public async Task<IActionResult> Restore(int id)
        {
            try
            {
                await _service.RestoreAsync(id, CurrentUserId);
                return ApiResponse.Success(null, "Serah terima dipulihkan");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpDelete("{id}/permanent")]
        [Authorize(Policy = "RadioHandoverDeletePermanent")]
        public async Task<IActionResult> DeletePermanent(int id)
        {
            try
            {
                await _service.DeletePermanentAsync(id, CurrentUserId);
                return ApiResponse.Success(null, "Serah terima dihapus permanen");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("handover", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }
    }
}
