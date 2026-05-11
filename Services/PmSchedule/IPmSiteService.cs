using System.Collections.Generic;
using System.Threading.Tasks;
using Pm.DTOs;

namespace Pm.Services.PmSchedule
{
    public interface IPmSiteService
    {
        Task<List<PmSiteDto>> GetAllSitesAsync();
        Task<PmSiteDto> CreateSiteAsync(PmSiteDto dto, int userId);
        Task<PmSiteDto?> UpdateSiteAsync(int id, PmSiteDto dto, int userId);
        Task<bool> DeleteSiteAsync(int id, int userId);
    }
}
