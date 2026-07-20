using Pm.DTOs;
using Pm.DTOs.Common;

namespace Pm.Services
{
    public interface IOperationalDocumentService
    {
        Task<PagedResultDto<OperationalDocumentResponseDto>> GetAllAsync(OperationalDocumentQueryDto query);
        Task<OperationalDocumentSummaryDto> GetSummaryAsync();
        Task<OperationalDocumentResponseDto> GetByIdAsync(int id);
        Task<OperationalDocumentResponseDto> CreateAsync(OperationalDocumentCreateDto dto);
        Task<OperationalDocumentResponseDto> UpsertAsync(OperationalDocumentCreateDto dto);
        Task<OperationalDocumentResponseDto> UpdateAsync(int id, OperationalDocumentUpdateDto dto);
        Task<OperationalDocumentResponseDto> UpdateFollowUpStatusAsync(int id, string status, string? remark = null);
        Task<OperationalDocumentResponseDto> MarkBhpPaymentAsync(int id, int year, string invoiceNumber, string userName);
        Task<OperationalDocumentResponseDto> UnmarkBhpPaymentAsync(int id, int year);
        Task DeleteAsync(int id);

        /// <summary>
        /// Backfill: generate BHP checklist untuk semua dokumen ISR yang belum punya checklist.
        /// Digunakan sekali untuk data lama sebelum fitur BHP diimplementasi.
        /// </summary>
        Task<(int processedCount, int generatedCount)> BackfillBhpChecklistsAsync();
    }
}
