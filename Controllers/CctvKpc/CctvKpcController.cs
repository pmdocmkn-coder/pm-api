using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pm.DTOs.CctvKpc;
using Pm.Helper;
using Pm.Services.CctvKpc;

namespace Pm.Controllers.CctvKpc
{
    [ApiController]
    [Route("api/cctv-kpc")]
    [Authorize]
    public class CctvKpcController : ControllerBase
    {
        private readonly ICctvKpcService _service;
        private readonly ILogger<CctvKpcController> _logger;

        public CctvKpcController(ICctvKpcService service, ILogger<CctvKpcController> logger)
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
        // GET ALL (paged)
        // ============================================
        [HttpGet]
        [Authorize(Policy = "CctvKpcView")]
        public async Task<IActionResult> GetAll([FromQuery] CctvKpcQueryDto query)
        {
            try
            {
                var data = await _service.GetAllAsync(query);
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CCTV KPC list");
                return ApiResponse.InternalServerError("Gagal mengambil data CCTV: " + ex.Message);
            }
        }

        // GET ALL (unpaged — untuk dropdown/export)
        [HttpGet("all")]
        [Authorize(Policy = "CctvKpcView")]
        public async Task<IActionResult> GetAllUnpaged()
        {
            try
            {
                var data = await _service.GetAllUnpagedAsync();
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all CCTV KPC");
                return ApiResponse.InternalServerError("Gagal mengambil data CCTV: " + ex.Message);
            }
        }

        // ============================================
        // GET BY ID
        // ============================================
        [HttpGet("{id}")]
        [Authorize(Policy = "CctvKpcView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _service.GetByIdAsync(id);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException)
            {
                return ApiResponse.NotFound("CCTV tidak ditemukan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CCTV by id: {Id}", id);
                return ApiResponse.InternalServerError("Gagal mengambil data CCTV: " + ex.Message);
            }
        }

        // ============================================
        // CREATE
        // ============================================
        [HttpPost]
        [Authorize(Policy = "CctvKpcCreate")]
        public async Task<IActionResult> Create([FromBody] CreateCctvKpcDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var data = await _service.CreateAsync(dto, CurrentUserId);
                return ApiResponse.Created(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating CCTV");
                return ApiResponse.InternalServerError("Gagal menambahkan CCTV: " + ex.Message);
            }
        }

        // ============================================
        // UPDATE
        // ============================================
        [HttpPut("{id}")]
        [Authorize(Policy = "CctvKpcUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCctvKpcDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var data = await _service.UpdateAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data, "Data CCTV berhasil diperbarui");
            }
            catch (KeyNotFoundException)
            {
                return ApiResponse.NotFound("CCTV tidak ditemukan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating CCTV: {Id}", id);
                return ApiResponse.InternalServerError("Gagal memperbarui CCTV: " + ex.Message);
            }
        }

        // ============================================
        // DELETE
        // ============================================
        [HttpDelete("{id}")]
        [Authorize(Policy = "CctvKpcDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "Data CCTV berhasil dihapus");
            }
            catch (KeyNotFoundException)
            {
                return ApiResponse.NotFound("CCTV tidak ditemukan");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting CCTV: {Id}", id);
                return ApiResponse.InternalServerError("Gagal menghapus CCTV: " + ex.Message);
            }
        }

        // ============================================
        // DELETE ALL
        // ============================================
        [HttpDelete("all")]
        [Authorize(Policy = "CctvKpcDeleteAll")]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                await _service.DeleteAllAsync(CurrentUserId);
                return ApiResponse.Success(new { }, "Seluruh data CCTV berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all CCTV");
                return ApiResponse.InternalServerError("Gagal menghapus seluruh data CCTV: " + ex.Message);
            }
        }
    }
}
