using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Pm.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink)
        {
            var smtpHost = _config["SmtpSettings:Host"]!;
            var smtpPort = int.Parse(_config["SmtpSettings:Port"]!);
            var smtpUser = _config["SmtpSettings:Username"]!;
            var smtpPass = _config["SmtpSettings:Password"]!;
            var fromName = _config["SmtpSettings:FromName"] ?? "PM Dashboard";
            var fromEmail = _config["SmtpSettings:FromEmail"] ?? smtpUser;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(fullName, toEmail));
            message.Subject = "Reset Password - PM Dashboard";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0;padding:0;background-color:#f1f5f9;font-family:Arial,sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f1f5f9;padding:40px 20px;'>
        <tr>
            <td align='center'>
                <table width='100%' style='max-width:480px;background-color:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07);'>
                    <!-- Header -->
                    <tr>
                        <td style='background:linear-gradient(135deg,#4f46e5,#7c3aed);padding:32px 24px;text-align:center;'>
                            <h1 style='color:#ffffff;margin:0;font-size:22px;font-weight:700;'>🔐 Reset Password</h1>
                            <p style='color:rgba(255,255,255,0.8);margin:8px 0 0;font-size:13px;'>PM Dashboard</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style='padding:32px 24px;'>
                            <p style='color:#334155;font-size:15px;margin:0 0 16px;'>Halo <strong>{fullName}</strong>,</p>
                            <p style='color:#64748b;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                                Kami menerima permintaan untuk mereset password akun Anda. Klik tombol di bawah untuk membuat password baru:
                            </p>
                            <!-- Button -->
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td align='center' style='padding:8px 0 24px;'>
                                        <a href='{resetLink}' 
                                           style='display:inline-block;background:linear-gradient(135deg,#4f46e5,#7c3aed);color:#ffffff;text-decoration:none;padding:14px 32px;border-radius:12px;font-size:14px;font-weight:700;letter-spacing:0.5px;'>
                                            Reset Password Saya
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            <p style='color:#94a3b8;font-size:12px;line-height:1.5;margin:0 0 16px;'>
                                ⏰ Link ini berlaku selama <strong>1 jam</strong>. Setelah itu, Anda perlu meminta link baru.
                            </p>
                            <p style='color:#94a3b8;font-size:12px;line-height:1.5;margin:0;'>
                                Jika Anda tidak meminta reset password, abaikan email ini. Akun Anda tetap aman.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='background-color:#f8fafc;padding:20px 24px;border-top:1px solid #e2e8f0;text-align:center;'>
                            <p style='color:#94a3b8;font-size:11px;margin:0;'>© {DateTime.Now.Year} PM Dashboard. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                _logger.LogInformation("📧 Connecting to SMTP: {Host}:{Port}", smtpHost, smtpPort);
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                _logger.LogInformation("✅ Password reset email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send email to {Email}", toEmail);
                throw;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        public async Task SendTemuanCreatedEmailAsync(int temuanId, string ruang, string temuan, string picEmail)
        {
            _logger.LogInformation("📧 SendTemuanCreatedEmailAsync - ID: {Id}, PIC: {Email}", temuanId, picEmail);
            // TODO: Implement temuan notification email
            await Task.CompletedTask;
        }
    }
}