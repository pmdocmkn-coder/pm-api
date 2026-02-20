using Pm.DTOs;
using Pm.DTOs.Common;

namespace Pm.Services;

public interface IDivisionService
{
    Task<DivisionResponseDto> CreateAsync(DivisionCreateDto dto, int userId);
    Task<DivisionResponseDto> UpdateAsync(int id, DivisionUpdateDto dto, int userId);
    Task DeleteAsync(int id, int userId);
    Task<DivisionResponseDto?> GetByIdAsync(int id);
    Task<PagedResultDto<DivisionListDto>> GetAllAsync(DivisionQueryDto query);
}
