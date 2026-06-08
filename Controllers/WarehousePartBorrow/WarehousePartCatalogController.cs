using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.WarehousePartBorrow;
using Pm.Helper;
using Pm.Services.WarehousePartBorrow;

namespace Pm.Controllers.WarehousePartBorrow
{
    [ApiController]
    [Route("api/warehouse-part-catalog")]
    [Authorize]
    public class WarehousePartCatalogController : ControllerBase
    {
        private readonly IWarehousePartCatalogService _service;

        public WarehousePartCatalogController(IWarehousePartCatalogService service) => _service = service;

        [HttpGet]
        [Authorize(Policy = "WarehouseBorrowView")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            try
            {
                return ApiResponse.Success(await _service.GetAllAsync(page, pageSize, search));
            }
            catch (Exception ex)
            {
                return ApiResponse.InternalServerError(ex.Message);
            }
        }

        [HttpGet("search")]
        [Authorize(Policy = "WarehouseBorrowView")]
        public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] int limit = 10)
        {
            try
            {
                return ApiResponse.Success(await _service.SearchAsync(query, limit));
            }
            catch (Exception ex)
            {
                return ApiResponse.InternalServerError(ex.Message);
            }
        }

        [HttpPost("import")]
        [Authorize(Policy = "WarehouseBorrowCreate")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException());
                var result = await _service.ImportAsync(file, userId);
                return ApiResponse.Success(result, result.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse.InternalServerError(ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "WarehouseBorrowCreate")]
        public async Task<IActionResult> Create([FromBody] CreateUpdateWarehousePartCatalogDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException());
                return ApiResponse.Created(await _service.CreateAsync(dto, userId));
            }
            catch (Exception ex)
            {
                return ApiResponse.InternalServerError(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "WarehouseBorrowCreate")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateUpdateWarehousePartCatalogDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedAccessException());
                return ApiResponse.Success(await _service.UpdateAsync(id, dto, userId));
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse.InternalServerError(ex.Message);
            }
        }
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "WarehouseBorrowCreate")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return ApiResponse.Success("Part berhasil dihapus.");
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse.InternalServerError(ex.Message);
            }
        }

        [HttpDelete("all")]
        [Authorize(Policy = "WarehouseBorrowCreate")]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                await _service.DeleteAllAsync();
                return ApiResponse.Success("Semua data part berhasil dihapus.");
            }
            catch (Exception ex)
            {
                return ApiResponse.InternalServerError(ex.Message);
            }
        }
    }
}
