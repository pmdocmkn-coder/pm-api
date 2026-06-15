using Pm.DTOs;

namespace Pm.Services
{
    public interface IWorkshopTechnicianService
    {
        Task<List<WorkshopTechnicianDto>> GetAllAsync(bool includeInactive = false);
        Task<WorkshopTechnicianDto?> GetByIdAsync(int id);
        Task<WorkshopTechnicianDto> CreateAsync(CreateWorkshopTechnicianDto dto, int currentUserId);
        Task<WorkshopTechnicianDto> UpdateAsync(int id, UpdateWorkshopTechnicianDto dto, int currentUserId);
        Task DeleteAsync(int id, int currentUserId);
    }
}
