using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.RepairJobCustomStatus;
using Pm.Enums;
using Pm.Models;

namespace Pm.Services.RepairJobCustomStatus
{
    public class RepairJobCustomStatusService : IRepairJobCustomStatusService
    {
        private readonly AppDbContext _context;

        public RepairJobCustomStatusService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RepairJobCustomStatusDto>> GetAllAsync()
        {
            var statuses = await _context.RepairJobCustomStatuses
                .AsNoTracking()
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.CreatedAt)
                .ToListAsync();

            // Hitung berapa job aktif yang menggunakan masing-masing status custom
            var activeJobCounts = await _context.RadioRepairJobs
                .AsNoTracking()
                .Where(j => !j.IsDeleted && j.CustomStatusId != null &&
                            j.Status != RadioRepairJobStatus.HandedToWarehouse &&
                            j.Status != RadioRepairJobStatus.ReturnedToHelpdesk &&
                            j.Status != RadioRepairJobStatus.Cancelled)
                .GroupBy(j => j.CustomStatusId!.Value)
                .Select(g => new { CustomStatusId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CustomStatusId, x => x.Count);

            return statuses.Select(s => new RepairJobCustomStatusDto
            {
                Id = s.Id,
                Label = s.Label,
                Color = s.Color,
                SortOrder = s.SortOrder,
                IsActive = s.IsActive,
                ActiveJobCount = activeJobCounts.GetValueOrDefault(s.Id, 0),
                CreatedAt = s.CreatedAt
            }).ToList();
        }

        public async Task<RepairJobCustomStatusDto> CreateAsync(CreateRepairJobCustomStatusDto dto, int userId)
        {
            // Cek duplikat label
            var exists = await _context.RepairJobCustomStatuses
                .AnyAsync(s => s.Label.ToLower() == dto.Label.Trim().ToLower());
            if (exists)
                throw new InvalidOperationException($"Status dengan label \"{dto.Label}\" sudah ada.");

            var status = new Pm.Models.RepairJobCustomStatus
            {
                Label = dto.Label.Trim(),
                Color = dto.Color.Trim(),
                SortOrder = dto.SortOrder,
                IsActive = true,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.RepairJobCustomStatuses.Add(status);
            await _context.SaveChangesAsync();

            return new RepairJobCustomStatusDto
            {
                Id = status.Id,
                Label = status.Label,
                Color = status.Color,
                SortOrder = status.SortOrder,
                IsActive = status.IsActive,
                ActiveJobCount = 0,
                CreatedAt = status.CreatedAt
            };
        }

        public async Task<RepairJobCustomStatusDto> UpdateAsync(int id, UpdateRepairJobCustomStatusDto dto)
        {
            var status = await _context.RepairJobCustomStatuses.FindAsync(id)
                ?? throw new KeyNotFoundException("Status tidak ditemukan.");

            // Cek duplikat label (kecuali dirinya sendiri)
            var exists = await _context.RepairJobCustomStatuses
                .AnyAsync(s => s.Id != id && s.Label.ToLower() == dto.Label.Trim().ToLower());
            if (exists)
                throw new InvalidOperationException($"Status dengan label \"{dto.Label}\" sudah ada.");

            status.Label = dto.Label.Trim();
            status.Color = dto.Color.Trim();
            status.SortOrder = dto.SortOrder;
            status.IsActive = dto.IsActive;
            status.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var activeJobCount = await _context.RadioRepairJobs
                .CountAsync(j => !j.IsDeleted && j.CustomStatusId == id &&
                                 j.Status != RadioRepairJobStatus.HandedToWarehouse &&
                                 j.Status != RadioRepairJobStatus.ReturnedToHelpdesk &&
                                 j.Status != RadioRepairJobStatus.Cancelled);

            return new RepairJobCustomStatusDto
            {
                Id = status.Id,
                Label = status.Label,
                Color = status.Color,
                SortOrder = status.SortOrder,
                IsActive = status.IsActive,
                ActiveJobCount = activeJobCount,
                CreatedAt = status.CreatedAt
            };
        }

        public async Task DeleteAsync(int id)
        {
            var status = await _context.RepairJobCustomStatuses.FindAsync(id)
                ?? throw new KeyNotFoundException("Status tidak ditemukan.");

            // Cek apakah masih ada job aktif yang menggunakan status ini
            var activeJobCount = await _context.RadioRepairJobs
                .CountAsync(j => !j.IsDeleted && j.CustomStatusId == id &&
                                 j.Status != RadioRepairJobStatus.HandedToWarehouse &&
                                 j.Status != RadioRepairJobStatus.ReturnedToHelpdesk &&
                                 j.Status != RadioRepairJobStatus.Cancelled);

            if (activeJobCount > 0)
                throw new InvalidOperationException(
                    $"Status \"{status.Label}\" masih digunakan oleh {activeJobCount} pekerjaan aktif. " +
                    "Pindahkan semua pekerjaan ke status lain sebelum menghapus.");

            _context.RepairJobCustomStatuses.Remove(status);
            await _context.SaveChangesAsync();
        }
    }
}
