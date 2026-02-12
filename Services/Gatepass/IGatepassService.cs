using Pm.DTOs;
using Pm.DTOs.Common;

namespace Pm.Services
{
    public interface IGatepassService
    {
        Task<GatepassResponseDto> CreateGatepassAsync(GatepassCreateDto dto, int userId);
        Task<PagedResultDto<GatepassListDto>> GetGatepassesAsync(GatepassQueryDto query);
        Task<GatepassResponseDto?> GetGatepassByIdAsync(int id);
        Task<GatepassResponseDto> UpdateGatepassAsync(int id, GatepassUpdateDto dto, int userId);
        Task DeleteGatepassAsync(int id, int userId, string? userRole = null);
    }
}
