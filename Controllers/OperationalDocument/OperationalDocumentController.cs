using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/operational-documents")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class OperationalDocumentController(
        IOperationalDocumentService _service,
        ILogger<OperationalDocumentController> _logger) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = "OperationalDocumentView")]
        public async Task<IActionResult> GetAll([FromQuery] OperationalDocumentQueryDto query)
        {
            try
            {
                var result = await _service.GetAllAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operational documents");
                return ApiResponse.InternalServerError("Get Operational Documents gagal: " + ex.Message);
            }
        }

        [HttpGet("summary")]
        [Authorize(Policy = "OperationalDocumentView")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var result = await _service.GetSummaryAsync();
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operational document summary");
                return ApiResponse.InternalServerError("Get Summary gagal: " + ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "OperationalDocumentView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operational document {Id}", id);
                return ApiResponse.InternalServerError("Get Document gagal: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "OperationalDocumentCreate")]
        public async Task<IActionResult> Create([FromBody] OperationalDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var result = await _service.CreateAsync(dto);
                return ApiResponse.Created(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating document");
                return ApiResponse.BadRequest("Create Document", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating operational document");
                return ApiResponse.InternalServerError("Create Document gagal: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "OperationalDocumentUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] OperationalDocumentUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var result = await _service.UpdateAsync(id, dto);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return ApiResponse.BadRequest("Update Document", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating operational document {Id}", id);
                return ApiResponse.InternalServerError("Update Document gagal: " + ex.Message);
            }
        }

        [HttpPatch("{id}/follow-up-status")]
        [Authorize(Policy = "OperationalDocumentUpdate")]
        public async Task<IActionResult> UpdateFollowUpStatus(int id, [FromBody] UpdateFollowUpStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var result = await _service.UpdateFollowUpStatusAsync(id, dto.Status);
                return ApiResponse.Success(result, "Status tindak lanjut berhasil diperbarui");
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return ApiResponse.BadRequest("Update Follow Up Status", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating follow-up status for document {Id}", id);
                return ApiResponse.InternalServerError("Update Follow Up Status gagal: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "OperationalDocumentDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return ApiResponse.Success(new { }, "Dokumen berhasil dihapus");
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting operational document {Id}", id);
                return ApiResponse.InternalServerError("Delete Document gagal: " + ex.Message);
            }
        }
    }

    public class UpdateFollowUpStatusDto
    {
        public required string Status { get; set; }
    }
}
