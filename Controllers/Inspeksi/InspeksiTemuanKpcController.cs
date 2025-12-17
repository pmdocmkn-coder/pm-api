// Controllers/InspeksiTemuanKpcController.cs - UPDATE POLICIES
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pm.DTOs;
using Pm.Services;
using System.Security.Claims;
using Pm.DTOs.Common;
using Pm.Helper;

namespace Pm.Controllers
{
    [Route("api/inspeksi-temuan-kpc")]
    [ApiController]
    public class InspeksiTemuanKpcController : ControllerBase
    {
        private readonly IInspeksiTemuanKpcService _service;
        private readonly IEmailService _emailService;
        private readonly ILogger<InspeksiTemuanKpcController> _logger;
        private readonly int _userId;

        public InspeksiTemuanKpcController(
            IInspeksiTemuanKpcService service,
            IEmailService emailService,
            IHttpContextAccessor http,
            ILogger<InspeksiTemuanKpcController> logger)
        {
            _service = service;
            _emailService = emailService;
            _logger = logger;
            _userId = int.Parse(http.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        // GET: api/inspeksi-temuan-kpc
        [Authorize(Policy = "InspeksiTemuanKpcView")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] InspeksiTemuanKpcQueryDto query)
        {
            // ✅ SELALU set IncludeDeleted = false untuk active data
            query.IncludeDeleted = false;
            
            _logger.LogInformation("📊 GetAll request - Page: {Page}, PageSize: {PageSize}, IncludeDeleted: {IncludeDeleted}",
                query.Page, query.PageSize, query.IncludeDeleted);
            
            var result = await _service.GetAllAsync(query);
            
            // ✅ PERBAIKAN: Akses melalui Meta.Pagination
            _logger.LogInformation("📊 GetAll response - TotalCount: {TotalCount}, DataLength: {DataLength}, Page: {Page}, TotalPages: {TotalPages}",
                result.Meta.Pagination.TotalCount, result.Data.Count, result.Meta.Pagination.Page, result.Meta.Pagination.TotalPages);
            
            // ✅ RETURN langsung result, middleware akan handle wrapping
            return ApiResponse.Success(result);
        }

        // GET: api/inspeksi-temuan-kpc/5
        [Authorize(Policy = "InspeksiTemuanKpcView")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound("Data tidak ditemukan");
            return ApiResponse.Success(result);
        }

        // GET: api/inspeksi-temuan-kpc/history
        [Authorize(Policy = "InspeksiTemuanKpcView")]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] InspeksiTemuanKpcQueryDto query)
        {
            query.IsArchived = true; // hanya data yang di-delete
            var result = await _service.GetAllAsync(query);
            return ApiResponse.Success(result, "Data history berhasil dimuat");
        }

        // POST: api/inspeksi-temuan-kpc
        [Authorize(Policy = "InspeksiTemuanKpcCreate")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CreateInspeksiTemuanKpcDto dto)
        {
            _logger.LogInformation("🔄 Create request for room: {Ruang}", dto.Ruang);
            _logger.LogInformation("📁 Files received: {Count}", dto.FotoTemuanFiles?.Count ?? 0);

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var id = await _service.CreateAsync(dto, _userId);

            // KIRIM EMAIL KE PIC (opsional)
            if (!string.IsNullOrEmpty(dto.PicPelaksana))
            {
                try
                {
                    await _emailService.SendTemuanCreatedEmailAsync(
                        temuanId: id,
                        ruang: dto.Ruang,
                        temuan: dto.Temuan,
                        picEmail: dto.PicPelaksana
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ Email send failed: {Message}", ex.Message);
                }
            }

            return ApiResponse.Success(
                data: new { id },
                message: "Temuan berhasil dibuat"
            );
        }

        // PATCH: Update
        [Authorize(Policy = "InspeksiTemuanKpcUpdate")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateInspeksiTemuanKpcDto dto)
        {
            try
            {
                _logger.LogInformation("🔄 UPDATE REQUEST - ID: {Id}, User: {UserId}", id, _userId);
                
                // ✅ LOG FORM DATA DETAILS
                _logger.LogInformation("📦 FormData values:");
                _logger.LogInformation("  - Ruang: '{Ruang}'", dto.Ruang ?? "NULL");
                _logger.LogInformation("  - Status: '{Status}'", dto.Status ?? "NULL");
                _logger.LogInformation("  - NoFollowUp: '{NoFollowUp}'", dto.NoFollowUp ?? "NULL");
                _logger.LogInformation("  - PicPelaksana: '{PicPelaksana}'", dto.PicPelaksana ?? "NULL");
                _logger.LogInformation("  - PerbaikanDilakukan: '{Perbaikan}'", dto.PerbaikanDilakukan ?? "NULL");
                _logger.LogInformation("  - Keterangan: '{Keterangan}'", dto.Keterangan ?? "NULL");
                _logger.LogInformation("  - TanggalPerbaikan: {Date}", dto.TanggalPerbaikan ?? "NULL");
                _logger.LogInformation("  - TanggalSelesaiPerbaikan: {Date}", dto.TanggalSelesaiPerbaikan ?? "NULL");
                
                // ✅ LOG CLEAR FLAGS
                _logger.LogInformation("  - Clear flags:");
                _logger.LogInformation("    • ClearNoFollowUp: {Value}", dto.ClearNoFollowUp ?? "false");
                _logger.LogInformation("    • ClearPicPelaksana: {Value}", dto.ClearPicPelaksana ?? "false");
                _logger.LogInformation("    • ClearPerbaikanDilakukan: {Value}", dto.ClearPerbaikanDilakukan ?? "false");
                _logger.LogInformation("    • ClearKeterangan: {Value}", dto.ClearKeterangan ?? "false");
                _logger.LogInformation("    • ClearTanggalPerbaikan: {Value}", dto.ClearTanggalPerbaikan ?? "false");
                _logger.LogInformation("    • ClearTanggalSelesaiPerbaikan: {Value}", dto.ClearTanggalSelesaiPerbaikan ?? "false");
                
                // ✅ LOG FILES
                _logger.LogInformation("  - Files: Temuan={TemuanCount}, Hasil={HasilCount}", 
                    dto.FotoTemuanFiles?.Count ?? 0, dto.FotoHasilFiles?.Count ?? 0);

                var updatedDto = await _service.UpdateAsync(id, dto, _userId);
                
                if (updatedDto == null)
                    return ApiResponse.NotFound($"Data dengan ID {id} tidak ditemukan");

                return ApiResponse.Success(
                    data: updatedDto,
                    message: "Temuan berhasil diperbarui"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ UNHANDLED ERROR in Update - ID: {Id}", id);
                return ApiResponse.InternalServerError($"Update gagal: {ex.Message}");
            }
        }

        // DELETE: api/inspeksi-temuan-kpc/5
        [Authorize(Policy = "InspeksiTemuanKpcDelete")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id, _userId);
            if (!success) return NotFound("Data tidak ditemukan");
            return ApiResponse.Success(
                data: new { deleted = true },
                message: "Temuan berhasil dipindahkan ke history"
            );
        }

        [Authorize(Policy = "InspeksiTemuanKpcDelete")] // Same policy as delete
        [HttpDelete("{id}/permanent")]
        public async Task<IActionResult> DeletePermanent(int id)
        {
            try
            {
                var success = await _service.DeletePermanentAsync(id, _userId);
                if (!success) return NotFound("Data tidak ditemukan atau belum dipindahkan ke history");
                return ApiResponse.Success(
                    data: new { deleted = true },
                    message: "Temuan berhasil dihapus secara permanen"
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permanent for ID: {Id}", id);
                return StatusCode(500, new { message = "Terjadi kesalahan saat menghapus data permanen" });
            }
        }

        [Authorize(Policy = "InspeksiTemuanKpcUpdate")]
        [HttpDelete("{id}/foto-temuan/{index}")]
        public async Task<IActionResult> DeleteFotoTemuan(int id, int index)
        {
            try
            {
                _logger.LogInformation("🗑️ Delete foto temuan request - ID: {Id}, Index: {Index}", id, index);
                
                // Get the entity first to check ownership
                var entity = await _service.GetByIdAsync(id);
                if (entity == null) return NotFound("Data tidak ditemukan");

                // Since we don't have direct access to service context, we need to modify the service
                // For now, we'll create a simple implementation
                var success = await _service.DeleteFotoAsync(id, index, "temuan", _userId);
                if (!success) return BadRequest("Gagal menghapus foto temuan");

                return ApiResponse.Success(
                    data: new { deleted = true },
                    message: "Foto temuan berhasil dihapus"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting foto temuan for ID: {Id}, Index: {Index}", id, index);
                return StatusCode(500, new { message = "Terjadi kesalahan saat menghapus foto temuan" });
            }
        }

        // DELETE: api/inspeksi-temuan-kpc/5/foto-hasil/0
        [Authorize(Policy = "InspeksiTemuanKpcUpdate")]
        [HttpDelete("{id}/foto-hasil/{index}")]
        public async Task<IActionResult> DeleteFotoHasil(int id, int index)
        {
            try
            {
                _logger.LogInformation("🗑️ Delete foto hasil request - ID: {Id}, Index: {Index}", id, index);
                
                // Get the entity first to check ownership
                var entity = await _service.GetByIdAsync(id);
                if (entity == null) return NotFound("Data tidak ditemukan");

                var success = await _service.DeleteFotoAsync(id, index, "hasil", _userId);
                if (!success) return BadRequest("Gagal menghapus foto hasil");

                return ApiResponse.Success(
                    data: new { deleted = true },
                    message: "Foto hasil berhasil dihapus"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting foto hasil for ID: {Id}, Index: {Index}", id, index);
                return StatusCode(500, new { message = "Terjadi kesalahan saat menghapus foto hasil" });
            }
        }
        

        // PATCH: api/inspeksi-temuan-kpc/5/restore
        [Authorize(Policy = "InspeksiTemuanKpcRestore")]
        [HttpPatch("{id}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            var success = await _service.RestoreAsync(id, _userId);
            if (!success) return NotFound("Data tidak ditemukan atau belum dihapus");
            return ApiResponse.Success(
                data: new { restored = true },
                message: "Temuan berhasil dipulihkan dari history"
            );
        }

        // GET: api/inspeksi-temuan-kpc/export
        [Authorize(Policy = "InspeksiTemuanKpcView")]
        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] bool history = false,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? ruang = null,
            [FromQuery] string? status = null)
        {
            var bytes = await _service.ExportToExcelAsync(history, startDate, endDate, ruang, status);
            var fileName = history ? $"History_KPC_{DateTime.Now:yyyyMMdd}.xlsx" : $"Laporan_KPC_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}