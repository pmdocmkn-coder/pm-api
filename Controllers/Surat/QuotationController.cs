using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/quotations")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class QuotationController : ControllerBase
    {
        private readonly IQuotationService _service;
        private readonly ILogger<QuotationController> _logger;

        public QuotationController(IQuotationService service, ILogger<QuotationController> logger)
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

        [HttpGet]
        [Authorize(Policy = "QuotationView")]
        public async Task<IActionResult> GetAll([FromQuery] QuotationQueryDto query)
        {
            try
            {
                var result = await _service.GetQuotationsAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotations");
                return ApiResponse.BadRequest("Get Quotations", ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "QuotationView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetQuotationByIdAsync(id);
                if (result == null)
                {
                    return ApiResponse.NotFound("Quotation tidak ditemukan");
                }
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotation: {Id}", id);
                return ApiResponse.InternalServerError("Get Quotation gagal: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "QuotationCreate")]
        public async Task<IActionResult> Create([FromBody] QuotationCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                return ApiResponse.BadRequest("Quotation", errors);
            }

            try
            {
                var result = await _service.CreateQuotationAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating quotation");
                return ApiResponse.BadRequest("Create Quotation", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quotation");
                var innerMsg = ex.InnerException != null ? $" ({ex.InnerException.Message})" : "";
                return ApiResponse.InternalServerError($"Create Quotation gagal: {ex.Message}{innerMsg}");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "QuotationUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] QuotationUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                return ApiResponse.BadRequest("Quotation", errors);
            }

            try
            {
                var result = await _service.UpdateQuotationAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating quotation: {Id}", id);
                return ApiResponse.BadRequest("Update Quotation", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quotation: {Id}", id);
                return ApiResponse.InternalServerError("Update Quotation gagal: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "QuotationDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                await _service.DeleteQuotationAsync(id, CurrentUserId, userRole);
                return ApiResponse.Success(new { }, "Quotation berhasil dihapus");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete quotation: {Id}", id);
                return ApiResponse.BadRequest("Delete Quotation", ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Quotation not found: {Id}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quotation: {Id}", id);
                return ApiResponse.InternalServerError("Delete Quotation gagal: " + ex.Message);
            }
        }
    }
}
