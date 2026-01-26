using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Helper;
using Pm.Services;
using Pm.Services.Letter;

namespace Pm.Controllers.Surat;

[ApiController]
[Route("api/letter-numbers")]
[Produces("application/json")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class LetterNumberController(
    ILetterNumberService service,
    ILogger<LetterNumberController> logger) : ControllerBase
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
    public async Task<IActionResult> GetAll([FromQuery] LetterNumberQueryDto query)
    {
        try
        {
            var result = await service.GetLetterNumbersAsync(query);
            // Return paged result directly
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting letter numbers");
            return ApiResponse.BadRequest("Get Letter Numbers", ex.Message);
        }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "LetterNumberView")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await service.GetLetterNumberByIdAsync(id);
            if (result == null)
            {
                return ApiResponse.NotFound("Nomor surat tidak ditemukan");
            }
            return ApiResponse.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting letter number: {Id}", id);
            return ApiResponse.InternalServerError("Get Letter Number gagal: " + ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Policy = "LetterNumberCreate")]
    public async Task<IActionResult> Generate([FromBody] LetterNumberCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            return ApiResponse.BadRequest("Letter Number", errors);
        }

        try
        {
            var result = await service.GenerateLetterNumberAsync(dto, CurrentUserId);
            return ApiResponse.Created(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error generating letter number");
            return ApiResponse.BadRequest("Generate Letter Number", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating letter number");
            var innerMsg = ex.InnerException != null ? $" ({ex.InnerException.Message})" : "";
            return ApiResponse.InternalServerError($"Generate Letter Number gagal: {ex.Message}{innerMsg}");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "LetterNumberUpdate")]
    public async Task<IActionResult> Update(int id, [FromBody] LetterNumberUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();
            return ApiResponse.BadRequest("Letter Number", errors);
        }

        try
        {
            var result = await service.UpdateLetterNumberAsync(id, dto, CurrentUserId);
            return ApiResponse.Success(result);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Letter number not found: {Id}", id);
            return ApiResponse.NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error updating letter number: {Id}", id);
            return ApiResponse.BadRequest("Update Letter Number", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating letter number: {Id}", id);
            return ApiResponse.InternalServerError("Update Letter Number gagal: " + ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "LetterNumberDelete")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await service.DeleteLetterNumberAsync(id, CurrentUserId);
            return ApiResponse.Success(new { }, "Nomor surat berhasil dihapus");
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Letter number not found: {Id}", id);
            return ApiResponse.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting letter number: {Id}", id);
            return ApiResponse.InternalServerError("Delete Letter Number gagal: " + ex.Message);
        }
    }
}

