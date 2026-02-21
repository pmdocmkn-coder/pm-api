using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.InternalLink;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/internal-link")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class InternalLinkController : ControllerBase
    {
        private readonly IInternalLinkService _service;
        private readonly ILogger<InternalLinkController> _logger;

        public InternalLinkController(
            IInternalLinkService service,
            ILogger<InternalLinkController> logger)
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
        // CRUD LINK
        // ============================================

        [HttpGet("links")]
        [Authorize(Policy = "InternalLinkView")]
        public async Task<IActionResult> GetLinks()
        {
            try
            {
                var data = await _service.GetLinksAsync();
                return ApiResponse.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting internal links");
                return ApiResponse.InternalServerError("Get Links gagal: " + ex.Message);
            }
        }

        [HttpGet("links/{id}")]
        [Authorize(Policy = "InternalLinkView")]
        public async Task<IActionResult> GetLink(int id)
        {
            try
            {
                var result = await _service.GetLinkByIdAsync(id);
                if (result == null)
                    return ApiResponse.NotFound("Link tidak ditemukan");
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting link: {LinkId}", id);
                return ApiResponse.InternalServerError("Get Link gagal: " + ex.Message);
            }
        }

        [HttpPost("links")]
        [Authorize(Policy = "InternalLinkCreate")]
        public async Task<IActionResult> CreateLink([FromBody] InternalLinkCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating link");
                return ApiResponse.InternalServerError("Create Link gagal: " + ex.Message);
            }
        }

        [HttpPut("links/{id}")]
        [Authorize(Policy = "InternalLinkUpdate")]
        public async Task<IActionResult> UpdateLink(int id, [FromBody] InternalLinkUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            dto.Id = id;

            try
            {
                var result = await _service.UpdateLinkAsync(dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Link not found: {LinkId}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating link: {LinkId}", id);
                return ApiResponse.BadRequest("Update Link", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating link: {LinkId}", id);
                return ApiResponse.InternalServerError("Update Link gagal: " + ex.Message);
            }
        }

        [HttpDelete("links/{id}")]
        [Authorize(Policy = "InternalLinkDelete")]
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
        // CRUD HISTORY
        // ============================================

        [HttpGet("histories")]
        [Authorize(Policy = "InternalLinkView")]
        public async Task<IActionResult> GetHistories([FromQuery] InternalLinkHistoryQueryDto query)
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
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting histories");
                return ApiResponse.BadRequest("Get Histories", ex.Message);
            }
        }

        [HttpGet("histories/{id}")]
        [Authorize(Policy = "InternalLinkView")]
        public async Task<IActionResult> GetHistory(int id)
        {
            try
            {
                var result = await _service.GetHistoryByIdAsync(id);
                if (result == null)
                    return ApiResponse.NotFound("History tidak ditemukan");
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history: {HistoryId}", id);
                return ApiResponse.InternalServerError("Get History gagal: " + ex.Message);
            }
        }

        [HttpPost("histories")]
        [Authorize(Policy = "InternalLinkCreate")]
        public async Task<IActionResult> CreateHistory([FromBody] InternalLinkHistoryCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

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
        [Authorize(Policy = "InternalLinkUpdate")]
        public async Task<IActionResult> UpdateHistory(int id, [FromBody] InternalLinkHistoryUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

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
        [Authorize(Policy = "InternalLinkDelete")]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            try
            {
                await _service.DeleteHistoryAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "History berhasil dihapus");
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
