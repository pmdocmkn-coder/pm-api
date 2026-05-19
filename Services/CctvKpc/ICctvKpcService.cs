using System.Collections.Generic;
using System.Threading.Tasks;
using Pm.DTOs.Common;
using Pm.DTOs.CctvKpc;

namespace Pm.Services.CctvKpc
{
    public interface ICctvKpcService
    {
        Task<PagedResultDto<CctvKpcDto>> GetAllAsync(CctvKpcQueryDto query);
        Task<IEnumerable<CctvKpcDto>> GetAllUnpagedAsync();
        Task<CctvKpcDto> GetByIdAsync(int id);
        Task<CctvKpcDto> CreateAsync(CreateCctvKpcDto dto, int userId);
        Task<CctvKpcDto> UpdateAsync(int id, UpdateCctvKpcDto dto, int userId);
        Task DeleteAsync(int id, int userId);
        Task DeleteAllAsync(int userId);
    }
}
