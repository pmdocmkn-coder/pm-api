// Services/ActivityLogService.cs - VERSI SIMPLE TAPI WORK
using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.Models;
using Microsoft.Extensions.Logging;

namespace Pm.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(
            AppDbContext context,
            ILogger<ActivityLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(string module, int? entityId, string action, int userId, string description)
        {
            try
            {
                _logger.LogDebug("📝 ActivityLog: {Module}/{Action} by {UserId}", module, action, userId);

                // Validasi sederhana
                if (string.IsNullOrWhiteSpace(module)) module = "Unknown";
                if (string.IsNullOrWhiteSpace(action)) action = "Unknown";
                if (string.IsNullOrWhiteSpace(description)) description = "No description";

                // Trim
                module = module.Trim();
                action = action.Trim();
                description = description.Trim();

                // Batasi panjang
                if (module.Length > 100) module = module.Substring(0, 100);
                if (action.Length > 50) action = action.Substring(0, 50);
                if (description.Length > 1000) description = description.Substring(0, 1000);

                // Cek user - jika tidak ada, skip
                var userExists = await _context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.UserId == userId);
                
                if (!userExists)
                {
                    _logger.LogWarning("⚠️ User {UserId} tidak ditemukan, skip activity log", userId);
                    return;
                }

                // Buat log
                var log = new ActivityLog
                {
                    Module = module,
                    EntityId = entityId,
                    Action = action,
                    UserId = userId,
                    Description = description,
                    Timestamp = DateTime.UtcNow
                };

                // Simpan
                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogDebug("✅ ActivityLog saved: ID={Id}", log.Id);
            }
            catch (DbUpdateException dbEx)
            {
                // Log sebagai Warning, bukan Error
                _logger.LogWarning(dbEx, "⚠️ Database error in ActivityLog (non-critical)");
                
                if (dbEx.InnerException != null)
                {
                    var msg = dbEx.InnerException.Message;
                    _logger.LogWarning("🔍 FK constraint failed for UserId={UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in ActivityLogService");
                // Jangan throw - activity log failure tidak boleh crash app
            }
        }
    }
}