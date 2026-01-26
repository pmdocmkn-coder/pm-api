using Pm.DTOs;
using Pm.DTOs.Common;

namespace Pm.Services.Letter
{
    public interface ILetterNumberService
    {
        Task<LetterNumberResponseDto> GenerateLetterNumberAsync(LetterNumberCreateDto dto, int userId);
        Task<PagedResultDto<LetterNumberListDto>> GetLetterNumbersAsync(LetterNumberQueryDto query);
        Task<LetterNumberResponseDto?> GetLetterNumberByIdAsync(int id);
        Task<LetterNumberResponseDto> UpdateLetterNumberAsync(int id, LetterNumberUpdateDto dto, int userId);
        Task DeleteLetterNumberAsync(int id, int userId);
    }
}
