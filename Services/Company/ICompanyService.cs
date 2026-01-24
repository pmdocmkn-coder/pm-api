using Pm.DTOs;

namespace Pm.Services
{
    public interface ICompanyService
    {
        Task<CompanyResponseDto> CreateAsync(CompanyCreateDto dto, int userId);
        Task<CompanyResponseDto> UpdateAsync(int id, CompanyUpdateDto dto, int userId);
        Task DeleteAsync(int id, int userId);
        Task<CompanyResponseDto?> GetByIdAsync(int id);
        Task<List<CompanyListDto>> GetAllAsync(bool activeOnly = true);
    }
}
