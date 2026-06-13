using Pm.DTOs.Notification;

namespace Pm.Services.Notification
{
    public interface INotificationService
    {
        Task CreateAsync(CreateNotificationDto dto);
        Task CreateForRoleAsync(string roleName, CreateNotificationDto dto);

        /// <summary>
        /// Kirim notifikasi ke semua user yang memiliki permission tertentu.
        /// Gunakan excludeUserIds untuk skip user yang sudah dapat notif personal agar tidak duplikat.
        /// </summary>
        Task CreateForPermissionAsync(string permissionName, CreateNotificationDto dto, IEnumerable<int>? excludeUserIds = null);

        Task<List<NotificationDto>> GetMyNotificationsAsync(int userId, string roleName, bool unreadOnly = false, int take = 20);
        Task<int> GetUnreadCountAsync(int userId, string roleName);
        Task MarkAsReadAsync(int notificationId, int userId, string roleName);
        Task MarkAllAsReadAsync(int userId, string roleName);
        Task DeleteOldNotificationsAsync(int daysOld = 30);
        Task BroadcastRefreshDataAsync(string entityName);
    }
}
