#pragma warning disable IDE0130
namespace Pm.DTOs;
#pragma warning restore IDE0130

public class NotificationSettingDto
{
    public bool HelpdeskEmailEnabled { get; set; }
    public List<string> HelpdeskEmailRecipients { get; set; } = [];
}

public class UpdateNotificationSettingDto
{
    public bool HelpdeskEmailEnabled { get; set; }
    public List<string> HelpdeskEmailRecipients { get; set; } = [];
}

public class TestEmailDto
{
    public string TargetEmail { get; set; } = null!;
}
