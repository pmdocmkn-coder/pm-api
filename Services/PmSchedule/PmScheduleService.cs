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
                    .ThenInclude(t => t.CompletedByUser)
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
                            Id = t.Id,
                            Month = t.Month,
                            Week = t.Week,
                            IsCompleted = t.IsCompleted,
                            CompletedAt = t.CompletedAt,
                            CompletedByUserId = t.CompletedByUserId,
                            CompletedByUserName = t.CompletedByUser?.FullName,
                            Remarks = t.Remarks
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
                // Merge tasks to preserve completion status
                if (dto.Tasks == null) dto.Tasks = new List<PmScheduleTaskDto>();
                var tasksToAdd = dto.Tasks.Where(dt => !schedule.Tasks.Any(st => st.Month == dt.Month && st.Week == dt.Week)).ToList();
                var tasksToRemove = schedule.Tasks.Where(st => !dto.Tasks.Any(dt => dt.Month == st.Month && dt.Week == st.Week)).ToList();

                _context.PmScheduleTasks.RemoveRange(tasksToRemove);
                foreach (var t in tasksToRemove) schedule.Tasks.Remove(t);

                foreach (var dt in tasksToAdd)
                {
                    schedule.Tasks.Add(new PmScheduleTask
                    {
                        Month = dt.Month,
                        Week = dt.Week
                    });
                }
            }

            if (action == "Create" && dto.Tasks != null && dto.Tasks.Count > 0)
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

        public async Task<bool> DeleteScheduleAsync(int year, int pmSiteId, string deviceName, int userId)
        {
            var schedule = await _context.PmSchedules
                .Include(s => s.Tasks)
                .FirstOrDefaultAsync(s => s.Year == year 
                                          && s.PmSiteId == pmSiteId 
                                          && s.DeviceName == deviceName);

            if (schedule != null)
            {
                _context.PmScheduleTasks.RemoveRange(schedule.Tasks);
                _context.PmSchedules.Remove(schedule);
                await _context.SaveChangesAsync();

                await _activityLogService.LogAsync(
                    "PM Schedule",
                    null,
                    "Delete",
                    userId,
                    $"Menghapus jadwal PM {deviceName} pada site ID {pmSiteId} tahun {year}"
                );

                return true;
            }
            return false;
        }

        public async Task<bool> ToggleTaskCompletionAsync(int taskId, string? remarks, System.DateTime? completedAt, int userId)
        {
            var task = await _context.PmScheduleTasks
                .Include(t => t.PmSchedule)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return false;

            task.IsCompleted = !task.IsCompleted;
            
            if (task.IsCompleted)
            {
                task.CompletedAt = completedAt ?? System.DateTime.UtcNow;
                task.CompletedByUserId = userId;
                task.Remarks = remarks;
            }
            else
            {
                task.CompletedAt = null;
                task.CompletedByUserId = null;
                task.Remarks = null;
            }

            await _context.SaveChangesAsync();

            string status = task.IsCompleted ? "Selesai" : "Belum Selesai";
            await _activityLogService.LogAsync(
                "PM Schedule Task",
                task.Id,
                "ToggleCompletion",
                userId,
                $"Mengubah status PM {task.PmSchedule.DeviceName} menjadi {status}"
            );

            return true;
        }

        public async Task<PmComplianceDashboardDto> GetComplianceDashboardAsync(int year)
        {
            var currentDate = System.DateTime.UtcNow.AddHours(7); // WIB
            var currentMonth = currentDate.Month;
            var currentYear = currentDate.Year;
            var currentWeek = (int)Math.Ceiling(currentDate.Day / 7.0);
            
            var sixMonthsAgo = currentDate.AddMonths(-5);

            var schedules = await _context.PmSchedules
                .Include(s => s.Tasks)
                .Where(s => s.Year == year || s.Year == sixMonthsAgo.Year)
                .ToListAsync();

            var allTasksYear = schedules.Where(s => s.Year == year).SelectMany(s => s.Tasks).ToList();
            var totalScheduled = allTasksYear.Count;
            var totalCompleted = allTasksYear.Count(t => t.IsCompleted);

            var totalOverdue = allTasksYear.Count(t => 
                !t.IsCompleted && (year < currentYear || (year == currentYear && t.Month < currentMonth) || (year == currentYear && t.Month == currentMonth && t.Week < currentWeek))
            );

            var dashboard = new PmComplianceDashboardDto
            {
                TotalScheduled = totalScheduled,
                TotalCompleted = totalCompleted,
                TotalOverdue = totalOverdue,
                CompliancePercentage = totalScheduled > 0 ? (double)totalCompleted / totalScheduled * 100 : 0
            };

            // Current Month
            var currentMonthTasks = schedules.Where(s => s.Year == currentYear).SelectMany(s => s.Tasks).Where(t => t.Month == currentMonth).ToList();
            dashboard.CurrentMonth = new PmCurrentMonthDto
            {
                TotalScheduled = currentMonthTasks.Count,
                Completed = currentMonthTasks.Count(t => t.IsCompleted),
                Overdue = currentMonthTasks.Count(t => !t.IsCompleted && t.Week < currentWeek),
                ProgressPercentage = currentMonthTasks.Count > 0 ? (double)currentMonthTasks.Count(t => t.IsCompleted) / currentMonthTasks.Count * 100 : 0
            };

            // Trend 6 Months
            var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "Mei", "Jun", "Jul", "Agt", "Sep", "Okt", "Nov", "Des" };
            
            var allTasks = schedules.SelectMany(s => s.Tasks.Select(t => new { s.Year, Task = t })).ToList();

            for (int i = 5; i >= 0; i--)
            {
                var targetDate = currentDate.AddMonths(-i);
                var m = targetDate.Month;
                var y = targetDate.Year;

                var tasksInMonth = allTasks.Where(t => t.Task.Month == m && t.Year == y).Select(t => t.Task).ToList();
                
                var completed = tasksInMonth.Count(t => t.IsCompleted);
                var scheduled = tasksInMonth.Count;
                var overdue = tasksInMonth.Count(t => !t.IsCompleted && (y < currentYear || (y == currentYear && m < currentMonth) || (y == currentYear && m == currentMonth && t.Week < currentWeek)));

                dashboard.Trend6Months.Add(new PmTrendDto
                {
                    MonthName = monthNames[m - 1],
                    Year = y,
                    Month = m,
                    Completed = completed,
                    Overdue = overdue,
                    CompliancePercentage = scheduled > 0 ? (double)completed / scheduled * 100 : 0
                });
            }

            return dashboard;
        }
    }
}
