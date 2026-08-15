using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.Models;

#pragma warning disable IDE0130
namespace Pm.Services;
#pragma warning restore IDE0130

public class NotificationSettingService(
    AppDbContext _context,
    IEmailService _emailService,
    ILogger<NotificationSettingService> _logger) : INotificationSettingService
{
    private const string EnabledKey = "HelpdeskEmailNotificationEnabled";
    private const string RecipientsKey = "HelpdeskNotificationEmails";

    public async Task<NotificationSettingDto> GetHelpdeskNotificationSettingAsync()
    {
        var enabledSetting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == EnabledKey);

        var recipientsSetting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == RecipientsKey);

        var isEnabled = string.Equals(enabledSetting?.SettingValue, "true", StringComparison.OrdinalIgnoreCase);
        
        List<string> emailList = [];
        if (!string.IsNullOrWhiteSpace(recipientsSetting?.SettingValue))
        {
            emailList = [.. recipientsSetting.SettingValue
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))];
        }

        return new NotificationSettingDto
        {
            HelpdeskEmailEnabled = isEnabled,
            HelpdeskEmailRecipients = emailList
        };
    }

    public async Task<NotificationSettingDto> UpdateHelpdeskNotificationSettingAsync(UpdateNotificationSettingDto dto)
    {
        // 1. Update Enabled status
        var enabledSetting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == EnabledKey);

        if (enabledSetting == null)
        {
            enabledSetting = new SystemSetting
            {
                SettingKey = EnabledKey,
                SettingValue = dto.HelpdeskEmailEnabled ? "true" : "false",
                Description = "Status aktifasi notifikasi email ke Helpdesk saat radio selesai diperbaiki dan masuk WH",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.SystemSettings.AddAsync(enabledSetting);
        }
        else
        {
            enabledSetting.SettingValue = dto.HelpdeskEmailEnabled ? "true" : "false";
            enabledSetting.UpdatedAt = DateTime.UtcNow;
        }

        // 2. Update Recipients
        var recipientEmails = dto.HelpdeskEmailRecipients != null
            ? string.Join(",", dto.HelpdeskEmailRecipients.Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e)))
            : string.Empty;

        var recipientsSetting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == RecipientsKey);

        if (recipientsSetting == null)
        {
            recipientsSetting = new SystemSetting
            {
                SettingKey = RecipientsKey,
                SettingValue = recipientEmails,
                Description = "Daftar email Helpdesk penerima notifikasi radio siap (dipisahkan koma)",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.SystemSettings.AddAsync(recipientsSetting);
        }
        else
        {
            recipientsSetting.SettingValue = recipientEmails;
            recipientsSetting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("✅ Notification settings updated. Enabled: {Enabled}, Recipients: {Recipients}",
            dto.HelpdeskEmailEnabled, recipientEmails);

        return await GetHelpdeskNotificationSettingAsync();
    }

    public async Task<bool> SendTestEmailAsync(string targetEmail)
    {
        if (string.IsNullOrWhiteSpace(targetEmail))
        {
            throw new ArgumentException("Email tujuan uji coba tidak boleh kosong.");
        }

        return await _emailService.SendTestNotificationEmailAsync(targetEmail.Trim());
    }
}
