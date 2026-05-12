using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.Models.PmSchedule;
using Pm.Services;

namespace Pm.Services.PmSchedule
{
    public class PmSiteService : IPmSiteService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        public PmSiteService(AppDbContext context, IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        public async Task<List<PmSiteDto>> GetAllSitesAsync()
        {
            var sites = await _context.PmSites
                .OrderBy(s => s.OrderIndex)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return sites.Select(s => new PmSiteDto
            {
                Id = s.Id,
                Name = s.Name,
                OrderIndex = s.OrderIndex
            }).ToList();
        }

        public async Task<PmSiteDto> CreateSiteAsync(PmSiteDto dto, int userId)
        {
            var site = new PmSite
            {
                Name = dto.Name,
                OrderIndex = dto.OrderIndex
            };

            _context.PmSites.Add(site);
            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync("PmSite", site.Id, "Create", userId, $"Membuat PM Site baru: {dto.Name}");

            dto.Id = site.Id;
            return dto;
        }

        public async Task<PmSiteDto?> UpdateSiteAsync(int id, PmSiteDto dto, int userId)
        {
            var site = await _context.PmSites.FindAsync(id);
            if (site == null) return null;

            site.Name = dto.Name;
            site.OrderIndex = dto.OrderIndex;

            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync("PmSite", site.Id, "Update", userId, $"Update PM Site ID {site.Id}");

            return dto;
        }

        public async Task<bool> UpdateSiteOrdersAsync(List<PmSiteOrderDto> orders, int userId)
        {
            var siteIds = orders.Select(o => o.Id).ToList();
            var sites = await _context.PmSites.Where(s => siteIds.Contains(s.Id)).ToListAsync();

            foreach (var order in orders)
            {
                var site = sites.FirstOrDefault(s => s.Id == order.Id);
                if (site != null)
                {
                    site.OrderIndex = order.OrderIndex;
                }
            }

            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync("PmSite", 0, "UpdateOrder", userId, $"Update urutan {orders.Count} PM Sites");

            return true;
        }

        public async Task<bool> DeleteSiteAsync(int id, int userId)
        {
            var site = await _context.PmSites.FindAsync(id);
            if (site == null) return false;

            _context.PmSites.Remove(site);
            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync("PmSite", id, "Delete", userId, $"Menghapus PM Site ID {id}");

            return true;
        }
    }
}
