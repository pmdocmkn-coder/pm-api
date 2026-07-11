using System.Text;
using Pm.Services.Telegram;

namespace Pm.Services
{
    public class TelegramSettings
    {
        public string BotToken { get; set; } = string.Empty;
        public int NotificationRunHour { get; set; } = 7;
    }

    public class TelegramService(
        ITelegramQueueService _queueService,
        IConfiguration _configuration,
        ILogger<TelegramService> _logger) : ITelegramService
    {
        public async Task<bool> SendDocumentExpiryMessageAsync(
            string chatId,
            string documentName,
            int daysRemaining,
            DateTime validUntil,
            string? fileLink,
            string documentId)
        {
            var settings = _configuration.GetSection("TelegramSettings").Get<TelegramSettings>() ?? new TelegramSettings();

            if (string.IsNullOrWhiteSpace(settings.BotToken))
            {
                _logger.LogWarning("Telegram BotToken belum dikonfigurasi. Notifikasi tidak terkirim untuk dokumen ID {Id}", documentId);
                return false;
            }

            var statusLabel = daysRemaining switch
            {
                0 => "⚠️ *EXPIRED HARI INI!*",
                < 0 => $"⚠️ *SUDAH EXPIRED {Math.Abs(daysRemaining)} hari lalu*",
                1 => "⏰ akan berakhir *BESOK*",
                _ => $"⏰ akan berakhir *{daysRemaining} hari lagi*"
            };

            var message = new StringBuilder();
            message.AppendLine("*\\[PM Dashboard MKN\\]*");
            message.AppendLine();
            message.AppendLine($"📄 Dokumen: *{EscapeMarkdown(documentName)}*");
            message.AppendLine($"📅 Status: {statusLabel} ({validUntil:dd MMM yyyy})");

            if (!string.IsNullOrWhiteSpace(fileLink))
                message.AppendLine($"🔗 File: {EscapeMarkdown(fileLink)}");

            message.AppendLine();
            message.AppendLine("Segera lakukan tindak lanjut atau tandai \"Sedang Diproses\" di dashboard untuk menghentikan notifikasi.");

            try
            {
                await _queueService.EnqueueMessageAsync(chatId, message.ToString());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception saat queue Telegram ke {ChatId} untuk dokumen ID {Id}", chatId, documentId);
                return false;
            }
        }

        public async Task<bool> SendGroupedDocumentExpiryMessageAsync(
            string chatId,
            string groupName,
            int daysRemaining,
            DateTime validUntil,
            IEnumerable<(string Name, DateTime ValidUntil)> documents)
        {
            var settings = _configuration.GetSection("TelegramSettings").Get<TelegramSettings>() ?? new TelegramSettings();

            if (string.IsNullOrWhiteSpace(settings.BotToken))
            {
                _logger.LogWarning("Telegram BotToken belum dikonfigurasi. Grouped notifikasi tidak terkirim untuk grup '{Group}'", groupName);
                return false;
            }

            var statusLabel = daysRemaining switch
            {
                0  => "⚠️ *EXPIRED HARI INI!*",
                < 0 => $"⚠️ *SUDAH EXPIRED {Math.Abs(daysRemaining)} hari lalu*",
                1  => "⏰ akan berakhir *BESOK*",
                _  => $"⏰ akan berakhir *{daysRemaining} hari lagi*"
            };

            var docList = documents.ToList();
            var sb = new StringBuilder();
            sb.AppendLine("*\\[PM Dashboard MKN\\]*");
            sb.AppendLine();
            sb.AppendLine($"📂 Grup Dokumen: *{EscapeMarkdown(groupName)}*");
            
            bool allSameDate = docList.All(d => d.ValidUntil.Date == docList[0].ValidUntil.Date);

            if (allSameDate)
            {
                sb.AppendLine($"📅 Status: {statusLabel} ({docList[0].ValidUntil:dd MMM yyyy})");
                sb.AppendLine();
                sb.AppendLine($"Dokumen terkait ({docList.Count} dokumen):");
                foreach (var doc in docList.Take(10))
                    sb.AppendLine($"  • {EscapeMarkdown(doc.Name)}");
            }
            else
            {
                sb.AppendLine($"📅 Peringatan: Dokumen memiliki masa berlaku berbeda-beda");
                sb.AppendLine();
                sb.AppendLine($"Dokumen terkait ({docList.Count} dokumen):");
                var today = DateTime.UtcNow.Date;
                foreach (var doc in docList.Take(10))
                {
                    var docDays = (int)(doc.ValidUntil.Date - today).TotalDays;
                    var docLabel = docDays switch
                    {
                        0 => "⚠️ Hari ini",
                        < 0 => $"⚠️ Expired {Math.Abs(docDays)} hr lalu",
                        1 => "⏰ Besok",
                        _ => $"⏰ {docDays} hari lagi"
                    };
                    sb.AppendLine($"  • {EscapeMarkdown(doc.Name)}");
                    sb.AppendLine($"      └ s/d {doc.ValidUntil:dd MMM yyyy} ({docLabel})");
                }
            }

            if (docList.Count > 10)
                sb.AppendLine($"  ... dan {docList.Count - 10} dokumen lainnya");
            sb.AppendLine();
            sb.AppendLine("Segera lakukan tindak lanjut atau tandai \"Sedang Diproses\" di dashboard untuk menghentikan notifikasi.");

            try
            {
                await _queueService.EnqueueMessageAsync(chatId, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception saat queue grouped Telegram ke {ChatId} untuk grup '{Group}'", chatId, groupName);
                return false;
            }
        }

        public async Task<bool> SendDocumentAnniversaryMessageAsync(
            string chatId,
            string documentName,
            int daysRemaining,
            DateTime validUntil,
            string? fileLink,
            string documentId,
            string documentType)
        {
            var settings = _configuration.GetSection("TelegramSettings").Get<TelegramSettings>() ?? new TelegramSettings();

            if (string.IsNullOrWhiteSpace(settings.BotToken))
            {
                _logger.LogWarning("Telegram BotToken belum dikonfigurasi. Anniversary WA tidak terkirim untuk dokumen ID {Id}", documentId);
                return false;
            }

            var statusLabel = daysRemaining switch
            {
                0 => "⚠️ *HARI INI!*",
                < 0 => $"⚠️ *TERLEWAT {Math.Abs(daysRemaining)} hari lalu*",
                1 => "⏰ *BESOK*",
                _ => $"⏰ *{daysRemaining} hari lagi*"
            };

            bool isIsr = documentType?.Contains("ISR", StringComparison.OrdinalIgnoreCase) == true;
            string titleMsg = isIsr ? "⚠️ *Peringatan Tahunan (BHP/Evaluasi) Dokumen*" : "⚠️ *Peringatan Tahunan Dokumen*";

            var message = new StringBuilder();
            message.AppendLine("*\\[PM Dashboard MKN\\]*");
            message.AppendLine();
            message.AppendLine(titleMsg);
            message.AppendLine($"📄 Dokumen: *{EscapeMarkdown(documentName)}*");
            message.AppendLine($"📅 Jadwal Tahunan: {statusLabel}");
            message.AppendLine($"_(Catatan: Dokumen ini baru akan berakhir penuh pada {validUntil:dd MMM yyyy})_");

            if (!string.IsNullOrWhiteSpace(fileLink))
                message.AppendLine($"🔗 File: {EscapeMarkdown(fileLink)}");

            message.AppendLine();
            message.AppendLine("Silakan cek dashboard untuk detail lebih lanjut.");

            try
            {
                await _queueService.EnqueueMessageAsync(chatId, message.ToString());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception saat queue Anniversary Telegram ke {ChatId} untuk dokumen ID {Id}", chatId, documentId);
                return false;
            }
        }

        public async Task<bool> SendGroupedDocumentAnniversaryMessageAsync(
            string chatId,
            string groupName,
            int daysRemaining,
            DateTime validUntil,
            IEnumerable<(string Name, string Type)> documents)
        {
            var settings = _configuration.GetSection("TelegramSettings").Get<TelegramSettings>() ?? new TelegramSettings();

            if (string.IsNullOrWhiteSpace(settings.BotToken))
            {
                _logger.LogWarning("Telegram BotToken belum dikonfigurasi. Grouped Anniversary Telegram tidak terkirim untuk grup '{Group}'", groupName);
                return false;
            }

            var statusLabel = daysRemaining switch
            {
                0 => "⚠️ *HARI INI!*",
                < 0 => $"⚠️ *TERLEWAT {Math.Abs(daysRemaining)} hari lalu*",
                1 => "⏰ *BESOK*",
                _ => $"⏰ *{daysRemaining} hari lagi*"
            };

            var docList = documents.ToList();
            bool anyIsr = docList.Any(d => d.Type?.Contains("ISR", StringComparison.OrdinalIgnoreCase) == true);
            string titleMsg = anyIsr ? "⚠️ *Peringatan Tahunan Grup (BHP/Evaluasi)*" : "⚠️ *Peringatan Tahunan Grup*";

            var sb = new StringBuilder();
            sb.AppendLine("*\\[PM Dashboard MKN\\]*");
            sb.AppendLine();
            sb.AppendLine(titleMsg);
            sb.AppendLine($"📂 Grup Dokumen: *{EscapeMarkdown(groupName)}*");
            sb.AppendLine($"📅 Jadwal Tahunan: {statusLabel}");
            sb.AppendLine($"_(Catatan: Masa berlaku penuh grup ini berakhir pada {validUntil:dd MMM yyyy})_");
            sb.AppendLine();
            sb.AppendLine($"Dokumen terkait ({docList.Count} dokumen):");
            
            foreach (var doc in docList.Take(10))
                sb.AppendLine($"  • {EscapeMarkdown(doc.Name)}");
                
            if (docList.Count > 10)
                sb.AppendLine($"  ... dan {docList.Count - 10} dokumen lainnya");
                
            sb.AppendLine();
            sb.AppendLine("Silakan cek dashboard untuk detail lebih lanjut.");

            try
            {
                await _queueService.EnqueueMessageAsync(chatId, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception saat queue Grouped Anniversary Telegram ke {ChatId} untuk grup '{Group}'", chatId, groupName);
                return false;
            }
        }

        private string EscapeMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[").Replace("]", "\\]").Replace("`", "\\`");
        }
    }
}
