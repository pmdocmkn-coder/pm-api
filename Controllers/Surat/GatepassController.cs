using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/gatepasses")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class GatepassController : ControllerBase
    {
        private readonly IGatepassService _service;
        private readonly ILogger<GatepassController> _logger;

        public GatepassController(IGatepassService service, ILogger<GatepassController> logger)
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
        [Authorize(Policy = "GatepassView")]
        public async Task<IActionResult> GetAll([FromQuery] GatepassQueryDto query)
        {
            try
            {
                var result = await _service.GetGatepassesAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gatepasses");
                return ApiResponse.BadRequest("Get Gatepasses", ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "GatepassView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetGatepassByIdAsync(id);
                if (result == null)
                {
                    return ApiResponse.NotFound("Gatepass tidak ditemukan");
                }
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting gatepass: {Id}", id);
                return ApiResponse.InternalServerError("Get Gatepass gagal: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "GatepassCreate")]
        public async Task<IActionResult> Create([FromBody] GatepassCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                return ApiResponse.BadRequest("Gatepass", errors);
            }

            try
            {
                var result = await _service.CreateGatepassAsync(dto, CurrentUserId);
                return ApiResponse.Created(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating gatepass");
                return ApiResponse.BadRequest("Create Gatepass", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating gatepass");
                var innerMsg = ex.InnerException != null ? $" ({ex.InnerException.Message})" : "";
                return ApiResponse.InternalServerError($"Create Gatepass gagal: {ex.Message}{innerMsg}");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "GatepassUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] GatepassUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
                return ApiResponse.BadRequest("Gatepass", errors);
            }

            try
            {
                var result = await _service.UpdateGatepassAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error updating gatepass: {Id}", id);
                return ApiResponse.BadRequest("Update Gatepass", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating gatepass: {Id}", id);
                return ApiResponse.InternalServerError("Update Gatepass gagal: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "GatepassDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                await _service.DeleteGatepassAsync(id, CurrentUserId, userRole);
                return ApiResponse.Success(new { }, "Gatepass berhasil dihapus");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete gatepass: {Id}", id);
                return ApiResponse.BadRequest("Delete Gatepass", ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Gatepass not found: {Id}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gatepass: {Id}", id);
                return ApiResponse.InternalServerError("Delete Gatepass gagal: " + ex.Message);
            }
        }

        [HttpPost("{id}/sign")]
        [Authorize(Policy = "GatepassUpdate")]
        public async Task<IActionResult> Sign(int id)
        {
            try
            {
                var result = await _service.SignGatepassAsync(id, CurrentUserId);
                return ApiResponse.Success(result, "Gatepass berhasil ditandatangani");
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest("Sign Gatepass", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing gatepass: {Id}", id);
                return ApiResponse.InternalServerError("Sign Gatepass gagal: " + ex.Message);
            }
        }
    }
}
