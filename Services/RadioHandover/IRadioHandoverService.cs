using Pm.DTOs.Common;
using Pm.DTOs.RadioHandover;

namespace Pm.Services.RadioHandover
{
    public interface IRadioHandoverService
    {
        Task<PagedResultDto<RadioHandoverListDto>> GetAllAsync(RadioHandoverQueryDto query, int currentUserId, string? roleName);
        Task<RadioHandoverDetailDto?> GetByIdAsync(int id);
        Task<RadioHandoverDetailDto> CreateAsync(CreateRadioHandoverDto dto, int currentUserId);
        Task<RadioHandoverDetailDto> CompleteReceiverSignatureAsync(int id, CompleteReceiverSignatureDto dto, int currentUserId);
        Task<List<UserOptionDto>> GetTechniciansAsync();
        Task<List<UserOptionDto>> GetWarehouseReceiversAsync();
        Task<List<UserOptionDto>> GetHelpdeskReceiversAsync();
    }
}
