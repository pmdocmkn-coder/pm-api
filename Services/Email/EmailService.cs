using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Pm.Helper;

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

        private async Task<bool> SendEmailInternalAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpHost = _config["SmtpSettings:Host"]!;
            var smtpPort = int.Parse(_config["SmtpSettings:Port"]!);
            var smtpUser = _config["SmtpSettings:Username"]!;
            var smtpPass = _config["SmtpSettings:Password"]!;
            var fromName = _config["SmtpSettings:FromName"] ?? "PM Dashboard";
            var fromEmail = _config["SmtpSettings:FromEmail"] ?? smtpUser;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            var emails = toEmail.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var email in emails)
            {
                var trimmedEmail = email.Trim();
                if (!string.IsNullOrEmpty(trimmedEmail))
                {
                    message.To.Add(new MailboxAddress(trimmedEmail, trimmedEmail));
                }
            }
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                _logger.LogInformation("📧 Connecting to SMTP: {Host}:{Port}", smtpHost, smtpPort);
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(message);
                _logger.LogInformation("✅ Email sent to {Email} with subject {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send email to {Email}", toEmail);
                return false;
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        private string GetBaseHtmlTemplate(string title, string content, string? callToAction = null, string? ctaLink = null)
        {
            string ctaSection = string.Empty;
            if (!string.IsNullOrEmpty(callToAction) && !string.IsNullOrEmpty(ctaLink))
            {
                ctaSection = $@"
                    <table width='100%' cellpadding='0' cellspacing='0'>
                        <tr>
                            <td align='center' style='padding:24px 0;'>
                                <a href='{ctaLink}' 
                                   style='display:inline-block;background-color:#1B3A6B;background:linear-gradient(135deg,#1B3A6B,#2B6CB0);color:#ffffff;text-decoration:none;padding:14px 32px;border-radius:12px;font-size:14px;font-weight:700;letter-spacing:0.5px;'>
                                    {callToAction}
                                </a>
                            </td>
                        </tr>
                    </table>";
            }

            return $@"
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
                <table width='100%' style='max-width:550px;background-color:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 6px rgba(0,0,0,0.07);'>
                    <!-- Header -->
                    <tr>
                        <td style='background-color:#1B3A6B;background:linear-gradient(135deg,#1B3A6B,#2B6CB0);padding:32px 24px;text-align:center;'>
                            <h1 style='color:#ffffff;margin:0;font-size:22px;font-weight:700;'>{title}</h1>
                            <p style='color:rgba(255,255,255,0.8);margin:8px 0 0;font-size:13px;'>PM Dashboard</p>
                        </td>
                    </tr>
                    <!-- Body -->
                    <tr>
                        <td style='padding:32px 24px;'>
                            {content}
                            {ctaSection}
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
</html>";
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string resetLink)
        {
            var content = $@"
                <p style='color:#334155;font-size:15px;margin:0 0 16px;'>Halo <strong>{fullName}</strong>,</p>
                <p style='color:#64748b;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Kami menerima permintaan untuk mereset password akun Anda. Klik tombol di bawah untuk membuat password baru:
                </p>
                <p style='color:#94a3b8;font-size:12px;line-height:1.5;margin:0 0 16px;'>
                    ⏰ Link ini berlaku selama <strong>1 jam</strong>. Setelah itu, Anda perlu meminta link baru.
                </p>
                <p style='color:#94a3b8;font-size:12px;line-height:1.5;margin:0;'>
                    Jika Anda tidak meminta reset password, abaikan email ini. Akun Anda tetap aman.
                </p>";
            
            var html = GetBaseHtmlTemplate("🔐 Reset Password", content, "Reset Password Saya", resetLink);
            await SendEmailInternalAsync(toEmail, "Reset Password - PM Dashboard", html);
        }

        public async Task SendTemuanCreatedEmailAsync(int temuanId, string ruang, string temuan, string picEmail)
        {
            _logger.LogInformation("📧 SendTemuanCreatedEmailAsync - ID: {Id}, PIC: {Email}", temuanId, picEmail);
            await Task.CompletedTask;
        }

        public async Task<bool> SendDocumentExpiryEmailAsync(string toEmail, string documentName, int daysRemaining, DateTime validUntil, string? fileLink, string documentId, string? documentType = null, string? groupName = null)
        {
            string color = daysRemaining <= 7 ? "#dc2626" : (daysRemaining <= 14 ? "#f59e0b" : "#2b6cb0");
            string statusText = daysRemaining == 0 ? "Hari ini" : (daysRemaining < 0 ? "Telah Berakhir" : $"H-{daysRemaining}");
            string bgColor = daysRemaining <= 7 ? "#fef2f2" : (daysRemaining <= 14 ? "#fffbeb" : "#eff6ff");
            string borderColor = daysRemaining <= 7 ? "#fca5a5" : (daysRemaining <= 14 ? "#fcd34d" : "#bfdbfe");
            string icon = daysRemaining <= 7 ? "🚨" : (daysRemaining <= 14 ? "⚠️" : "ℹ️");

            string typeHtml = !string.IsNullOrEmpty(documentType) 
                ? $@"<tr><td style='padding:10px 0;border-bottom:1px solid #e2e8f0;color:#64748b;font-size:13px;' width='130'>Tipe Dokumen</td>
                         <td style='padding:10px 0;border-bottom:1px solid #e2e8f0;color:#1e293b;font-size:14px;font-weight:600;'>{documentType}</td></tr>" 
                : "";

            string groupHtml = !string.IsNullOrEmpty(groupName)
                ? $@"<tr><td style='padding:10px 0;border-bottom:1px solid #e2e8f0;color:#64748b;font-size:13px;' width='130'>Grup</td>
                         <td style='padding:10px 0;border-bottom:1px solid #e2e8f0;color:#1e293b;font-size:14px;font-weight:600;'>{groupName}</td></tr>" 
                : "";

            var content = $@"
                <p style='color:#334155;font-size:15px;margin:0 0 16px;'>Yth. Pihak Terkait,</p>
                <p style='color:#475569;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Berikut adalah informasi mengenai dokumen operasional yang <strong>mendekati atau telah melewati masa berlaku</strong>. 
                    Mohon untuk segera menindaklanjuti dokumen di bawah ini:
                </p>

                <div style='background-color:{bgColor};border:1px solid {borderColor};border-radius:8px;padding:24px;margin-bottom:32px;box-shadow:0 1px 3px rgba(0,0,0,0.05);'>
                    <div style='margin-bottom:20px;display:table;width:100%;'>
                        <div style='display:table-cell;vertical-align:middle;width:32px;font-size:24px;'>{icon}</div>
                        <div style='display:table-cell;vertical-align:middle;'>
                            <h3 style='margin:0;color:#0f172a;font-size:17px;line-height:1.4;'>{documentName}</h3>
                        </div>
                    </div>
                    
                    <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;'>
                        {typeHtml}
                        {groupHtml}
                        <tr>
                            <td style='padding:10px 0;border-bottom:1px solid #e2e8f0;color:#64748b;font-size:13px;' width='130'>Batas Waktu</td>
                            <td style='padding:10px 0;border-bottom:1px solid #e2e8f0;color:#1e293b;font-size:14px;font-weight:600;'>{validUntil:dd MMMM yyyy}</td>
                        </tr>
                        <tr>
                            <td style='padding:10px 0;color:#64748b;font-size:13px;'>Sisa Waktu</td>
                            <td style='padding:10px 0;color:{color};font-size:14px;font-weight:bold;'>{statusText}</td>
                        </tr>
                    </table>
                </div>

                <p style='color:#475569;font-size:13px;line-height:1.6;margin:0;background-color:#f8fafc;padding:12px 16px;border-left:4px solid #94a3b8;border-radius:4px;'>
                    <strong style='color:#334155;'>💡 Tindakan yang diperlukan:</strong><br/>
                    Harap segera melakukan perpanjangan atau penyelesaian kewajiban yang berkaitan dengan dokumen ini untuk menghindari kendala operasional.
                </p>";

            return await SendEmailInternalAsync(toEmail, $"{icon} Peringatan Kadaluarsa: {documentName}", GetBaseHtmlTemplate("Pemberitahuan Jatuh Tempo Dokumen", content, fileLink != null ? "Lihat Dokumen" : null, fileLink));
        }

        public async Task<bool> SendGroupedDocumentExpiryEmailAsync(string toEmail, string groupName, int daysRemaining, DateTime validUntil, IEnumerable<(string DocumentName, DateTime ValidUntil)> documents)
        {
            string color = daysRemaining <= 7 ? "#dc2626" : (daysRemaining <= 14 ? "#f59e0b" : "#2b6cb0");
            string status = daysRemaining == 0 ? "Hari ini" : (daysRemaining < 0 ? "Telah Berakhir" : $"H-{daysRemaining}");
            
            var docsHtml = string.Join("", documents.Select(d => 
                $"<tr><td style='padding:8px 0;border-bottom:1px solid #e2e8f0;color:#1e293b;font-size:14px;'>{d.DocumentName}</td><td style='padding:8px 0;border-bottom:1px solid #e2e8f0;text-align:right;color:#475569;font-size:14px;'>{d.ValidUntil:dd MMM yyyy}</td></tr>"
            ));

            var content = $@"
                <p style='color:#334155;font-size:15px;margin:0 0 16px;'>Yth. Pihak Terkait,</p>
                <p style='color:#64748b;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Terdapat kelompok dokumen operasional (<strong>{groupName}</strong>) yang mendekati masa kadaluarsa. Mohon untuk segera menindaklanjuti dokumen-dokumen berikut:
                </p>
                <div style='background-color:#f8fafc;border-top:4px solid {color};padding:16px;border-radius:4px;margin-bottom:24px;'>
                    <p style='margin:0 0 16px;color:{color};font-size:14px;font-weight:bold;'><strong>Status Peringatan:</strong> {status}</p>
                    <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;'>
                        {docsHtml}
                    </table>
                </div>
                <p style='color:#64748b;font-size:14px;line-height:1.6;margin:0;'>
                    Harap segera melakukan perpanjangan dokumen untuk menghindari kendala operasional perusahaan.
                </p>";

            return await SendEmailInternalAsync(toEmail, $"Peringatan Grup ({status}): {groupName}", GetBaseHtmlTemplate("⚠️ Peringatan Kadaluarsa Grup", content));
        }

        public async Task<bool> SendDocumentAnniversaryEmailAsync(string toEmail, string documentName, int daysRemaining, DateTime validUntil, string? fileLink, string documentId, string documentType)
        {
            string jatuhTempoText = daysRemaining < 0
                ? $"Telah melewati siklus sejak <strong>{Math.Abs(daysRemaining)} hari yang lalu</strong>."
                : daysRemaining == 0 
                    ? "<strong>Memasuki siklus tahunan HARI INI.</strong>" 
                    : $"Akan memasuki siklus tahunan dalam <strong>{daysRemaining} hari</strong>.";

            var content = $@"
                <p style='color:#334155;font-size:15px;margin:0 0 16px;'>Yth. Pihak Terkait,</p>
                <p style='color:#64748b;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Dokumen <strong>{documentName}</strong> ({documentType}) membutuhkan perhatian Anda.
                    <br/><br/>
                    Status: <span style='color:{(daysRemaining <= 0 ? "#e11d48" : "#2b6cb0")}'>{jatuhTempoText}</span>
                </p>
                <div style='background-color:#f8fafc;border-left:4px solid #2b6cb0;padding:16px;border-radius:4px;margin-bottom:24px;'>
                    <p style='margin:0;color:#475569;font-size:14px;'>Mohon melakukan pengecekan atau evaluasi dokumen sesuai dengan prosedur perusahaan agar operasional tetap berjalan dengan baik.</p>
                </div>";

            return await SendEmailInternalAsync(toEmail, $"Peringatan Siklus Tahunan: {documentName}", GetBaseHtmlTemplate("📅 Peringatan Siklus Tahunan", content, fileLink != null ? "Lihat Dokumen" : null, fileLink));
        }

        public async Task<bool> SendGroupedDocumentAnniversaryEmailAsync(string toEmail, string groupName, int daysRemaining, DateTime validUntil, IEnumerable<(string DocumentName, string DocumentType)> documents)
        {
            var docsHtml = string.Join("", documents.Select(d => 
                $"<li><strong style='color:#1e293b;'>{d.DocumentName}</strong> <span style='color:#64748b;'>({d.DocumentType})</span></li>"
            ));

            string jatuhTempoText = daysRemaining < 0
                ? $"Telah melewati siklus sejak <strong>{Math.Abs(daysRemaining)} hari yang lalu</strong>."
                : daysRemaining == 0 
                    ? "<strong>Memasuki siklus tahunan HARI INI.</strong>" 
                    : $"Akan memasuki siklus tahunan dalam <strong>{daysRemaining} hari</strong>.";

            var content = $@"
                <p style='color:#334155;font-size:15px;margin:0 0 16px;'>Yth. Pihak Terkait,</p>
                <p style='color:#64748b;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Grup dokumen <strong>{groupName}</strong> membutuhkan perhatian Anda.
                    <br/><br/>
                    Status: <span style='color:{(daysRemaining <= 0 ? "#e11d48" : "#2b6cb0")}'>{jatuhTempoText}</span>
                </p>
                <div style='background-color:#f8fafc;border-left:4px solid #2b6cb0;padding:16px;border-radius:4px;margin-bottom:24px;'>
                    <p style='margin:0 0 12px;color:#475569;font-size:14px;'>Daftar Dokumen dalam Grup:</p>
                    <ul style='margin:0;padding-left:20px;font-size:14px;line-height:1.6;'>
                        {docsHtml}
                    </ul>
                </div>";

            return await SendEmailInternalAsync(toEmail, $"Peringatan Siklus Tahunan Grup: {groupName}", GetBaseHtmlTemplate("📅 Peringatan Siklus Tahunan Grup", content));
        }

        public async Task<bool> SendBhpPaymentReminderEmailAsync(string toEmail, string documentName, int daysToAnniv, int currentYear, IEnumerable<(int Year, bool IsPaid, string? InvoiceNumber)> bhpItems)
        {
            var unpaidItems = bhpItems.Where(x => !x.IsPaid).Select(x => x.Year.ToString()).ToList();
            var unpaidYears = string.Join(", ", unpaidItems);

            string jatuhTempoText = daysToAnniv < 0
                ? $"Telah jatuh tempo sejak <strong>{Math.Abs(daysToAnniv)} hari yang lalu</strong>."
                : daysToAnniv == 0 
                    ? "<strong>Jatuh tempo HARI INI.</strong>" 
                    : $"Akan jatuh tempo dalam <strong>{daysToAnniv} hari</strong>.";

            var content = $@"
                <p style='color:#334155;font-size:15px;margin:0 0 16px;'>Yth. Pihak Terkait,</p>
                <p style='color:#64748b;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Kami ingin mengingatkan bahwa tagihan Biaya Hak Penggunaan (BHP) Frekuensi Radio untuk dokumen <strong>{documentName}</strong> membutuhkan atensi Anda.
                    <br/><br/>
                    Status: <span style='color:{(daysToAnniv <= 0 ? "#e11d48" : "#d97706")}'>{jatuhTempoText}</span>
                </p>
                <div style='background-color:#fff1f2;border-left:4px solid #e11d48;padding:16px;border-radius:4px;margin-bottom:24px;'>
                    <h3 style='margin:0 0 8px;color:#be123c;font-size:16px;'>Tagihan Tahun {currentYear}</h3>
                    <p style='margin:0;color:#881337;font-size:14px;'>Tunggakan lain: <strong>{unpaidYears}</strong></p>
                </div>
                <p style='color:#64748b;font-size:14px;line-height:1.6;margin:0;'>
                    Harap segera melakukan pelunasan sebelum batas waktu agar operasional tidak terganggu dan terhindar dari denda atau pencabutan izin.
                </p>";

            return await SendEmailInternalAsync(toEmail, $"[URGENT] Tagihan BHP ISR: {documentName}", GetBaseHtmlTemplate("💰 Tagihan BHP Frekuensi Radio", content));
        }

        public async Task<bool> SendGroupedBhpPaymentReminderEmailAsync(string toEmail, string groupName, int daysToAnniv, int currentYear, IEnumerable<(string DocName, int UnpaidCount, IEnumerable<int> UnpaidYears)> groupItems)
        {
            var docsHtml = string.Join("", groupItems.Select(d => 
                $"<tr><td style='padding:8px 0;border-bottom:1px solid #e2e8f0;color:#1e293b;font-size:14px;'>{d.DocName}</td><td style='padding:8px 0;border-bottom:1px solid #e2e8f0;text-align:right;'><div style='color:#e11d48;font-size:14px;font-weight:bold;'>Tagihan Tahun: {currentYear}</div><div style='color:#64748b;font-size:12px;margin-top:2px;'>Tunggakan lain: {string.Join(", ", d.UnpaidYears)}</div></td></tr>"
            ));

            string jatuhTempoText = daysToAnniv < 0
                ? $"Telah jatuh tempo sejak <strong>{Math.Abs(daysToAnniv)} hari yang lalu</strong>."
                : daysToAnniv == 0 
                    ? "<strong>Jatuh tempo HARI INI.</strong>" 
                    : $"Akan jatuh tempo dalam <strong>{daysToAnniv} hari</strong>.";

            var content = $@"
                <p style='color:#334155;font-size:15px;margin:0 0 16px;'>Yth. Pihak Terkait,</p>
                <p style='color:#64748b;font-size:14px;line-height:1.6;margin:0 0 24px;'>
                    Kami ingin mengingatkan bahwa terdapat tagihan Biaya Hak Penggunaan (BHP) Frekuensi Radio untuk Grup <strong>{groupName}</strong> yang membutuhkan atensi Anda.
                    <br/><br/>
                    Status: <span style='color:{(daysToAnniv <= 0 ? "#e11d48" : "#d97706")}'>{jatuhTempoText}</span>
                </p>
                <div style='background-color:#fff1f2;border-top:4px solid #e11d48;padding:16px;border-radius:4px;margin-bottom:24px;'>
                    <h3 style='margin:0 0 12px;color:#be123c;font-size:14px;'>Rincian Tunggakan Tagihan:</h3>
                    <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;'>
                        {docsHtml}
                    </table>
                </div>
                <p style='color:#64748b;font-size:14px;line-height:1.6;margin:0;'>
                    Mohon atensi segera untuk melakukan pelunasan tagihan BHP tersebut demi kelancaran operasional dan menghindari sanksi administratif.
                </p>";

            return await SendEmailInternalAsync(toEmail, $"[URGENT] Tagihan BHP Grup ISR: {groupName}", GetBaseHtmlTemplate("💰 Tagihan BHP Frekuensi Radio Grup", content));
        }

        public async Task<bool> SendRadioReadyForHelpdeskEmailAsync(
            string toEmail,
            string ticketNumber,
            string radioSerial,
            string equipmentName,
            string? unitNumber,
            string technicianName,
            string? notes,
            DateTime handoverAt,
            string webAppBaseUrl,
            bool isFromHelpdesk = false)
        {
            var subject = isFromHelpdesk 
                ? $"[Radio Scrap] Tiket {ticketNumber} - SN: {radioSerial} Diserahkan ke Warehouse"
                : $"[Radio Ready] Tiket {ticketNumber} - SN: {radioSerial} Masuk Warehouse";
            var formattedDate = WitaHelper.Format(handoverAt);
            var targetLink = $"{webAppBaseUrl.TrimEnd('/')}/radio-handover/warehouse";

            var introText = isFromHelpdesk
                ? "Radio berikut telah <strong>diserahkan ke Warehouse oleh Helpdesk</strong> (misal: untuk proses scrap atau lainnya)."
                : "Radio berikut telah selesai diperbaiki oleh teknisi workshop dan telah <strong>diserahkan ke Warehouse</strong>. Radio siap untuk diproses serah terima ke Helpdesk / Pengguna.";

            var senderLabel = isFromHelpdesk ? "Diserahkan Oleh" : "Teknisi Penyerah";
            var title = isFromHelpdesk ? "Radio Masuk WH (Dari Helpdesk)" : "Radio Selesai Diperbaiki — Siap di WH";

            var content = $@"
                <p style='color:#334155;font-size:14px;line-height:1.6;margin-top:0;'>
                    Halo Tim Helpdesk,
                </p>
                <p style='color:#334155;font-size:14px;line-height:1.6;'>
                    {introText}
                </p>
                
                <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#F8FAFC;border:1px solid #E2E8F0;border-radius:12px;margin:20px 0;'>
                    <tr>
                        <td style='padding:16px;'>
                            <table width='100%' cellpadding='6' cellspacing='0' style='font-size:13px;'>
                                <tr>
                                    <td style='color:#64748B;width:130px;font-weight:600;'>No. Tiket MKN</td>
                                    <td style='color:#0F172A;font-weight:700;'>{ticketNumber}</td>
                                </tr>
                                <tr>
                                    <td style='color:#64748B;font-weight:600;'>Serial Number</td>
                                    <td style='color:#0F172A;font-weight:700;'>{radioSerial}</td>
                                </tr>
                                <tr>
                                    <td style='color:#64748B;font-weight:600;'>Tipe / Model</td>
                                    <td style='color:#0F172A;'>{equipmentName}</td>
                                </tr>
                                <tr>
                                    <td style='color:#64748B;font-weight:600;'>No. Unit / Fleet</td>
                                    <td style='color:#0F172A;'>{unitNumber ?? "-"}</td>
                                </tr>
                                <tr>
                                    <td style='color:#64748B;font-weight:600;'>{senderLabel}</td>
                                    <td style='color:#0F172A;'>{technicianName}</td>
                                </tr>
                                <tr>
                                    <td style='color:#64748B;font-weight:600;'>Waktu Masuk WH</td>
                                    <td style='color:#0F172A;'>{formattedDate} WITA</td>
                                </tr>
                                <tr>
                                    <td style='color:#64748B;font-weight:600;'>Catatan</td>
                                    <td style='color:#0F172A;'>{notes ?? "-"}</td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>

                <p style='color:#64748B;font-size:12px;margin-bottom:0;'>
                    Silakan klik tombol di bawah untuk membuka halaman <strong>Radio Masuk WH</strong>.
                </p>";

            var body = GetBaseHtmlTemplate(title, content, "Buka Radio Masuk WH", targetLink);
            return await SendEmailInternalAsync(toEmail, subject, body);
        }

        public async Task<bool> SendTestNotificationEmailAsync(string toEmail)
        {
            var subject = "[Test Email] Pengaturan Notifikasi Email Helpdesk - PM Dashboard";
            var content = $@"
                <p style='color:#334155;font-size:14px;line-height:1.6;margin-top:0;'>
                    Halo,
                </p>
                <p style='color:#334155;font-size:14px;line-height:1.6;'>
                    Ini adalah <strong>email uji coba (test email)</strong> dari sistem PM Dashboard untuk memverifikasi bahwa konfigurasi email notifikasi Helpdesk telah berfungsi dengan baik.
                </p>
                <div style='background-color:#F0FDF4;border:1px solid #BBF7D0;border-radius:10px;padding:16px;margin:16px 0;color:#166534;font-size:13px;font-weight:600;'>
                    ✅ Konfigurasi Email Berhasil & Siap Menerima Notifikasi
                </div>";

            var body = GetBaseHtmlTemplate("Uji Coba Notifikasi Email", content);
            return await SendEmailInternalAsync(toEmail, subject, body);
        }
    }
}