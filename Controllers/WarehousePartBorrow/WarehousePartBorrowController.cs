using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.WarehousePartBorrow;
using Pm.Helper;
using Pm.Services.WarehousePartBorrow;

namespace Pm.Controllers.WarehousePartBorrow
{
    [ApiController]
    [Route("api/warehouse-part-borrows")]
    [Authorize]
    public class WarehousePartBorrowController : ControllerBase
    {
        private readonly IWarehousePartBorrowService _service;

        public WarehousePartBorrowController(IWarehousePartBorrowService service) => _service = service;

        private int CurrentUserId =>
            int.Parse(User.FindFirst("UserId")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException());

        private string? RoleName => User.FindFirst("RoleName")?.Value;

        [HttpGet]
        [Authorize(Policy = "WarehouseBorrowView")]
        public async Task<IActionResult> GetAll([FromQuery] WarehousePartBorrowQueryDto query)
        {
            try
            {
                var data = await _service.GetAllAsync(query, CurrentUserId, RoleName);
                return ApiResponse.Success(data);
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpGet("pending")]
        [Authorize(Policy = "WarehouseBorrowSupervise")]
        public async Task<IActionResult> GetPending()
        {
            try
            {
                return ApiResponse.Success(await _service.GetPendingAsync());
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "WarehouseBorrowView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var data = await _service.GetByIdAsync(id, CurrentUserId, RoleName);
                if (data == null) return ApiResponse.NotFound("Peminjaman tidak ditemukan");
                return ApiResponse.Success(data);
            }
            catch (UnauthorizedAccessException) { return ApiResponse.Forbidden(); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPost]
        [Authorize(Policy = "WarehouseBorrowCreate")]
        public async Task<IActionResult> Create([FromBody] CreateWarehousePartBorrowDto dto)
        {
            try
            {
                var data = await _service.CreateAsync(dto, CurrentUserId);
                return ApiResponse.Created(data, "Permintaan peminjaman dibuat");
            }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Policy = "WarehouseBorrowSupervise")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveBorrowDto dto)
        {
            try
            {
                var data = await _service.ApproveAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("borrow", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/reject")]
        [Authorize(Policy = "WarehouseBorrowSupervise")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectBorrowDto dto)
        {
            try
            {
                var data = await _service.RejectAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("borrow", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/issue")]
        [Authorize(Policy = "WarehouseBorrowIssue")]
        public async Task<IActionResult> Issue(int id, [FromBody] IssueBorrowDto dto)
        {
            try
            {
                var data = await _service.IssueAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("borrow", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/sign-receiver")]
        [Authorize(Policy = "WarehouseBorrowView")]
        public async Task<IActionResult> SignReceiver(int id, [FromBody] SignReceiverBorrowDto dto)
        {
            try
            {
                var data = await _service.SignReceiverAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("borrow", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/return")]
        [Authorize(Policy = "WarehouseBorrowReturn")]
        public async Task<IActionResult> Return(int id, [FromBody] ReturnBorrowDto dto)
        {
            try
            {
                var data = await _service.ReturnAsync(id, dto, CurrentUserId, RoleName);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("borrow", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPatch("{id}/sign-return")]
        [Authorize(Policy = "WarehouseBorrowView")]
        public async Task<IActionResult> SignReturnReceiver(int id, [FromBody] SignReturnReceiverBorrowDto dto)
        {
            try
            {
                var data = await _service.SignReturnReceiverAsync(id, dto, CurrentUserId);
                return ApiResponse.Success(data);
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("borrow", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Policy = "WarehouseBorrowCancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _service.CancelAsync(id, CurrentUserId);
                return ApiResponse.Success(null, "Permintaan dibatalkan");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (UnauthorizedAccessException) { return ApiResponse.Forbidden(); }
            catch (InvalidOperationException ex) { return ApiResponse.BadRequest("borrow", new[] { ex.Message }); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "WarehouseBorrowCreate")] // Only admins can soft delete
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return ApiResponse.Success(null, "Peminjaman berhasil dihapus.");
            }
            catch (KeyNotFoundException ex) { return ApiResponse.NotFound(ex.Message); }
            catch (UnauthorizedAccessException) { return ApiResponse.Forbidden(); }
            catch (Exception ex) { return ApiResponse.InternalServerError(ex.Message); }
        }
    }
}
