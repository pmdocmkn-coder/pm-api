using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Pm.DTOs.Radio;

namespace Pm.Services.Radio
{
    public interface IRadioService
    {
        Task<IEnumerable<RadioDto>> GetAllAsync(string? category = null, bool isScrap = false);
        Task<RadioDto> GetByIdAsync(int id);
        Task<IEnumerable<RadioHistoryDto>> GetHistoryAsync(int id);
        Task<RadioDto> CreateAsync(CreateRadioDto dto, int userId);
        Task<RadioDto> UpdateAsync(int id, UpdateRadioDto dto, int userId);
        Task DeleteAsync(int id, int userId);
        Task DeleteAllAsync(int userId);
        Task<RadioDto> ScrapRadioAsync(int id, ScrapRadioDto dto, int userId);
        
        // Import endpoints
        Task<int> ImportInternalAsync(IFormFile file, int userId);
        Task<int> ImportContractorAsync(IFormFile file, int userId);
        Task<int> ImportUnitAsync(IFormFile file, int userId);
        Task<int> ImportLegacyScrapAsync(IFormFile file, int userId);
    }
}
