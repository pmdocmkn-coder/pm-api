using Pm.DTOs;
using Pm.DTOs.Common;

namespace Pm.Services
{
    public interface IOperationalDocumentService
    {
        Task<PagedResultDto<OperationalDocumentResponseDto>> GetAllAsync(OperationalDocumentQueryDto query);
        Task<OperationalDocumentSummaryDto> GetSummaryAsync();
        Task<OperationalDocumentResponseDto> GetByIdAsync(int id);
        Task<OperationalDocumentResponseDto> CreateAsync(OperationalDocumentCreateDto dto);
        Task<OperationalDocumentResponseDto> UpsertAsync(OperationalDocumentCreateDto dto);
        Task<OperationalDocumentResponseDto> UpdateAsync(int id, OperationalDocumentUpdateDto dto);
        Task<OperationalDocumentResponseDto> UpdateFollowUpStatusAsync(int id, string status, string? remark = null);
        Task DeleteAsync(int id);
    }
}
