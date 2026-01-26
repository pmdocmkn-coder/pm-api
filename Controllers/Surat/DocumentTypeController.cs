using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/document-types")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class DocumentTypeController(
        IDocumentTypeService service,
        ILogger<DocumentTypeController> logger) : ControllerBase
    {

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
        [Authorize(Policy = "LetterNumberView")]
        public async Task<IActionResult> GetAll([FromQuery] DocumentTypeQueryDto query)
        {
            try
            {
                var result = await service.GetAllAsync(query);
                return Ok(result); // Return PagedResultDto directly
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting document types");
                return ApiResponse.BadRequest("Get Document Types", ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "LetterNumberView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await service.GetByIdAsync(id);
                if (result == null)
                {
                    return ApiResponse.NotFound("Document type tidak ditemukan");
                }
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting document type: {Id}", id);
                return ApiResponse.InternalServerError("Get Document Type gagal: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "LetterNumberCreate")]
        public async Task<IActionResult> Create([FromBody] DocumentTypeCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await service.CreateAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Validation error creating document type");
                return ApiResponse.BadRequest("Create Document Type", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating document type");
                return ApiResponse.InternalServerError("Create Document Type gagal: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "LetterNumberEdit")]
        public async Task<IActionResult> Update(int id, [FromBody] DocumentTypeUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await service.UpdateAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Document type not found: {Id}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Validation error updating document type: {Id}", id);
                return ApiResponse.BadRequest("Update Document Type", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating document type: {Id}", id);
                return ApiResponse.InternalServerError("Update Document Type gagal: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "LetterNumberDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await service.DeleteAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "Document type berhasil dihapus");
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Document type not found: {Id}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Cannot delete document type: {Id}", id);
                return ApiResponse.BadRequest("Delete Document Type", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting document type: {Id}", id);
                return ApiResponse.InternalServerError("Delete Document Type gagal: " + ex.Message);
            }
        }
    }
}
