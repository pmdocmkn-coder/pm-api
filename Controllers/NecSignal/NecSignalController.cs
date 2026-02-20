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
    [Route("api/nec-signal")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class NecSignalController : ControllerBase
    {
        private readonly INecSignalService _service;
        private readonly ILogger<NecSignalController> _logger;

        public NecSignalController(
            INecSignalService service,
            ILogger<NecSignalController> logger)
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
        [Authorize(Policy = "NecSignalView")]
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
        [Authorize(Policy = "NecSignalView")]
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
        [Authorize(Policy = "NecSignalView")]
        public async Task<IActionResult> GetYearlyPivot(int year, string? tower = null)
        {
            try
            {
                var result = await _service.GetYearlyPivotAsync(year, tower);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting yearly pivot for {Year}, Tower: {Tower}", year, tower);
                return ApiResponse.BadRequest("Parameter", ex.Message);
            }
        }

        // ============================================
        // IMPORT & EXPORT
        // ============================================
        [HttpPost("import-pivot-excel")]
        [Authorize(Policy = "NecSignalImport")]
        public async Task<IActionResult> ImportPivotExcel([FromForm] NecSignalImportRequestDto request)
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

                var result = await _service.ImportFromPivotExcelAsync(request.ExcelFile, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error importing pivot Excel file");
                return ApiResponse.InternalServerError("Import gagal: " + ex.Message);
            }
        }
        [HttpGet("export-yearly-excel")]
        [Authorize(Policy = "NecSignalExport")]
        public async Task<IActionResult> ExportYearlyExcel(int year, string? tower = null)
        {
            try
            {
                var bytes = await _service.ExportYearlyToExcelAsync(year, tower, CurrentUserId);
                var fileName = $"RSL_History_NEC_{year}{(tower == null ? "" : $"_{tower}")}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting yearly Excel for {Year}, Tower: {Tower}", year, tower);
                return ApiResponse.BadRequest("Export", ex.Message);
            }
        }

        // ============================================
        // CRUD TOWER
        // ============================================

        [HttpGet("towers")]
        [Authorize(Policy = "NecSignalView")]
        public async Task<IActionResult> GetTowers()
        {
            try
            {
                var data = await _service.GetTowersAsync();
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting towers");
                return ApiResponse.InternalServerError("Get Towers gagal: " + ex.Message);
            }
        }

        [HttpPost("towers")]
        [Authorize(Policy = "NecSignalCreate")]
        public async Task<IActionResult> CreateTower([FromBody] TowerCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _service.CreateTowerAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating tower");
                return ApiResponse.BadRequest("Create Tower", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tower");
                return ApiResponse.InternalServerError("Create Tower gagal: " + ex.Message);
            }
        }

        [HttpPut("towers")]
        [Authorize(Policy = "NecSignalUpdate")]
        public async Task<IActionResult> UpdateTower([FromBody] TowerUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _service.UpdateTowerAsync(dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Tower not found: {TowerId}", dto.Id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating tower: {TowerId}", dto.Id);
                return ApiResponse.BadRequest("Update Tower", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tower: {TowerId}", dto.Id);
                return ApiResponse.InternalServerError("Update Tower gagal: " + ex.Message);
            }
        }

        [HttpDelete("towers/{id}")]
        [Authorize(Policy = "NecSignalDelete")]
        public async Task<IActionResult> DeleteTower(int id)
        {
            try
            {
                await _service.DeleteTowerAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "Tower berhasil dihapus");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Tower not found: {TowerId}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete tower: {TowerId}", id);
                return ApiResponse.BadRequest("Delete Tower", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tower: {TowerId}", id);
                return ApiResponse.InternalServerError("Delete Tower gagal: " + ex.Message);
            }
        }

        // ============================================
        // CRUD LINK
        // ============================================

        [HttpGet("links")]
        [Authorize(Policy = "NecSignalView")]
        public async Task<IActionResult> GetLinks()
        {
            try
            {
                var data = await _service.GetLinksAsync();
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting links");
                return ApiResponse.InternalServerError("Get Links gagal: " + ex.Message);
            }
        }

        [HttpPost("links")]
        [Authorize(Policy = "NecSignalCreate")]
        public async Task<IActionResult> CreateLink([FromBody] NecLinkCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _service.CreateLinkAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating link");
                return ApiResponse.BadRequest("Create Link", ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Tower not found for link creation");
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating link");
                return ApiResponse.InternalServerError("Create Link gagal: " + ex.Message);
            }
        }

        [HttpPut("links")]
        [Authorize(Policy = "NecSignalUpdate")]
        public async Task<IActionResult> UpdateLink([FromBody] NecLinkUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _service.UpdateLinkAsync(dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Link or Tower not found: {LinkId}", dto.Id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating link: {LinkId}", dto.Id);
                return ApiResponse.BadRequest("Update Link", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating link: {LinkId}", dto.Id);
                return ApiResponse.InternalServerError("Update Link gagal: " + ex.Message);
            }
        }

        [HttpDelete("links/{id}")]
        [Authorize(Policy = "NecSignalDelete")]
        public async Task<IActionResult> DeleteLink(int id)
        {
            try
            {
                await _service.DeleteLinkAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "Link berhasil dihapus");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Link not found: {LinkId}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete link: {LinkId}", id);
                return ApiResponse.BadRequest("Delete Link", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting link: {LinkId}", id);
                return ApiResponse.InternalServerError("Delete Link gagal: " + ex.Message);
            }
        }

        // ============================================
        // CRUD HISTORY RSL
        // ============================================

        // Cek method GetHistories di controller
        [HttpGet("histories")]
        [Authorize(Policy = "NecSignalView")]
        public async Task<IActionResult> GetHistories([FromQuery] NecRslHistoryQueryDto query)
        {
            var validationResults = query.Validate(new ValidationContext(query)).ToList();
            if (validationResults.Any())
            {
                return ApiResponse.BadRequest("Invalid parameter",
                    string.Join("; ", validationResults.Select(v => v.ErrorMessage)));
            }

            try
            {
                var result = await _service.GetHistoriesAsync(query);

                // ✅ Kembalikan langsung, ResponseWrapperFilter akan skip wrapping
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting histories");
                return ApiResponse.BadRequest("Get Histories", ex.Message);
            }
        }

        [HttpGet("histories/{id}")]
        [Authorize(Policy = "NecSignalView")]
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
        [Authorize(Policy = "NecSignalCreate")]
        public async Task<IActionResult> CreateHistory([FromBody] NecRslHistoryCreateDto dto)
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
                _logger.LogWarning(ex, "Link not found for history creation");
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
        [Authorize(Policy = "NecSignalUpdate")]
        public async Task<IActionResult> UpdateHistory(int id, [FromBody] NecRslHistoryUpdateDto dto)
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
        [Authorize(Policy = "NecSignalDelete")]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            try
            {
                await _service.DeleteHistoryAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "History RSL berhasil dihapus");
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