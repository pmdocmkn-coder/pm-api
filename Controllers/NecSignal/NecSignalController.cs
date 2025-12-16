using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/nec-signal")]
    public class NecSignalController : ControllerBase
    {
        private readonly INecSignalService _service;

        public NecSignalController(INecSignalService service)
        {
            _service = service;
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

        // === HISTORY MONTHLY & YEARLY ===
        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthly(int year, int month)
        {
            try
            {
                var result = await _service.GetMonthlyAsync(year, month);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Parameter", ex.Message);
            }
        }

        [HttpGet("yearly")]
        public async Task<IActionResult> GetYearly(int year)
        {
            try
            {
                var result = await _service.GetYearlyAsync(year);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Parameter", ex.Message);
            }
        }

        // === IMPORT & EXPORT ===
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel([FromForm] NecSignalImportRequestDto request)
        {
            try
            {
                var result = await _service.ImportFromExcelAsync(request.ExcelFile, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.InternalServerError("Import gagal: " + ex.Message);
            }
        }

        [HttpGet("export-yearly-excel")]
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
                return ApiResponse.BadRequest("Export", ex.Message);
            }
        }

        // === CRUD TOWER ===
        [HttpGet("towers")]
        public async Task<IActionResult> GetTowers()
        {
            var data = await _service.GetTowersAsync();
            return ApiResponse.Success(data);
        }

        [HttpPost("towers")]
        public async Task<IActionResult> CreateTower([FromBody] TowerCreateDto dto)
        {
            try
            {
                var result = await _service.CreateTowerAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Create Tower", ex.Message);
            }
        }

        [HttpPut("towers")]
        public async Task<IActionResult> UpdateTower([FromBody] TowerUpdateDto dto)
        {
            try
            {
                var result = await _service.UpdateTowerAsync(dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Update Tower", ex.Message);
            }
        }

        [HttpDelete("towers/{id}")]
        public async Task<IActionResult> DeleteTower(int id)
        {
            try
            {
                await _service.DeleteTowerAsync(id, CurrentUserId);
                return ApiResponse.Success(new { message = "Tower berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Delete Tower", ex.Message);
            }
        }

        // === CRUD LINK ===
        [HttpGet("links")]
        public async Task<IActionResult> GetLinks()
        {
            var data = await _service.GetLinksAsync();
            return ApiResponse.Success(data);
        }

        [HttpPost("links")]
        public async Task<IActionResult> CreateLink([FromBody] NecLinkCreateDto dto)
        {
            try
            {
                var result = await _service.CreateLinkAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Create Link", ex.Message);
            }
        }

        [HttpPut("links")]
        public async Task<IActionResult> UpdateLink([FromBody] NecLinkUpdateDto dto)
        {
            try
            {
                var result = await _service.UpdateLinkAsync(dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Update Link", ex.Message);
            }
        }

        [HttpDelete("links/{id}")]
        public async Task<IActionResult> DeleteLink(int id)
        {
            try
            {
                await _service.DeleteLinkAsync(id, CurrentUserId);
                return ApiResponse.Success(new { message = "Link berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Delete Link", ex.Message);
            }
        }

        // === CRUD HISTORY RSL ===
        [HttpGet("histories")]
        public async Task<IActionResult> GetHistories([FromQuery] NecRslHistoryQueryDto query)
        {
            // Validasi SortBy
            var validationResults = query.Validate(new ValidationContext(query)).ToList();
            if (validationResults.Any())
            {
                return ApiResponse.BadRequest("Invalid parameter", 
                    string.Join("; ", validationResults.Select(v => v.ErrorMessage)));
            }

            try
            {
                var result = await _service.GetHistoriesAsync(query);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Get Histories", ex.Message);
            }
        }

        [HttpGet("histories/{id}")]
        public async Task<IActionResult> GetHistory(int id)
        {
            try
            {
                var result = await _service.GetHistoryByIdAsync(id);
                return result == null
                    ? ApiResponse.NotFound("History tidak ditemukan")
                    : ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Get History", ex.Message);
            }
        }

        [HttpPost("histories")]
        public async Task<IActionResult> CreateHistory([FromBody] NecRslHistoryCreateDto dto)
        {
            try
            {
                var result = await _service.CreateHistoryAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Create History", ex.Message);
            }
        }

        [HttpPut("histories/{id}")]
        public async Task<IActionResult> UpdateHistory(int id, [FromBody] NecRslHistoryUpdateDto dto)
        {
            try
            {
                var result = await _service.UpdateHistoryAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Update History", ex.Message);
            }
        }

        [HttpDelete("histories/{id}")]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            try
            {
                await _service.DeleteHistoryAsync(id, CurrentUserId);
                return ApiResponse.Success(new { message = "History RSL berhasil dihapus" });
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest("Delete History", ex.Message);
            }
        }
    }
}