using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;

namespace Pm.Services
{
    public class WorkshopTechnicianService : IWorkshopTechnicianService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;

        public WorkshopTechnicianService(AppDbContext context, IActivityLogService activityLog)
        {
            _context = context;
            _activityLog = activityLog;
        }

        public async Task<List<WorkshopTechnicianDto>> GetAllAsync(bool includeInactive = false)
        {
            var query = _context.WorkshopTechnicians
                .Where(t => !t.IsDeleted);

            if (!includeInactive)
                query = query.Where(t => t.IsActive);

            return await query.OrderBy(t => t.Name)
                .Select(t => new WorkshopTechnicianDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    UserId = t.UserId
                })
                .ToListAsync();
        }

        public async Task<WorkshopTechnicianDto?> GetByIdAsync(int id)
        {
            var t = await _context.WorkshopTechnicians
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
                
            if (t == null) return null;

            return new WorkshopTechnicianDto
            {
                Id = t.Id,
                Name = t.Name,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                UserId = t.UserId
            };
        }

        public async Task<WorkshopTechnicianDto> CreateAsync(CreateWorkshopTechnicianDto dto, int currentUserId)
        {
            var tech = new Pm.Models.WorkshopTechnician
            {
                Name = dto.Name.Trim(),
                IsActive = dto.IsActive,
                UserId = dto.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkshopTechnicians.Add(tech);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("WorkshopTechnician", tech.Id, "Create", currentUserId, $"Tambah teknisi: {tech.Name}");

            return (await GetByIdAsync(tech.Id))!;
        }

        public async Task<WorkshopTechnicianDto> UpdateAsync(int id, UpdateWorkshopTechnicianDto dto, int currentUserId)
        {
            var tech = await _context.WorkshopTechnicians
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
                ?? throw new KeyNotFoundException("Teknisi tidak ditemukan");

            tech.Name = dto.Name.Trim();
            tech.IsActive = dto.IsActive;
            tech.UserId = dto.UserId;
            tech.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("WorkshopTechnician", tech.Id, "Update", currentUserId, $"Update teknisi: {tech.Name}");

            return (await GetByIdAsync(tech.Id))!;
        }

        public async Task DeleteAsync(int id, int currentUserId)
        {
            var tech = await _context.WorkshopTechnicians
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
                ?? throw new KeyNotFoundException("Teknisi tidak ditemukan");

            tech.IsDeleted = true;
            tech.DeletedAt = DateTime.UtcNow;
            tech.DeletedByUserId = currentUserId;

            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("WorkshopTechnician", tech.Id, "Delete", currentUserId, $"Hapus teknisi: {tech.Name}");
        }
    }
}
