using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pm.Helper;
using Pm.DTOs.Radio;
using Pm.Services.Radio;

namespace Pm.Controllers.Radio
{
    [ApiController]
    [Route("api/radios")]
    [Authorize]
    public class RadioController(IRadioService radioService, ILogger<RadioController> logger) : ControllerBase
    {
        private readonly IRadioService _radioService = radioService;
        private readonly ILogger<RadioController> _logger = logger;

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

        // ============================================
        // GET
        // ============================================

        [HttpGet]
        [Authorize(Policy = "RadioView")]
        public async Task<IActionResult> GetAll([FromQuery] RadioQueryDto query)
        {
            try
            {
                var data = await _radioService.GetAllAsync(query);
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting radios");
                return ApiResponse.InternalServerError("Gagal mengambil data radio: " + ex.Message);
            }
        }

        [HttpGet("unpaged")]
        [Authorize(Policy = "RadioView")]
        public async Task<IActionResult> GetAllUnpaged([FromQuery] string? category = null, [FromQuery] bool isScrap = false)
        {
            try
            {
                var data = await _radioService.GetAllUnpagedAsync(category, isScrap);
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unpaged radios");
                return ApiResponse.InternalServerError("Gagal mengambil data radio: " + ex.Message);
            }
        }

        [HttpGet("duplicate-sns")]
        [Authorize(Policy = "RadioView")]
        public async Task<IActionResult> GetDuplicateSerialNumbers()
        {
            try
            {
                var data = await _radioService.GetDuplicateSerialNumbersAsync();
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting duplicate serial numbers");
                return ApiResponse.InternalServerError("Gagal mengambil data duplikat SN: " + ex.Message);
            }
        }

        // Alias untuk kompatibilitas frontend yang memanggil /api/radios/all
        [HttpGet("all")]
        [Authorize(Policy = "RadioView")]
        public async Task<IActionResult> GetAllAlias([FromQuery] string? category = null, [FromQuery] bool isScrap = false)
        {
            try
            {
                var data = await _radioService.GetAllUnpagedAsync(category, isScrap);
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all radios");
                return ApiResponse.InternalServerError("Gagal mengambil data radio: " + ex.Message);
            }
        }

        [HttpGet("lookup-by-serial")]
        [Authorize(Policy = "RadioHandoverLookup")]
        public async Task<IActionResult> LookupBySerial([FromQuery] string serialNumber)
        {
            try
            {
                var data = await _radioService.LookupBySerialAsync(serialNumber);
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error lookup radio by serial");
                return ApiResponse.InternalServerError("Gagal lookup SN: " + ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "RadioView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _radioService.GetByIdAsync(id);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException)
            {
                return ApiResponse.NotFound("Radio tidak ditemukan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting radio by id: {Id}", id);
                return ApiResponse.InternalServerError("Gagal mengambil data radio: " + ex.Message);
            }
        }

        [HttpGet("{id}/history")]
        [Authorize(Policy = "RadioView")]
        public async Task<IActionResult> GetHistory(int id)
        {
            try
            {
                var data = await _radioService.GetHistoryAsync(id);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException)
            {
                return ApiResponse.NotFound("Radio tidak ditemukan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting radio history by id: {Id}", id);
                return ApiResponse.InternalServerError("Gagal mengambil histori radio: " + ex.Message);
            }
        }

        // ============================================
        // CREATE / UPDATE / DELETE
        // ============================================

        [HttpPost]
        [Authorize(Policy = "RadioCreate")]
        public async Task<IActionResult> Create([FromBody] CreateRadioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var data = await _radioService.CreateAsync(dto, CurrentUserId);
                return ApiResponse.Created(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating radio");
                return ApiResponse.InternalServerError("Gagal membuat data radio: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RadioUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRadioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var data = await _radioService.UpdateAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data, "Data radio berhasil diupdate");
            }
            catch (KeyNotFoundException)
            {
                return ApiResponse.NotFound("Radio tidak ditemukan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating radio: {Id}", id);
                return ApiResponse.InternalServerError("Gagal mengupdate data radio: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RadioDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _radioService.DeleteAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "Data radio berhasil dihapus");
            }
            catch (KeyNotFoundException)
            {
                return ApiResponse.NotFound("Radio tidak ditemukan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting radio: {Id}", id);
                return ApiResponse.InternalServerError("Gagal menghapus data radio: " + ex.Message);
            }
        }

        [HttpDelete("all")]
        [Authorize(Policy = "RadioDelete")]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                await _radioService.DeleteAllAsync(CurrentUserId);
                return ApiResponse.Success(new { }, "Seluruh data radio berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all radios");
                return ApiResponse.InternalServerError("Gagal menghapus seluruh data radio: " + ex.Message);
            }
        }

        [HttpDelete("all/kpc")]
        [Authorize(Policy = "RadioDeletetAllKPC")]
        public async Task<IActionResult> DeleteAllKpc()
        {
            try
            {
                var count = await _radioService.DeleteByCategoryAsync("Internal", CurrentUserId);
                return ApiResponse.Success(new { deleted = count }, $"{count} data Radio KPC berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all KPC radios");
                return ApiResponse.InternalServerError("Gagal menghapus data Radio KPC: " + ex.Message);
            }
        }

        [HttpDelete("all/kontraktor")]
        [Authorize(Policy = "RadioDeletetAllKontraktor")]
        public async Task<IActionResult> DeleteAllKontraktor()
        {
            try
            {
                var count = await _radioService.DeleteByCategoryAsync("Contractor", CurrentUserId);
                return ApiResponse.Success(new { deleted = count }, $"{count} data Radio Kontraktor berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all Contractor radios");
                return ApiResponse.InternalServerError("Gagal menghapus data Radio Kontraktor: " + ex.Message);
            }
        }

        [HttpDelete("all/unit")]
        [Authorize(Policy = "RadioDeletetAllUnit")]
        public async Task<IActionResult> DeleteAllUnit()
        {
            try
            {
                var count = await _radioService.DeleteByCategoryAsync("Unit", CurrentUserId);
                return ApiResponse.Success(new { deleted = count }, $"{count} data Radio Unit berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all Unit radios");
                return ApiResponse.InternalServerError("Gagal menghapus data Radio Unit: " + ex.Message);
            }
        }

        [HttpDelete("all/scrap")]
        [Authorize(Policy = "RadioDeletetAllScrap")]
        public async Task<IActionResult> DeleteAllScrap()
        {
            try
            {
                var count = await _radioService.DeleteByCategoryAsync("LegacyScrap", CurrentUserId);
                return ApiResponse.Success(new { deleted = count }, $"{count} data Radio Scrap berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all Scrap radios");
                return ApiResponse.InternalServerError("Gagal menghapus data Radio Scrap: " + ex.Message);
            }
        }

        // ============================================
        // SCRAP
        // ============================================

        [HttpPost("{id}/scrap")]
        [Authorize(Policy = "RadioScrapCreate")]
        public async Task<IActionResult> ScrapRadio(int id, [FromBody] ScrapRadioDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var data = await _radioService.ScrapRadioAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data, "Radio berhasil di-scrap");
            }
            catch (KeyNotFoundException)
            {
                return ApiResponse.NotFound("Radio tidak ditemukan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scrapping radio: {Id}", id);
                return ApiResponse.InternalServerError("Gagal men-scrap radio: " + ex.Message);
            }
        }

        [HttpPost("{id}/unscrap")]
        [Authorize(Policy = "RadioScrapUpdate")]
        public async Task<IActionResult> UnscrapRadio(int id)
        {
            try
            {
                var data = await _radioService.UnscrapRadioAsync(id, CurrentUserId);
                return ApiResponse.Success(data, "Radio berhasil dikembalikan dari scrap");
            }
            catch (KeyNotFoundException)
            {
                return ApiResponse.NotFound("Radio tidak ditemukan");
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest("radio", [ex.Message]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unscrapping radio: {Id}", id);
                return ApiResponse.InternalServerError("Gagal batal scrap radio: " + ex.Message);
            }
        }

        // ============================================
        // TRANSFER CATEGORY
        // ============================================

        [HttpPatch("{id}/transfer-category")]
        [Authorize(Policy = "RadioUpdate")]
        public async Task<IActionResult> TransferCategory(int id, [FromBody] TransferCategoryRequest request)
        {
            try
            {
                var data = await _radioService.TransferCategoryAsync(id, request.TargetCategory, CurrentUserId);
                var label = request.TargetCategory == "Internal" ? "KPC" : request.TargetCategory;
                return ApiResponse.Success(data, $"Radio berhasil dipindahkan ke {label}");
            }
            catch (KeyNotFoundException)
            {
                return ApiResponse.NotFound("Radio tidak ditemukan");
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest("radio", [ex.Message]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring radio category: {Id}", id);
                return ApiResponse.InternalServerError("Gagal memindahkan radio: " + ex.Message);
            }
        }

        // ============================================
        // IMPORT
        // ============================================

        [HttpPost("import/internal")]
        [Authorize(Policy = "RadioImport")]
        public async Task<IActionResult> ImportInternal(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ApiResponse.BadRequest("file", "File tidak boleh kosong");

            try
            {
                var result = await _radioService.ImportInternalAsync(file, CurrentUserId);
                var msg = result.SheetCount > 1
                    ? $"Berhasil import {result.TotalImported} data dari {result.SheetCount} sheet"
                    : $"{result.TotalImported} data radio internal berhasil diimport";
                return ApiResponse.Success(result, msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing internal radios");
                return ApiResponse.InternalServerError("Gagal import radio internal: " + ex.Message);
            }
        }

        [HttpPost("import/contractor")]
        [Authorize(Policy = "RadioImport")]
        public async Task<IActionResult> ImportContractor(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ApiResponse.BadRequest("file", "File tidak boleh kosong");

            try
            {
                var result = await _radioService.ImportContractorAsync(file, CurrentUserId);
                var msg = result.SheetCount > 1
                    ? $"Berhasil import {result.TotalImported} data dari {result.SheetCount} sheet"
                    : $"{result.TotalImported} data radio contractor berhasil diimport";
                return ApiResponse.Success(result, msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing contractor radios");
                return ApiResponse.InternalServerError("Gagal import radio contractor: " + ex.Message);
            }
        }

        [HttpPost("import/unit")]
        [Authorize(Policy = "RadioImport")]
        public async Task<IActionResult> ImportUnit(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ApiResponse.BadRequest("file", "File tidak boleh kosong");

            try
            {
                var result = await _radioService.ImportUnitAsync(file, CurrentUserId);
                var msg = result.SheetCount > 1
                    ? $"Berhasil import {result.TotalImported} data dari {result.SheetCount} sheet"
                    : $"{result.TotalImported} data radio unit berhasil diimport";
                return ApiResponse.Success(result, msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing unit radios");
                return ApiResponse.InternalServerError("Gagal import radio unit: " + ex.Message);
            }
        }

        [HttpPost("import/scrap")]
        [Authorize(Policy = "RadioScrapImport")]
        public async Task<IActionResult> ImportScrap(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ApiResponse.BadRequest("file", "File tidak boleh kosong");

            try
            {
                var result = await _radioService.ImportLegacyScrapAsync(file, CurrentUserId);
                var msg = result.SheetCount > 1
                    ? $"Berhasil import {result.TotalImported} data dari {result.SheetCount} sheet"
                    : $"{result.TotalImported} data radio scrap legacy berhasil diimport";
                return ApiResponse.Success(result, msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing legacy scrap radios");
                return ApiResponse.InternalServerError("Gagal import radio scrap legacy: " + ex.Message);
            }
        }
    }

    public class TransferCategoryRequest
    {
        public string TargetCategory { get; set; } = string.Empty;
    }
}
