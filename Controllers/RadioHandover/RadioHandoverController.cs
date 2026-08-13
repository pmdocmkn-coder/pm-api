using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pm.Data;
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
        private readonly AppDbContext _context;

        public RadioHandoverController(IRadioHandoverService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

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

        /// <summary>Batch fetch preview thumbnails for multiple handover IDs (lightweight, no full detail).</summary>
        [HttpGet("thumbnails")]
        [Authorize(Policy = "RadioHandoverView")]
        public async Task<IActionResult> GetThumbnails([FromQuery] string ids)
        {
            if (string.IsNullOrWhiteSpace(ids)) return ApiResponse.Success(new Dictionary<int, string?>());
            var idList = ids.Split(',').Select(s => int.TryParse(s.Trim(), out var v) ? v : -1).Where(v => v > 0).Distinct().Take(50).ToList();
            if (idList.Count == 0) return ApiResponse.Success(new Dictionary<int, string?>());

            try
            {
                // Fetch from Photos table first (multi-photo), fallback to legacy RadioPhotoBase64
                var fromPhotos = await _context.Set<Pm.Models.RadioHandoverPhoto>()
                    .Where(p => idList.Contains(p.RadioHandoverId))
                    .GroupBy(p => p.RadioHandoverId)
                    .Select(g => new { Id = g.Key, Photo = g.OrderBy(p => p.SortOrder).Select(p => p.PhotoBase64).FirstOrDefault() })
                    .ToDictionaryAsync(x => x.Id, x => x.Photo);

                // Find IDs not covered by Photos table, try legacy column
                var missingIds = idList.Where(id => !fromPhotos.ContainsKey(id) || fromPhotos[id] == null).ToList();
                var fromLegacy = missingIds.Count > 0
                    ? await _context.RadioHandovers.AsNoTracking()
                        .Where(h => missingIds.Contains(h.Id) && h.RadioPhotoBase64 != null)
                        .Select(h => new { h.Id, h.RadioPhotoBase64 })
                        .ToDictionaryAsync(x => x.Id, x => x.RadioPhotoBase64)
                    : new Dictionary<int, string?>();

                var result = new Dictionary<int, string?>();
                foreach (var id in idList)
                {
                    if (fromPhotos.TryGetValue(id, out var photo) && photo != null) result[id] = photo;
                    else if (fromLegacy.TryGetValue(id, out var legacy)) result[id] = legacy;
                }
                return ApiResponse.Success(result);
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
                Enums.RadioHandoverType.TechnicianToHelpdesk => HandoverPermissionHelper.CanCreateTechnicianToHelpdesk(User),
                Enums.RadioHandoverType.HelpdeskToWarehouse => HandoverPermissionHelper.CanCreateHelpdeskToWarehouse(User),
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
        [Authorize(Policy = "RadioHandoverGetWarehouseReceivers")]
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

        [HttpPatch("{id}/reset-receiver-signature")]
        [Authorize]
        public async Task<IActionResult> ResetReceiverSignature(int id)
        {
            try
            {
                var data = await _service.ResetReceiverSignatureAsync(id, CurrentUserId);
                return ApiResponse.Success(data, "TTD penerima berhasil direset, menunggu tanda tangan ulang");
            }
            catch (UnauthorizedAccessException) { return ApiResponse.Forbidden(); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("handover", new[] { ex.Message }); }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpGet("helpdesk-receivers")]
        [Authorize]
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

        [HttpPatch("{id}/cancel-pending")]
        [Authorize]
        public async Task<IActionResult> CancelPending(int id)
        {
            try
            {
                await _service.CancelPendingHandoverAsync(id, CurrentUserId);
                return ApiResponse.Success(null, "Serah terima berhasil dibatalkan");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("handover", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/change-receiver")]
        [Authorize]
        public async Task<IActionResult> ChangeReceiver(int id, [FromBody] ChangeReceiverDto dto)
        {
            try
            {
                var data = await _service.ChangeReceiverAsync(id, dto.NewReceiverUserId, CurrentUserId);
                return ApiResponse.Success(data, "Penerima berhasil diubah");
            }
            catch (UnauthorizedAccessException) { return ApiResponse.Forbidden(); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("handover", new[] { ex.Message }); }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
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
