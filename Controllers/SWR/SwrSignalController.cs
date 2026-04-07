using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/swr-signal")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class SwrSignalController : ControllerBase
    {
        private readonly ISwrSignalService _service;
        private readonly ILogger<SwrSignalController> _logger;

        public SwrSignalController(
            ISwrSignalService service,
            ILogger<SwrSignalController> logger)
        {
            _service = service;
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
        // MONTHLY & YEARLY SUMMARY
        // ============================================

        [HttpGet("monthly")]
        [Authorize(Policy = "SwrSignalView")]
        public async Task<IActionResult> GetMonthly(int year, int month)
        {
            try
            {
                var result = await _service.GetMonthlyAsync(year, month);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly data for {Year}-{Month}", year, month);
                return ApiResponse.BadRequest("Parameter", ex.Message);
            }
        }

        [HttpGet("yearly")]
        [Authorize(Policy = "SwrSignalView")]
        public async Task<IActionResult> GetYearly(int year)
        {
            try
            {
                var result = await _service.GetYearlyAsync(year);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting yearly data for {Year}", year);
                return ApiResponse.BadRequest("Parameter", ex.Message);
            }
        }

        [HttpGet("yearly-pivot")]
        [Authorize(Policy = "SwrSignalView")]
        public async Task<IActionResult> GetYearlyPivot(int year, string? site = null)
        {
            try
            {
                var result = await _service.GetYearlyPivotAsync(year, site);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting yearly pivot for {Year}, Site: {Site}", year, site);
                return ApiResponse.BadRequest("Parameter", ex.Message);
            }
        }

        // ============================================
        // IMPORT & EXPORT
        // ============================================

        [HttpPost("import-pivot-excel")]
        [Authorize(Policy = "SwrSignalImport")]
        public async Task<IActionResult> ImportPivotExcel([FromForm] SwrImportRequestDto request)
        {
            try
            {
                _logger.LogInformation("📤 ImportPivotExcel endpoint hit!");
                _logger.LogInformation("📤 File: {FileName}, Size: {Size}",
                    request?.ExcelFile?.FileName, request?.ExcelFile?.Length);

                if (request?.ExcelFile == null || request.ExcelFile.Length == 0)
                {
                    return ApiResponse.BadRequest("Import", "File tidak boleh kosong");
                }

                var result = await _service.ImportFromExcelAsync(request.ExcelFile, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error importing pivot Excel file");
                return ApiResponse.InternalServerError("Import gagal: " + ex.Message);
            }
        }

        [HttpGet("export-yearly-excel")]
        [Authorize(Policy = "SwrSignalExport")]
        public async Task<IActionResult> ExportYearlyExcel([FromQuery] int year, [FromQuery] List<string>? sites = null, [FromQuery] string? type = null, [FromQuery] string? search = null)
        {
            try
            {
                var bytes = await _service.ExportYearlyToExcelAsync(year, sites, type, search, CurrentUserId);
                var fileName = $"SWR_History_{year}{(sites != null && sites.Any() ? $"_Filtered" : "")}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting yearly Excel for {Year}, Sites count: {SiteCount}", year, sites?.Count ?? 0);
                return ApiResponse.BadRequest("Export", ex.Message);
            }
        }

        // ============================================
        // CRUD SITE
        // ============================================

        [HttpGet("sites")]
        [Authorize(Policy = "SwrSignalView")]
        public async Task<IActionResult> GetSites()
        {
            try
            {
                var data = await _service.GetSitesAsync();
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sites");
                return ApiResponse.InternalServerError("Get Sites gagal: " + ex.Message);
            }
        }

        [HttpPost("sites")]
        [Authorize(Policy = "SwrSignalCreate")]
        public async Task<IActionResult> CreateSite([FromBody] SwrSiteCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _service.CreateSiteAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating site");
                return ApiResponse.BadRequest("Create Site", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating site");
                return ApiResponse.InternalServerError("Create Site gagal: " + ex.Message);
            }
        }

        [HttpPut("sites")]
        [Authorize(Policy = "SwrSignalUpdate")]
        public async Task<IActionResult> UpdateSite([FromBody] SwrSiteUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _service.UpdateSiteAsync(dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Site not found: {SiteId}", dto.Id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating site: {SiteId}", dto.Id);
                return ApiResponse.BadRequest("Update Site", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating site: {SiteId}", dto.Id);
                return ApiResponse.InternalServerError("Update Site gagal: " + ex.Message);
            }
        }

        [HttpDelete("sites/{id}")]
        [Authorize(Policy = "SwrSignalDelete")]
        public async Task<IActionResult> DeleteSite(int id)
        {
            try
            {
                await _service.DeleteSiteAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "Site berhasil dihapus");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Site not found: {SiteId}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete site: {SiteId}", id);
                return ApiResponse.BadRequest("Delete Site", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting site: {SiteId}", id);
                return ApiResponse.InternalServerError("Delete Site gagal: " + ex.Message);
            }
        }

        // ============================================
        // CRUD CHANNEL
        // ============================================

        [HttpGet("channels")]
        [Authorize(Policy = "SwrSignalView")]
        public async Task<IActionResult> GetChannels()
        {
            try
            {
                var data = await _service.GetChannelsAsync();
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting channels");
                return ApiResponse.InternalServerError("Get Channels gagal: " + ex.Message);
            }
        }

        [HttpPost("channels")]
        [Authorize(Policy = "SwrSignalCreate")]
        public async Task<IActionResult> CreateChannel([FromBody] SwrChannelCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _service.CreateChannelAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating channel");
                return ApiResponse.BadRequest("Create Channel", ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Site not found for channel creation");
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating channel");
                return ApiResponse.InternalServerError("Create Channel gagal: " + ex.Message);
            }
        }

        [HttpPut("channels")]
        [Authorize(Policy = "SwrSignalUpdate")]
        public async Task<IActionResult> UpdateChannel([FromBody] SwrChannelUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _service.UpdateChannelAsync(dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Channel or Site not found: {ChannelId}", dto.Id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating channel: {ChannelId}", dto.Id);
                return ApiResponse.BadRequest("Update Channel", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating channel: {ChannelId}", dto.Id);
                return ApiResponse.InternalServerError("Update Channel gagal: " + ex.Message);
            }
        }

        [HttpDelete("channels/{id}")]
        [Authorize(Policy = "SwrSignalDelete")]
        public async Task<IActionResult> DeleteChannel(int id)
        {
            try
            {
                await _service.DeleteChannelAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "Channel berhasil dihapus");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Channel not found: {ChannelId}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete channel: {ChannelId}", id);
                return ApiResponse.BadRequest("Delete Channel", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting channel: {ChannelId}", id);
                return ApiResponse.InternalServerError("Delete Channel gagal: " + ex.Message);
            }
        }

        // ============================================
        // CRUD HISTORY SWR
        // ============================================

        [HttpGet("histories")]
        [Authorize(Policy = "SwrSignalView")]
        public async Task<IActionResult> GetHistories([FromQuery] SwrHistoryQueryDto query)
        {
            var validationResults = query.Validate(new ValidationContext(query)).ToList();
            if (validationResults.Count > 0)
            {
                return ApiResponse.BadRequest("Invalid parameter",
                    string.Join("; ", validationResults.Select(v => v.ErrorMessage)));
            }

            try
            {
                var result = await _service.GetHistoriesAsync(query);

                // Kembalikan langsung, ResponseWrapperFilter akan skip wrapping
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting histories");
                return ApiResponse.BadRequest("Get Histories", ex.Message);
            }
        }

        [HttpGet("histories/{id}")]
        [Authorize(Policy = "SwrSignalView")]
        public async Task<IActionResult> GetHistory(int id)
        {
            try
            {
                var result = await _service.GetHistoryByIdAsync(id);
                if (result == null)
                {
                    return ApiResponse.NotFound("History tidak ditemukan");
                }
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history: {HistoryId}", id);
                return ApiResponse.BadRequest("Get History", ex.Message);
            }
        }

        [HttpPost("histories")]
        [Authorize(Policy = "SwrSignalCreate")]
        public async Task<IActionResult> CreateHistory([FromBody] SwrHistoryCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _service.CreateHistoryAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Channel not found for history creation");
                return ApiResponse.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Duplicate history entry");
                return ApiResponse.BadRequest("Create History", ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating history");
                return ApiResponse.BadRequest("Create History", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating history");
                return ApiResponse.InternalServerError("Create History gagal: " + ex.Message);
            }
        }

        [HttpPut("histories/{id}")]
        [Authorize(Policy = "SwrSignalUpdate")]
        public async Task<IActionResult> UpdateHistory(int id, [FromBody] SwrHistoryUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _service.UpdateHistoryAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "History not found: {HistoryId}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating history: {HistoryId}", id);
                return ApiResponse.BadRequest("Update History", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating history: {HistoryId}", id);
                return ApiResponse.InternalServerError("Update History gagal: " + ex.Message);
            }
        }

        [HttpDelete("histories/{id}")]
        [Authorize(Policy = "SwrSignalDelete")]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            try
            {
                await _service.DeleteHistoryAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "History SWR berhasil dihapus");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "History not found: {HistoryId}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting history: {HistoryId}", id);
                return ApiResponse.InternalServerError("Delete History gagal: " + ex.Message);
            }
        }
    }
}