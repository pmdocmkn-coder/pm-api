using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/divisions")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class DivisionController(
        IDivisionService service,
        ILogger<DivisionController> logger) : ControllerBase
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
        [Authorize(Policy = "DivisiView")]
        public async Task<IActionResult> GetAll([FromQuery] DivisionQueryDto query)
        {
            try
            {
                var result = await service.GetAllAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting divisions");
                return ApiResponse.BadRequest("Get Divisions", ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "DivisiView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await service.GetByIdAsync(id);
                if (result == null)
                {
                    return ApiResponse.NotFound("Division tidak ditemukan");
                }
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting division: {Id}", id);
                return ApiResponse.InternalServerError("Get Division gagal: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "DivisiCreate")]
        public async Task<IActionResult> Create([FromBody] DivisionCreateDto dto)
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
                logger.LogWarning(ex, "Validation error creating division");
                return ApiResponse.BadRequest("Create Division", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating division");
                return ApiResponse.InternalServerError("Create Division gagal: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "DivisiUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] DivisionUpdateDto dto)
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
                logger.LogWarning(ex, "Division not found: {Id}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Validation error updating division: {Id}", id);
                return ApiResponse.BadRequest("Update Division", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating division: {Id}", id);
                return ApiResponse.InternalServerError("Update Division gagal: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "DivisiDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await service.DeleteAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "Division berhasil dihapus");
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Division not found: {Id}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Cannot delete division: {Id}", id);
                return ApiResponse.BadRequest("Delete Division", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting division: {Id}", id);
                return ApiResponse.InternalServerError("Delete Division gagal: " + ex.Message);
            }
        }

        [HttpGet("debug/permissions")]
        [Authorize]
        public IActionResult CheckPermissions()
        {
            var userId = CurrentUserId;

            // Get all claims from the current token
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

            var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
            var hasDivisionCreate = permissions.Contains("division.create");

            return Ok(new
            {
                UserId = userId,
                TotalClaims = claims.Count,
                HasDivisionCreate = hasDivisionCreate,
                Permissions = permissions,
                AllClaims = claims,
                Message = hasDivisionCreate
                    ? "You HAVE the permission 'division.create'. Policy should work."
                    : "You MISSING the permission 'division.create'. Please Logout and Login again."
            });
        }
    }
}
