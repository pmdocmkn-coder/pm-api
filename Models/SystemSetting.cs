#pragma warning disable IDE0130
namespace Pm.Models;
#pragma warning restore IDE0130

public class SystemSetting
{
    public int Id { get; set; }
    public string SettingKey { get; set; } = null!;
    public string? SettingValue { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
