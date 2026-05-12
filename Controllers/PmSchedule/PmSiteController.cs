using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pm.Helper;
using Pm.DTOs;
using Pm.Services.PmSchedule;

namespace Pm.Controllers.PmSchedule
{
    [ApiController]
    [Route("api/pm-sites")]
    [Authorize] // Assuming you want this to be protected
    public class PmSiteController : ControllerBase
    {
        private readonly IPmSiteService _pmSiteService;
        private readonly ILogger<PmSiteController> _logger;

        public PmSiteController(IPmSiteService pmSiteService, ILogger<PmSiteController> logger)
        {
            _pmSiteService = pmSiteService;
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

        [HttpGet]
        [Authorize(Policy = "PmScheduleView")]
        public async Task<IActionResult> GetAllSites()
        {
            try
            {
                var sites = await _pmSiteService.GetAllSitesAsync();
                return ApiResponse.Success(sites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PM sites");
                return ApiResponse.InternalServerError("Gagal mengambil data PM Site: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "PmScheduleCreate")]
        public async Task<IActionResult> CreateSite(PmSiteDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var createdSite = await _pmSiteService.CreateSiteAsync(dto, CurrentUserId);
                return ApiResponse.Created(createdSite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PM site");
                return ApiResponse.InternalServerError("Gagal membuat PM Site: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "PmScheduleUpdate")]
        public async Task<IActionResult> UpdateSite(int id, PmSiteDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var updatedSite = await _pmSiteService.UpdateSiteAsync(id, dto, CurrentUserId);
                if (updatedSite == null)
                    return ApiResponse.NotFound("PM Site tidak ditemukan");

                return ApiResponse.Success(updatedSite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PM site: {SiteId}", id);
                return ApiResponse.InternalServerError("Gagal mengupdate PM Site: " + ex.Message);
            }
        }

        [HttpPut("reorder")]
        [Authorize(Policy = "PmScheduleUpdate")]
        public async Task<IActionResult> ReorderSites(List<PmSiteOrderDto> orders)
        {
            if (orders == null || orders.Count == 0)
                return BadRequest(new { message = "Data urutan tidak valid" });

            try
            {
                var success = await _pmSiteService.UpdateSiteOrdersAsync(orders, CurrentUserId);
                return ApiResponse.Success(new { success }, "Urutan PM Site berhasil diupdate");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering PM sites");
                return ApiResponse.InternalServerError("Gagal mengupdate urutan PM Site: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "PmScheduleDelete")]
        public async Task<IActionResult> DeleteSite(int id)
        {
            try
            {
                var success = await _pmSiteService.DeleteSiteAsync(id, CurrentUserId);
                if (!success)
                    return ApiResponse.NotFound("PM Site tidak ditemukan");

                return ApiResponse.Success(new { }, "PM Site berhasil dihapus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PM site: {SiteId}", id);
                return ApiResponse.InternalServerError("Gagal menghapus PM Site: " + ex.Message);
            }
        }
    }
}
