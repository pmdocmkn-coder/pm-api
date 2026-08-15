using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Helper;
using Pm.Services;

#pragma warning disable IDE0130
namespace Pm.Controllers;
#pragma warning restore IDE0130

[ApiController]
[Route("api/notification-settings")]
[Authorize]
public class NotificationSettingController(
    INotificationSettingService _settingService) : ControllerBase
{
    [HttpGet("helpdesk")]
    [Authorize(Policy = "CanViewNotificationSettings")]
    public async Task<IActionResult> GetHelpdeskSetting()
    {
        var data = await _settingService.GetHelpdeskNotificationSettingAsync();
        return ApiResponse.Success(data, "Berhasil mengambil pengaturan notifikasi email helpdesk");
    }

    [HttpPut("helpdesk")]
    [Authorize(Policy = "CanViewNotificationSettings")]
    public async Task<IActionResult> UpdateHelpdeskSetting([FromBody] UpdateNotificationSettingDto dto)
    {
        var data = await _settingService.UpdateHelpdeskNotificationSettingAsync(dto);
        return ApiResponse.Success(data, "Pengaturan notifikasi email helpdesk berhasil diperbarui");
    }

    [HttpPost("test-email")]
    [Authorize(Policy = "CanViewNotificationSettings")]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailDto dto)
    {
        var result = await _settingService.SendTestEmailAsync(dto.TargetEmail);
        if (result)
        {
            return ApiResponse.Success(new { Sent = true }, "Email uji coba berhasil dikirim.");
        }
        return ApiResponse.BadRequest("Test Email", "Gagal mengirim email uji coba. Periksa konfigurasi SMTP.");
    }
}
