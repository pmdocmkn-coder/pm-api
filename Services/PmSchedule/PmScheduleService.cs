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
    public class PmScheduleService : IPmScheduleService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        public PmScheduleService(AppDbContext context, IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        public async Task<PmYearlyScheduleResponseDto> GetYearlyScheduleAsync(int year)
        {
            var response = new PmYearlyScheduleResponseDto { Year = year };

            // Get all PM sites, ordered
            var sites = await _context.PmSites
                .OrderBy(s => s.OrderIndex)
                .ThenBy(s => s.Name)
                .ToListAsync();

            // Get all schedules for the specified year
            var schedules = await _context.PmSchedules
                .Include(s => s.Tasks)
                .Where(s => s.Year == year)
                .ToListAsync();

            // Grouping into the response structure
            foreach (var site in sites)
            {
                var siteDto = new PmSiteScheduleDto
                {
                    SiteId = site.Id,
                    SiteName = site.Name,
                    OrderIndex = site.OrderIndex,
                    Devices = new List<PmDeviceScheduleDto>()
                };

                var siteSchedules = schedules.Where(s => s.PmSiteId == site.Id).ToList();
                foreach (var schedule in siteSchedules)
                {
                    var deviceDto = new PmDeviceScheduleDto
                    {
                        ScheduleId = schedule.Id,
                        DeviceName = schedule.DeviceName,
                        Tasks = schedule.Tasks.Select(t => new PmScheduleTaskDto
                        {
                            Month = t.Month,
                            Week = t.Week
                        }).ToList()
                    };
                    siteDto.Devices.Add(deviceDto);
                }

                response.Sites.Add(siteDto);
            }

            return response;
        }

        public async Task<bool> UpsertScheduleAsync(PmScheduleUpsertDto dto, int userId)
        {
            // Find existing schedule header for this site, device and year
            var schedule = await _context.PmSchedules
                .Include(s => s.Tasks)
                .FirstOrDefaultAsync(s => s.Year == dto.Year 
                                          && s.PmSiteId == dto.PmSiteId 
                                          && s.DeviceName == dto.DeviceName);

            string action = "Update";

            if (schedule == null)
            {
                action = "Create";
                schedule = new Models.PmSchedule.PmSchedule
                {
                    Year = dto.Year,
                    PmSiteId = dto.PmSiteId,
                    DeviceName = dto.DeviceName,
                    Tasks = new List<PmScheduleTask>()
                };
                _context.PmSchedules.Add(schedule);
            }
            else
            {
                // Remove existing tasks
                _context.PmScheduleTasks.RemoveRange(schedule.Tasks);
                schedule.Tasks.Clear();
            }

            // Add new tasks (if any)
            if (dto.Tasks != null && dto.Tasks.Count > 0)
            {
                foreach (var taskDto in dto.Tasks)
                {
                    schedule.Tasks.Add(new PmScheduleTask
                    {
                        Month = taskDto.Month,
                        Week = taskDto.Week
                    });
                }
            }

            await _context.SaveChangesAsync();

            await _activityLogService.LogAsync("PmSchedule", schedule.Id, action, userId, $"{action} jadwal PM untuk site ID {dto.PmSiteId}");

            return true;
        }
    }
}
