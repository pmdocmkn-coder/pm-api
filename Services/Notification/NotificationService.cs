using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Notification;
using Pm.Hubs;
using Pm.Models;

namespace Pm.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateAsync(CreateNotificationDto dto)
        {
            var notif = new Models.Notification
            {
                RecipientUserId = dto.RecipientUserId,
                RecipientRoleName = dto.RecipientRoleName,
                Title = dto.Title,
                Message = dto.Message,
                Category = dto.Category,
                LinkUrl = dto.LinkUrl,
                ReferenceId = dto.ReferenceId,
                ReferenceType = dto.ReferenceType,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notif);
            await _context.SaveChangesAsync();

            // Push via SignalR
            var notificationDto = MapToDto(notif);
            var targetGroups = new List<string>();
            
            if (dto.RecipientUserId.HasValue)
            {
                targetGroups.Add($"User_{dto.RecipientUserId.Value}");
            }
            
            if (!string.IsNullOrEmpty(dto.RecipientRoleName))
            {
                targetGroups.Add($"Role_{dto.RecipientRoleName}");
            }

            if (targetGroups.Count > 0)
            {
                await _hubContext.Clients.Groups(targetGroups).SendAsync("ReceiveNotification", notificationDto);
            }
        }

        public async Task CreateForRoleAsync(string roleName, CreateNotificationDto dto)
        {
            dto.RecipientRoleName = roleName;
            dto.RecipientUserId = null;
            await CreateAsync(dto);
        }

        public async Task<List<NotificationDto>> GetMyNotificationsAsync(int userId, string roleName, bool unreadOnly = false, int take = 20)
        {
            var query = _context.Notifications.AsNoTracking()
                .Where(n => n.RecipientUserId == userId || n.RecipientRoleName == roleName);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();

            return notifications.Select(MapToDto).ToList();
        }

        public async Task<int> GetUnreadCountAsync(int userId, string roleName)
        {
            return await _context.Notifications.AsNoTracking()
                .CountAsync(n => (!n.IsRead) && (n.RecipientUserId == userId || n.RecipientRoleName == roleName));
        }

        public async Task MarkAsReadAsync(int notificationId, int userId, string roleName)
        {
            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId);
            if (notif == null) return;

            // Pastikan milik user atau role-nya
            if (notif.RecipientUserId == userId || notif.RecipientRoleName == roleName)
            {
                notif.IsRead = true;
                notif.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId, string roleName)
        {
            var unreadNotifs = await _context.Notifications
                .Where(n => (!n.IsRead) && (n.RecipientUserId == userId || n.RecipientRoleName == roleName))
                .ToListAsync();

            if (unreadNotifs.Any())
            {
                var now = DateTime.UtcNow;
                foreach (var n in unreadNotifs)
                {
                    n.IsRead = true;
                    n.ReadAt = now;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteOldNotificationsAsync(int daysOld = 30)
        {
            var threshold = DateTime.UtcNow.AddDays(-daysOld);
            var oldNotifs = await _context.Notifications
                .Where(n => n.CreatedAt < threshold)
                .ToListAsync();

            if (oldNotifs.Any())
            {
                _context.Notifications.RemoveRange(oldNotifs);
                await _context.SaveChangesAsync();
            }
        }

        private static NotificationDto MapToDto(Models.Notification n)
        {
            return new NotificationDto
            {
                Id = n.Id,
                RecipientUserId = n.RecipientUserId,
                RecipientRoleName = n.RecipientRoleName,
                Title = n.Title,
                Message = n.Message,
                Category = n.Category,
                LinkUrl = n.LinkUrl,
                ReferenceId = n.ReferenceId,
                ReferenceType = n.ReferenceType,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            };
        }

        public async Task BroadcastRefreshDataAsync(string entityName)
        {
            await _hubContext.Clients.All.SendAsync("RefreshData", entityName);
        }
    }
}
