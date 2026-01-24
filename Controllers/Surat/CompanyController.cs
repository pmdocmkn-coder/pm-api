using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers.Surat
{
    [ApiController]
    [Route("api/companies")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class CompanyController(
        ICompanyService service,
        ILogger<CompanyController> logger) : ControllerBase
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
        public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
        {
            try
            {
                var result = await service.GetAllAsync(activeOnly);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting companies");
                return ApiResponse.InternalServerError("Get Companies gagal: " + ex.Message);
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
                    return ApiResponse.NotFound("Company tidak ditemukan");
                }
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting company: {Id}", id);
                return ApiResponse.InternalServerError("Get Company gagal: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "LetterNumberCreate")]
        public async Task<IActionResult> Create([FromBody] CompanyCreateDto dto)
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
                logger.LogWarning(ex, "Validation error creating company");
                return ApiResponse.BadRequest("Create Company", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating company");
                return ApiResponse.InternalServerError("Create Company gagal: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "LetterNumberEdit")]
        public async Task<IActionResult> Update(int id, [FromBody] CompanyUpdateDto dto)
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
                logger.LogWarning(ex, "Company not found: {Id}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Validation error updating company: {Id}", id);
                return ApiResponse.BadRequest("Update Company", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating company: {Id}", id);
                return ApiResponse.InternalServerError("Update Company gagal: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "LetterNumberDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await service.DeleteAsync(id, CurrentUserId);
                return ApiResponse.Success(new { }, "Company berhasil dihapus");
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Company not found: {Id}", id);
                return ApiResponse.NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Cannot delete company: {Id}", id);
                return ApiResponse.BadRequest("Delete Company", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting company: {Id}", id);
                return ApiResponse.InternalServerError("Delete Company gagal: " + ex.Message);
            }
        }
    }
}
