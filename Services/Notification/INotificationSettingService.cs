using Pm.DTOs;

#pragma warning disable IDE0130
namespace Pm.Services;
#pragma warning restore IDE0130

public interface INotificationSettingService
{
    Task<NotificationSettingDto> GetHelpdeskNotificationSettingAsync();
    Task<NotificationSettingDto> UpdateHelpdeskNotificationSettingAsync(UpdateNotificationSettingDto dto);
    Task<bool> SendTestEmailAsync(string targetEmail);
}
