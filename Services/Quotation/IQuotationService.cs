using Microsoft.EntityFrameworkCore;
using Pm.DTOs;
using Pm.DTOs.Common;

namespace Pm.Services
{
    public interface IQuotationService
    {
        Task<QuotationResponseDto> CreateQuotationAsync(QuotationCreateDto dto, int userId);
        Task<PagedResultDto<QuotationListDto>> GetQuotationsAsync(QuotationQueryDto query);
        Task<QuotationResponseDto?> GetQuotationByIdAsync(int id);
        Task<QuotationResponseDto> UpdateQuotationAsync(int id, QuotationUpdateDto dto, int userId);
        Task DeleteQuotationAsync(int id, int userId, string? userRole = null);
    }
}
