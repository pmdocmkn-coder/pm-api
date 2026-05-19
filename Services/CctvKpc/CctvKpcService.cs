using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.CctvKpc;
using Pm.Models;
using Pm.Services;

namespace Pm.Services.CctvKpc
{
    public class CctvKpcService : ICctvKpcService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<CctvKpcService> _logger;

        public CctvKpcService(AppDbContext context, IActivityLogService activityLog, ILogger<CctvKpcService> logger)
        {
            _context = context;
            _activityLog = activityLog;
            _logger = logger;
        }

        private async Task<string> GetUserDisplayNameAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new { u.FullName, u.Username })
                .FirstOrDefaultAsync();

            if (user == null) return userId.ToString();
            if (!string.IsNullOrWhiteSpace(user.FullName))
                return $"{user.FullName} ({user.Username})";
            return user.Username ?? userId.ToString();
        }

        private static CctvKpcDto MapToDto(Models.CctvKpc c) => new()
        {
            Id = c.Id,
            Severity = c.Severity,
            Camera = c.Camera,
            IpCamera = c.IpCamera,
            Model = c.Model,
            Brand = c.Brand,
            ExplicitLocation = c.ExplicitLocation,
            FotoKoordinat = c.FotoKoordinat,
            Remarks = c.Remarks,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };

        // ============================================
        // GET ALL — PAGED
        // ============================================
        public async Task<PagedResultDto<CctvKpcDto>> GetAllAsync(CctvKpcQueryDto query)
        {
            var q = _context.CctvKpcs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(c =>
                    (c.Camera != null && c.Camera.ToLower().Contains(s)) ||
                    (c.IpCamera != null && c.IpCamera.ToLower().Contains(s)) ||
                    (c.Model != null && c.Model.ToLower().Contains(s)) ||
                    (c.Brand != null && c.Brand.ToLower().Contains(s)) ||
                    (c.ExplicitLocation != null && c.ExplicitLocation.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(query.Severity))
                q = q.Where(c => c.Severity == query.Severity);

            if (!string.IsNullOrWhiteSpace(query.Brand))
                q = q.Where(c => c.Brand == query.Brand);

            if (query.IsActive.HasValue)
                q = q.Where(c => c.IsActive == query.IsActive.Value);

            var totalCount = await q.CountAsync();
            var items = await q
                .OrderBy(c => c.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResultDto<CctvKpcDto>(items.Select(MapToDto).ToList(), query, totalCount);
        }

        // ============================================
        // GET ALL UNPAGED
        // ============================================
        public async Task<IEnumerable<CctvKpcDto>> GetAllUnpagedAsync()
        {
            var items = await _context.CctvKpcs.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
            return items.Select(MapToDto);
        }

        // ============================================
        // GET BY ID
        // ============================================
        public async Task<CctvKpcDto> GetByIdAsync(int id)
        {
            var item = await _context.CctvKpcs.FindAsync(id);
            if (item == null) throw new KeyNotFoundException("CCTV tidak ditemukan");
            return MapToDto(item);
        }

        // ============================================
        // CREATE
        // ============================================
        public async Task<CctvKpcDto> CreateAsync(CreateCctvKpcDto dto, int userId)
        {
            var item = new Models.CctvKpc
            {
                Severity = dto.Severity,
                Camera = dto.Camera,
                IpCamera = dto.IpCamera,
                Model = dto.Model,
                Brand = dto.Brand,
                ExplicitLocation = dto.ExplicitLocation,
                FotoKoordinat = dto.FotoKoordinat,
                Remarks = dto.Remarks,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.CctvKpcs.Add(item);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("CctvKpc", item.Id, "Create", userId,
                $"CCTV '{item.Camera}' ditambahkan (Severity: {item.Severity})");

            return MapToDto(item);
        }

        // ============================================
        // UPDATE
        // ============================================
        public async Task<CctvKpcDto> UpdateAsync(int id, UpdateCctvKpcDto dto, int userId)
        {
            var item = await _context.CctvKpcs.FindAsync(id);
            if (item == null) throw new KeyNotFoundException("CCTV tidak ditemukan");

            item.Severity = dto.Severity;
            item.Camera = dto.Camera;
            item.IpCamera = dto.IpCamera;
            item.Model = dto.Model;
            item.Brand = dto.Brand;
            item.ExplicitLocation = dto.ExplicitLocation;
            item.FotoKoordinat = dto.FotoKoordinat;
            item.Remarks = dto.Remarks;
            item.IsActive = dto.IsActive;
            item.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("CctvKpc", item.Id, "Update", userId,
                $"CCTV '{item.Camera}' diperbarui");

            return MapToDto(item);
        }

        // ============================================
        // DELETE
        // ============================================
        public async Task DeleteAsync(int id, int userId)
        {
            var item = await _context.CctvKpcs.FindAsync(id);
            if (item == null) throw new KeyNotFoundException("CCTV tidak ditemukan");

            _context.CctvKpcs.Remove(item);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("CctvKpc", id, "Delete", userId,
                $"CCTV '{item.Camera}' dihapus");
        }

        // ============================================
        // DELETE ALL
        // ============================================
        public async Task DeleteAllAsync(int userId)
        {
            var all = await _context.CctvKpcs.ToListAsync();
            _context.CctvKpcs.RemoveRange(all);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("CctvKpc", 0, "DeleteAll", userId,
                $"Seluruh data CCTV KPC dihapus ({all.Count} records)");
        }
    }
}
