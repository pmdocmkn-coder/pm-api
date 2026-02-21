using System.Collections.Generic;
using System.Threading.Tasks;
using Pm.DTOs.Common;
using Pm.DTOs.InternalLink;

namespace Pm.Services
{
    public interface IInternalLinkService
    {
        // CRUD Link
        Task<List<InternalLinkListDto>> GetLinksAsync();
        Task<InternalLinkDetailDto?> GetLinkByIdAsync(int id);
        Task<InternalLinkListDto> CreateLinkAsync(InternalLinkCreateDto dto, int userId);
        Task<InternalLinkListDto> UpdateLinkAsync(InternalLinkUpdateDto dto, int userId);
        Task DeleteLinkAsync(int id, int userId);

        // CRUD History
        Task<PagedResultDto<InternalLinkHistoryItemDto>> GetHistoriesAsync(InternalLinkHistoryQueryDto query);
        Task<InternalLinkHistoryDetailDto?> GetHistoryByIdAsync(int id);
        Task<InternalLinkHistoryItemDto> CreateHistoryAsync(InternalLinkHistoryCreateDto dto, int userId);
        Task<InternalLinkHistoryItemDto> UpdateHistoryAsync(int id, InternalLinkHistoryUpdateDto dto, int userId);
        Task DeleteHistoryAsync(int id, int userId);
    }
}
