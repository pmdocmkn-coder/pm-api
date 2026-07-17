using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
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
                _logger.LogInformation("📣 Sending notification '{Title}' to groups: {Groups}", dto.Title, string.Join(", ", targetGroups));
                await _hubContext.Clients.Groups(targetGroups).SendAsync("ReceiveNotification", notificationDto);
            }
        }

        public async Task CreateForRoleAsync(string roleName, CreateNotificationDto dto)
        {
            _logger.LogInformation("📢 CreateForRoleAsync — Role: '{RoleName}', Title: '{Title}'", roleName, dto.Title);
            dto.RecipientRoleName = roleName;
            dto.RecipientUserId = null;
            await CreateAsync(dto);
        }

        /// <summary>
        /// Kirim notifikasi ke semua user aktif yang memiliki permission tertentu.
        /// Notif disimpan per-user (bukan per-role) agar setiap orang bisa baca/hapus sendiri.
        /// Gunakan excludeUserIds untuk skip user yang sudah dapat notif personal agar tidak duplikat.
        /// </summary>
        public async Task CreateForPermissionAsync(string permissionName, CreateNotificationDto dto, IEnumerable<int>? excludeUserIds = null)
        {
            _logger.LogInformation("📢 CreateForPermissionAsync — Permission: '{Permission}', Title: '{Title}'", permissionName, dto.Title);

            var excludeSet = excludeUserIds?.ToHashSet() ?? new HashSet<int>();

            // Query via join: Permission → RolePermission → Role → User
            // Lebih reliable dari navigation property chaining di EF Core
            var userIds = await (
                from u in _context.Users
                join rp in _context.RolePermissions on u.RoleId equals rp.RoleId
                join p in _context.Permissions on rp.PermissionId equals p.PermissionId
                where u.IsActive && p.PermissionName == permissionName
                select u.UserId
            ).Distinct().ToListAsync();

            // Filter out user yang sudah dapat notif personal
            userIds = userIds.Where(id => !excludeSet.Contains(id)).ToList();

            if (!userIds.Any())
            {
                _logger.LogWarning("⚠️ CreateForPermissionAsync — Tidak ada user dengan permission '{Permission}' (setelah exclude). Total sebelum exclude: {Count}",
                    permissionName, userIds.Count);
                return;
            }

            _logger.LogInformation("📣 Sending '{Title}' to {Count} users with permission '{Permission}'",
                dto.Title, userIds.Count, permissionName);

            foreach (var userId in userIds)
            {
                var perUserDto = new CreateNotificationDto
                {
                    RecipientUserId = userId,
                    Title = dto.Title,
                    Message = dto.Message,
                    Category = dto.Category,
                    LinkUrl = dto.LinkUrl,
                    ReferenceId = dto.ReferenceId,
                    ReferenceType = dto.ReferenceType
                };
                await CreateAsync(perUserDto);
            }
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

        public async Task UpdateOrCreateAsync(CreateNotificationDto dto)
        {
            // Cari notif lama berdasarkan recipientUserId + referenceId + referenceType
            var existing = dto.RecipientUserId.HasValue && dto.ReferenceId.HasValue
                ? await _context.Notifications.FirstOrDefaultAsync(n =>
                    n.RecipientUserId == dto.RecipientUserId &&
                    n.ReferenceId == dto.ReferenceId &&
                    n.ReferenceType == dto.ReferenceType)
                : null;

            if (existing != null)
            {
                // Update pesan notif lama
                existing.Title = dto.Title;
                existing.Message = dto.Message;
                existing.IsRead = false;
                existing.ReadAt = null;
                existing.CreatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Push update via SignalR
                var notifDto = MapToDto(existing);
                if (dto.RecipientUserId.HasValue)
                    await _hubContext.Clients.Group($"User_{dto.RecipientUserId.Value}").SendAsync("ReceiveNotification", notifDto);
            }
            else
            {
                await CreateAsync(dto);
            }
        }
    }
}
