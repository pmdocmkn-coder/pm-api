using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Pm.DTOs.Common;
using Pm.DTOs.Radio;

namespace Pm.Services.Radio
{
    public interface IRadioService
    {
        Task<PagedResultDto<RadioDto>> GetAllAsync(RadioQueryDto query);
        Task<IEnumerable<RadioDto>> GetAllUnpagedAsync(string? category = null, bool isScrap = false);
        Task<List<DuplicateSnDto>> GetDuplicateSerialNumbersAsync();
        Task<RadioDto> GetByIdAsync(int id);
        Task<List<RadioLookupDto>> LookupBySerialAsync(string serialNumber);
        Task<IEnumerable<RadioHistoryDto>> GetHistoryAsync(int id);
        Task<RadioDto> CreateAsync(CreateRadioDto dto, int userId);
        Task<RadioDto> UpdateAsync(int id, UpdateRadioDto dto, int userId);
        Task DeleteAsync(int id, int userId);
        Task DeleteAllAsync(int userId);
        Task<int> DeleteByCategoryAsync(string category, int userId);
        Task<RadioDto> ScrapRadioAsync(int id, ScrapRadioDto dto, int userId);
        
        // Import endpoints - return detailed result
        Task<ImportResultDto> ImportInternalAsync(IFormFile file, int userId);
        Task<ImportResultDto> ImportContractorAsync(IFormFile file, int userId);
        Task<ImportResultDto> ImportUnitAsync(IFormFile file, int userId);
        Task<ImportResultDto> ImportLegacyScrapAsync(IFormFile file, int userId);
    }
}
