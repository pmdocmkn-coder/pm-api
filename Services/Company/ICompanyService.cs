using Pm.DTOs;
using Pm.DTOs.Common;

namespace Pm.Services.Company;

public interface ICompanyService
{
    Task<CompanyResponseDto> CreateAsync(CompanyCreateDto dto, int userId);
    Task<CompanyResponseDto> UpdateAsync(int id, CompanyUpdateDto dto, int userId);
    Task DeleteAsync(int id, int userId);
    Task<CompanyResponseDto?> GetByIdAsync(int id);
    Task<PagedResultDto<CompanyListDto>> GetAllAsync(CompanyQueryDto query);
}

