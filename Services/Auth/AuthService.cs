using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Auth;
using Pm.Helper;
using Pm.Models;
using Pm.Services.Notification;

namespace Pm.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IJwtService jwtService, IEmailService emailService, INotificationService notificationService, ILogger<AuthService> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto dto)
        {
            _logger.LogInformation("🔐 Login attempt for: {Username}", dto.Username);

            // Search by username OR email
            var input = dto.Username.Trim();
            var user = await _context.Users
                .AsTracking()
                .Include(u => u.Role)
                    .ThenInclude(r => r!.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => 
                    (u.Username == input || u.Email == input) && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("❌ Login failed: User {Input} not found or inactive", input);
                throw new UnauthorizedAccessException("USER_NOT_FOUND");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.LogWarning("❌ Login failed: Invalid password for user {Username}", user.Username);
                throw new UnauthorizedAccessException("WRONG_PASSWORD");
            }

            // Get user permissions (untuk response body saja, BUKAN untuk token)
            var permissions = user.Role?.RolePermissions
                .Select(rp => rp.Permission.PermissionName)
                .ToList() ?? new List<string>();

            // Generate JWT token (lean — tanpa permissions)
            var token = _jwtService.GenerateToken(user);
            var expiresIn = _jwtService.GetTokenExpirationTime();

            var trueUtcTime = DateTimeOffset.UtcNow.DateTime;

            _logger.LogInformation("🕐 Server Local Time: {Local}", DateTime.Now);
            _logger.LogInformation("🕐 TRUE UTC Time: {Utc}", trueUtcTime);
            _logger.LogInformation("🕐 Server Timezone: {Tz}", TimeZoneInfo.Local.Id);

            user.LastLogin = trueUtcTime;
            user.UpdatedAt = trueUtcTime;

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                var rowsAffected = await _context.SaveChangesAsync();
                _logger.LogInformation("✅ LastLogin saved to DB - UTC: {LastLogin} (Rows: {Rows})",
                    user.LastLogin, rowsAffected);

                // Broadcast agar User Management page auto-refresh
                await _notificationService.BroadcastRefreshDataAsync("User");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to update LastLogin");
            }

            return new LoginResponseDto
            {
                Token = token,
                ExpiresIn = expiresIn,
                User = new DTOs.UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhotoUrl = user.PhotoUrl,
                    EmployeeId = user.EmployeeId,
                    Division = user.Division,
                    IsActive = user.IsActive,
                    RoleId = user.RoleId,
                    RoleName = user.Role?.RoleName,
                    LastLogin = user.LastLogin,
                    CreatedAt = user.CreatedAt
                },
                Permissions = permissions
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            _logger.LogInformation("🔐 Change password attempt for user {UserId}", userId);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("❌ Change password failed: User {UserId} not found", userId);
                return false;
            }

            var isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning("❌ Change password failed: Invalid current password for user {UserId}", userId);
                return false;
            }

            if (!IsStrongPassword(dto.NewPassword))
            {
                throw new Exception("Password baru harus minimal 8 karakter dan mengandung huruf besar, huruf kecil, angka, dan simbol.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Password changed successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Database error while changing password for user {UserId}", userId);
                throw new Exception("Gagal menyimpan password baru ke database.");
            }
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        // =============================================
        // FORGOT PASSWORD
        // =============================================
        public async Task ForgotPasswordAsync(ForgotPasswordDto dto, string resetBaseUrl)
        {
            _logger.LogInformation("🔑 Forgot password request for email: {Email}", dto.Email);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("⚠️ Forgot password: email {Email} not found or user inactive", dto.Email);
                return; // Don't reveal if email exists
            }

            // Invalidate old unused tokens
            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.UserId && !t.IsUsed)
                .ToListAsync();
            foreach (var old in oldTokens) old.IsUsed = true;

            // Create new token
            var token = Guid.NewGuid().ToString("N");
            var resetToken = new PasswordResetToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // Send email
            var resetLink = $"{resetBaseUrl}/reset-password?token={token}";
            await _emailService.SendPasswordResetEmailAsync(user.Email!, user.FullName, resetLink);

            _logger.LogInformation("✅ Password reset email sent to {Email} for user {UserId}", dto.Email, user.UserId);
        }

        // =============================================
        // RESET PASSWORD
        // =============================================
        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            _logger.LogInformation("🔑 Reset password attempt with token");

            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == dto.Token && !t.IsUsed);

            if (resetToken == null)
                throw new Exception("Link reset tidak valid atau sudah pernah digunakan.");

            if (resetToken.ExpiresAt < DateTime.UtcNow)
                throw new Exception("Link reset sudah kedaluwarsa. Silakan minta link baru.");

            if (!IsStrongPassword(dto.NewPassword))
                throw new Exception("Password baru harus minimal 8 karakter dan mengandung huruf besar, huruf kecil, angka, dan simbol.");

            var user = resetToken.User!;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            resetToken.IsUsed = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Password reset successful for user {UserId}", user.UserId);
        }

        private static bool IsStrongPassword(string password)
        {
            return password.Length >= 8
                && password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }
}