using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.DTOs.KpiDocument;

namespace Pm.Services
{
    public interface IKpiDocumentService
    {
        Task<PagedResultDto<KpiDocumentDto>> GetAllAsync(KpiDocumentQueryDto query);
        Task<KpiDocumentDto> CreateAsync(CreateKpiDocumentDto dto, int userId);
        Task<KpiDocumentDto> UpdateAsync(int id, UpdateKpiDocumentDto dto, int userId);
        Task<KpiDocumentDto> UpdateDatesAsync(int id, UpdateKpiDocumentDatesDto dto, int userId);
        Task DeleteAsync(int id, int userId);
        Task<List<KpiDocumentDto>> CloneFromPreviousMonthAsync(DateTime sourceMonth, DateTime targetMonth, int userId);
        Task DeleteMonthDataAsync(DateTime targetMonth, int userId);
        Task<byte[]> ExportExcelAsync(KpiDocumentQueryDto query);
        Task<int> ImportExcelAsync(Microsoft.AspNetCore.Http.IFormFile file, int userId);
    }
}
