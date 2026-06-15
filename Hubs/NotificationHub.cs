using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Pm.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                // Cek custom claim "UserId" dulu, fallback ke ClaimTypes.NameIdentifier
                var userId = user.FindFirst("UserId")?.Value
                          ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                }

                // Cek custom claim "RoleName" dulu, fallback ke ClaimTypes.Role
                var roleName = user.FindFirst("RoleName")?.Value
                            ?? user.FindFirst(ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(roleName))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{roleName}");
                }

                _logger.LogInformation(
                    "🔌 SignalR Connected — User: {UserId}, Role: '{RoleName}', ConnId: {ConnId}",
                    userId ?? "null", roleName ?? "null", Context.ConnectionId);
            }
            else
            {
                _logger.LogWarning("⚠️ SignalR Connected tapi tidak authenticated. ConnId: {ConnId}", Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("🔌 SignalR Disconnected — ConnId: {ConnId}", Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
