using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.Notification;
using Pm.Helper;
using Pm.Services.Notification;
using System.Security.Claims;

namespace Pm.Controllers.Notification
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController(INotificationService notificationService) : ControllerBase
    {
        private readonly INotificationService _notificationService = notificationService;

        private int GetCurrentUserId()
        {
            // Cek custom claim "UserId" dulu, fallback ke ClaimTypes.NameIdentifier
            var userIdStr = User.FindFirstValue("UserId")
                         ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdStr, out var userId) ? userId : 0;
        }

        private string GetCurrentUserRole()
        {
            // Cek custom claim "RoleName" dulu, fallback ke ClaimTypes.Role
            return User.FindFirstValue("RoleName")
                ?? User.FindFirstValue(ClaimTypes.Role)
                ?? string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications([FromQuery] bool unreadOnly = false, [FromQuery] int take = 20)
        {
            var userId = GetCurrentUserId();
            var roleName = GetCurrentUserRole();
            var notifs = await _notificationService.GetMyNotificationsAsync(userId, roleName, unreadOnly, take);
            return ApiResponse.Success(notifs, "Success");
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var roleName = GetCurrentUserRole();
            var count = await _notificationService.GetUnreadCountAsync(userId, roleName);
            return ApiResponse.Success(new { count }, "Success");
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetCurrentUserId();
            var roleName = GetCurrentUserRole();
            await _notificationService.MarkAsReadAsync(id, userId, roleName);
            return ApiResponse.Success("Notification marked as read");
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            var roleName = GetCurrentUserRole();
            await _notificationService.MarkAllAsReadAsync(userId, roleName);
            return ApiResponse.Success("All notifications marked as read");
        }
    }
}
