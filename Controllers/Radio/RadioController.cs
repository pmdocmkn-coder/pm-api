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
    public class RadioController : ControllerBase
    {
        private readonly IRadioService _radioService;
        private readonly ILogger<RadioController> _logger;

        public RadioController(IRadioService radioService, ILogger<RadioController> logger)
        {
            _radioService = radioService;
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

        // ============================================
        // GET
        // ============================================

        [HttpGet]
        [Authorize(Policy = "RadioView")]
        public async Task<IActionResult> GetAll([FromQuery] string? category = null, [FromQuery] bool isScrap = false)
        {
            try
            {
                var data = await _radioService.GetAllAsync(category, isScrap);
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting radios, category: {Category}, isScrap: {IsScrap}", category, isScrap);
                return ApiResponse.InternalServerError("Gagal mengambil data radio: " + ex.Message);
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
                var count = await _radioService.ImportInternalAsync(file, CurrentUserId);
                return ApiResponse.Success(new { imported = count }, $"{count} data radio internal berhasil diimport");
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
                var count = await _radioService.ImportContractorAsync(file, CurrentUserId);
                return ApiResponse.Success(new { imported = count }, $"{count} data radio contractor berhasil diimport");
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
                var count = await _radioService.ImportUnitAsync(file, CurrentUserId);
                return ApiResponse.Success(new { imported = count }, $"{count} data radio unit berhasil diimport");
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
                var count = await _radioService.ImportLegacyScrapAsync(file, CurrentUserId);
                return ApiResponse.Success(new { imported = count }, $"{count} data radio scrap legacy berhasil diimport");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing legacy scrap radios");
                return ApiResponse.InternalServerError("Gagal import radio scrap legacy: " + ex.Message);
            }
        }
    }
}
