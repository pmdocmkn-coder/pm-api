using System.Text;
using System.Text.Json;

namespace Pm.Services
{
    public class WhatsAppSettings
    {
        public string Provider { get; set; } = "fonnte";
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.fonnte.com/send";
        public string DashboardBaseUrl { get; set; } = string.Empty;
        public int NotificationRunHour { get; set; } = 7;
    }

    public class WhatsAppService(
        IHttpClientFactory _httpClientFactory,
        IConfiguration _configuration,
        ILogger<WhatsAppService> _logger) : IWhatsAppService
    {
        public async Task<bool> SendDocumentExpiryMessageAsync(
            string phone,
            string documentName,
            int daysRemaining,
            DateTime validUntil,
            string? fileLink,
            string documentId)
        {
            var settings = _configuration.GetSection("WhatsAppSettings").Get<WhatsAppSettings>() ?? new WhatsAppSettings();

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                _logger.LogWarning("WhatsApp ApiKey belum dikonfigurasi. Notifikasi tidak terkirim untuk dokumen ID {Id}", documentId);
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
            message.AppendLine("[PM Dashboard MKN]");
            message.AppendLine();
            message.AppendLine($"📄 Dokumen: *{documentName}*");
            message.AppendLine($"📅 Status: {statusLabel} ({validUntil:dd MMM yyyy})");

            if (!string.IsNullOrWhiteSpace(fileLink))
                message.AppendLine($"🔗 File: {fileLink}");

            message.AppendLine($"🔍 Cek detail: {settings.DashboardBaseUrl}/#/operational-documents/{documentId}");
            message.AppendLine();
            message.AppendLine("Segera lakukan tindak lanjut atau tandai \"Sedang Diproses\" di dashboard untuk menghentikan notifikasi.");

            try
            {
                var client = _httpClientFactory.CreateClient("fonnte");
                var payload = new Dictionary<string, string>
                {
                    { "target", phone },
                    { "message", message.ToString() },
                    { "countryCode", "62" }
                };

                using var content = new FormUrlEncodedContent(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, settings.BaseUrl)
                {
                    Content = content
                };
                request.Headers.Add("Authorization", settings.ApiKey);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("WhatsApp terkirim ke {Phone} untuk dokumen ID {Id}. Response: {Body}", 
                        phone, documentId, responseBody);
                    return true;
                }

                _logger.LogWarning("WhatsApp GAGAL ke {Phone} untuk dokumen ID {Id}. Status: {Status}, Body: {Body}", 
                    phone, documentId, response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception saat kirim WhatsApp ke {Phone} untuk dokumen ID {Id}", phone, documentId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SendGroupedDocumentExpiryMessageAsync(
            string phone,
            string groupName,
            int daysRemaining,
            DateTime validUntil,
            IEnumerable<string> documentNames)
        {
            var settings = _configuration.GetSection("WhatsAppSettings").Get<WhatsAppSettings>() ?? new WhatsAppSettings();

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                _logger.LogWarning("WhatsApp ApiKey belum dikonfigurasi. Grouped notifikasi tidak terkirim untuk grup '{Group}'", groupName);
                return false;
            }

            var statusLabel = daysRemaining switch
            {
                0  => "⚠️ *EXPIRED HARI INI!*",
                < 0 => $"⚠️ *SUDAH EXPIRED {Math.Abs(daysRemaining)} hari lalu*",
                1  => "⏰ akan berakhir *BESOK*",
                _  => $"⏰ akan berakhir *{daysRemaining} hari lagi*"
            };

            var docList = documentNames.ToList();
            var sb = new StringBuilder();
            sb.AppendLine("[PM Dashboard MKN]");
            sb.AppendLine();
            sb.AppendLine($"📂 Grup Dokumen: *{groupName}*");
            sb.AppendLine($"📅 Status: {statusLabel} ({validUntil:dd MMM yyyy})");
            sb.AppendLine();
            sb.AppendLine($"Dokumen terkait ({docList.Count} dokumen):");
            foreach (var name in docList.Take(10))
                sb.AppendLine($"  • {name}");
            if (docList.Count > 10)
                sb.AppendLine($"  ... dan {docList.Count - 10} dokumen lainnya");
            sb.AppendLine();
            sb.AppendLine($"🔍 Cek semua dokumen: {settings.DashboardBaseUrl}/#/operational-documents");
            sb.AppendLine();
            sb.AppendLine("Segera lakukan tindak lanjut atau tandai \"Sedang Diproses\" di dashboard untuk menghentikan notifikasi.");

            try
            {
                var client = _httpClientFactory.CreateClient("fonnte");
                var payload = new Dictionary<string, string>
                {
                    { "target", phone },
                    { "message", sb.ToString() },
                    { "countryCode", "62" }
                };

                using var content = new FormUrlEncodedContent(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, settings.BaseUrl)
                {
                    Content = content
                };
                request.Headers.Add("Authorization", settings.ApiKey);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Grouped WA terkirim ke {Phone} untuk grup '{Group}'. Response: {Body}",
                        phone, groupName, responseBody);
                    return true;
                }

                _logger.LogWarning("Grouped WA GAGAL ke {Phone} untuk grup '{Group}'. Status: {Status}, Body: {Body}",
                    phone, groupName, response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception saat kirim grouped WA ke {Phone} untuk grup '{Group}'", phone, groupName);
                return false;
            }
        }

        public async Task<bool> SendDocumentAnniversaryMessageAsync(
            string phone,
            string documentName,
            int daysRemaining,
            DateTime validUntil,
            string? fileLink,
            string documentId,
            string documentType)
        {
            var settings = _configuration.GetSection("WhatsAppSettings").Get<WhatsAppSettings>() ?? new WhatsAppSettings();

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                _logger.LogWarning("WhatsApp ApiKey belum dikonfigurasi. Anniversary WA tidak terkirim untuk dokumen ID {Id}", documentId);
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
            message.AppendLine("[PM Dashboard MKN]");
            message.AppendLine();
            message.AppendLine(titleMsg);
            message.AppendLine($"📄 Dokumen: *{documentName}*");
            message.AppendLine($"📅 Jadwal Tahunan: {statusLabel}");
            message.AppendLine($"*(Catatan: Dokumen ini baru akan berakhir penuh pada {validUntil:dd MMM yyyy})*");

            if (!string.IsNullOrWhiteSpace(fileLink))
                message.AppendLine($"🔗 File: {fileLink}");

            message.AppendLine($"🔍 Cek detail: {settings.DashboardBaseUrl}/#/operational-documents/{documentId}");
            message.AppendLine();
            message.AppendLine("Silakan cek dashboard untuk detail lebih lanjut.");

            try
            {
                var client = _httpClientFactory.CreateClient("fonnte");
                var payload = new Dictionary<string, string>
                {
                    { "target", phone },
                    { "message", message.ToString() },
                    { "countryCode", "62" }
                };

                using var content = new FormUrlEncodedContent(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, settings.BaseUrl) { Content = content };
                request.Headers.Add("Authorization", settings.ApiKey);

                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception saat kirim Anniversary WA ke {Phone} untuk dokumen ID {Id}", phone, documentId);
                return false;
            }
        }

        public async Task<bool> SendGroupedDocumentAnniversaryMessageAsync(
            string phone,
            string groupName,
            int daysRemaining,
            DateTime validUntil,
            IEnumerable<(string Name, string Type)> documents)
        {
            var settings = _configuration.GetSection("WhatsAppSettings").Get<WhatsAppSettings>() ?? new WhatsAppSettings();

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                _logger.LogWarning("WhatsApp ApiKey belum dikonfigurasi. Grouped Anniversary WA tidak terkirim untuk grup '{Group}'", groupName);
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
            sb.AppendLine("[PM Dashboard MKN]");
            sb.AppendLine();
            sb.AppendLine(titleMsg);
            sb.AppendLine($"📂 Grup Dokumen: *{groupName}*");
            sb.AppendLine($"📅 Jadwal Tahunan: {statusLabel}");
            sb.AppendLine($"*(Catatan: Masa berlaku penuh grup ini berakhir pada {validUntil:dd MMM yyyy})*");
            sb.AppendLine();
            sb.AppendLine($"Dokumen terkait ({docList.Count} dokumen):");
            
            foreach (var doc in docList.Take(10))
                sb.AppendLine($"  • {doc.Name}");
                
            if (docList.Count > 10)
                sb.AppendLine($"  ... dan {docList.Count - 10} dokumen lainnya");
                
            sb.AppendLine();
            sb.AppendLine($"🔍 Cek semua dokumen: {settings.DashboardBaseUrl}/#/operational-documents");
            sb.AppendLine();
            sb.AppendLine("Silakan cek dashboard untuk detail lebih lanjut.");

            try
            {
                var client = _httpClientFactory.CreateClient("fonnte");
                var payload = new Dictionary<string, string>
                {
                    { "target", phone },
                    { "message", sb.ToString() },
                    { "countryCode", "62" }
                };

                using var content = new FormUrlEncodedContent(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, settings.BaseUrl) { Content = content };
                request.Headers.Add("Authorization", settings.ApiKey);

                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception saat kirim Grouped Anniversary WA ke {Phone} untuk grup '{Group}'", phone, groupName);
                return false;
            }
        }
    }
}
