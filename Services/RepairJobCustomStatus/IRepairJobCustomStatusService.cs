using Pm.DTOs.RepairJobCustomStatus;

namespace Pm.Services.RepairJobCustomStatus
{
    public interface IRepairJobCustomStatusService
    {
        Task<List<RepairJobCustomStatusDto>> GetAllAsync();
        Task<RepairJobCustomStatusDto> CreateAsync(CreateRepairJobCustomStatusDto dto, int userId);
        Task<RepairJobCustomStatusDto> UpdateAsync(int id, UpdateRepairJobCustomStatusDto dto);
        Task DeleteAsync(int id);
    }
}
