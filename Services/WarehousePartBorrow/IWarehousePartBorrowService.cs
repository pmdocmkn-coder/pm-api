using Pm.DTOs.Common;
using Pm.DTOs.WarehousePartBorrow;

namespace Pm.Services.WarehousePartBorrow
{
    public interface IWarehousePartBorrowService
    {
        Task<PagedResultDto<WarehousePartBorrowListDto>> GetAllAsync(WarehousePartBorrowQueryDto query, int currentUserId, string? roleName);
        Task<List<WarehousePartBorrowListDto>> GetPendingAsync();
        Task<WarehousePartBorrowDetailDto?> GetByIdAsync(int id, int currentUserId, string? roleName);
        Task<WarehousePartBorrowDetailDto> CreateAsync(CreateWarehousePartBorrowDto dto, int userId);
        Task<WarehousePartBorrowDetailDto> ApproveAsync(int id, ApproveBorrowDto dto, int userId);
        Task<WarehousePartBorrowDetailDto> RejectAsync(int id, RejectBorrowDto dto, int userId);
        Task<WarehousePartBorrowDetailDto> IssueAsync(int id, int userId);
        Task<WarehousePartBorrowDetailDto> ReturnAsync(int id, ReturnBorrowDto dto, int userId, string? roleName);
        Task CancelAsync(int id, int userId);
    }
}
