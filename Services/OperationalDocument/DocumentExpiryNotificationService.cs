using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.Models;
using Pm.Services.Notification;
using Pm.DTOs.Notification;

namespace Pm.Services
{
    /// <summary>
    /// Background Service: Cron job harian untuk kirim notifikasi Telegram
    /// saat dokumen operasional mendekati tanggal berakhir.
    ///
    /// Threshold: H-30, H-14, H-7, H-3, H-1, H-0
    /// Skip dokumen dengan FollowUpStatus "SedangDiproses" atau "Selesai".
    /// Anti-duplikat: cek NotificationHistory sebelum kirim.
    ///
    /// Grouped Notification:
    ///   Dokumen yang punya GroupName yang sama + ValidUntil yang sama
    ///   akan digabung menjadi 1 notifikasi Telegram (tidak dikirim satupersatu).
    ///   PIC phone yang digunakan adalah dari dokumen pertama dalam grup.
    /// </summary>
    public class DocumentExpiryNotificationService(
        IServiceProvider _serviceProvider,
        IConfiguration _configuration,
        ILogger<DocumentExpiryNotificationService> _logger) : BackgroundService
    {
        private static readonly int[] NotificationThresholds = [30, 14, 7, 3, 1, 0];

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DocumentExpiryNotificationService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunNotificationJobAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saat menjalankan DocumentExpiryNotificationService");
                }

                var delay = GetDelayUntilNextRun();
                _logger.LogInformation("Job selesai. Jadwal berikutnya dalam {Minutes} menit.", delay.TotalMinutes);
                await Task.Delay(delay, stoppingToken);
            }
        }

        public async Task RunNotificationJobAsync(CancellationToken ct)
        {
            _logger.LogInformation("[DocExpiry] Job berjalan pada {Time}", DateTime.Now);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var Telegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var today = DateTime.UtcNow.Date;

            // Ambil semua dokumen yang belum selesai ditindaklanjuti dan punya no WA
            // Ambil semua dokumen yang belum selesai ditindaklanjuti dan punya no WA
            var documents = await db.OperationalDocuments
                .Include(d => d.BhpChecklists)
                .AsNoTracking()
                .Where(d => d.PicTelegramId != null && d.PicTelegramId != ""
                         && d.FollowUpStatus != "SedangDiproses"
                         && d.FollowUpStatus != "Selesai")
                .ToListAsync(ct);

            // ─────────────────────────────────────────────────────────────
            // Pisahkan dokumen menjadi dua kategori:
            //   A) Dokumen dengan GroupName → proses sebagai grup
            //   B) Dokumen tanpa GroupName  → proses individual seperti biasa
            // ─────────────────────────────────────────────────────────────

            var groupedDocs = documents
                .Where(d => !string.IsNullOrWhiteSpace(d.GroupName))
                .GroupBy(d => new
                {
                    GroupName = d.GroupName!,
                    ValidUntilDate = d.ValidUntil.Date,
                    // Kirim ke PIC phone pertama dalam grup (semua harus sama idealnya)
                    PicTelegramId = d.PicTelegramId!
                })
                .ToList();

            var individualDocs = documents
                .Where(d => string.IsNullOrWhiteSpace(d.GroupName))
                .ToList();

            int totalProcessed = 0;

            // ── A. Proses Grouped Notifications ──────────────────────────
            foreach (var group in groupedDocs)
            {
                var daysRemaining = (int)(group.Key.ValidUntilDate - today).TotalDays;

                if (NotificationThresholds.Contains(daysRemaining))
                {
                    var representativeDoc = group.First();

                    var alreadySent = await db.OperationalDocumentNotificationHistories
                        .AnyAsync(h => h.OperationalDocumentId == representativeDoc.Id
                                       && h.DaysRemaining == daysRemaining
                                       && h.NotifiedAt.Date == today, ct);

                    if (!alreadySent)
                    {
                        _logger.LogInformation("[DocExpiry] Kirim Grouped WA: Group='{Group}', Phone={Phone}, H-{Days}, Count={Count}",
                            group.Key.GroupName, group.Key.PicTelegramId, daysRemaining, group.Count());

                        var sent = await Telegram.SendGroupedDocumentExpiryMessageAsync(
                            chatId: group.Key.PicTelegramId,
                            groupName: group.Key.GroupName,
                            daysRemaining: daysRemaining,
                            validUntil: group.Key.ValidUntilDate,
                            documents: group.Select(d => (d.Name, d.ValidUntil))
                        );

                        if (sent)
                        {
                            foreach (var doc in group)
                            {
                                db.OperationalDocumentNotificationHistories.Add(new OperationalDocumentNotificationHistory
                                {
                                    OperationalDocumentId = doc.Id,
                                    NotifiedAt = DateTime.UtcNow,
                                    DaysRemaining = daysRemaining
                                });
                            }
                            await db.SaveChangesAsync(ct);
                            totalProcessed += group.Count();

                            await notificationService.CreateForPermissionAsync(
                                "notification.operationaldocument.expiry",
                                new CreateNotificationDto
                                {
                                    Title = $"Dokumen Hampir Expired (Grup {group.Key.GroupName})",
                                    Message = $"Terdapat {group.Count()} dokumen dalam grup '{group.Key.GroupName}' yang akan berakhir dalam {daysRemaining} hari.",
                                    Category = "OperationalDocument",
                                    LinkUrl = "/operational-documents"
                                }
                            );
                        }
                    }
                    else
                    {
                        _logger.LogDebug("[DocExpiry] Skip grup (sudah terkirim): Group='{Group}', H-{Days}",
                            group.Key.GroupName, daysRemaining);
                    }
                }

                // --- Pengecekan Anniversary (Grouped) ---
                var annivDocs = new List<OperationalDocument>();
                int? groupDaysToAnniv = null;

                foreach (var doc in group)
                {
                    var anniv = GetCurrentYearAnniversary(doc.ValidFrom, doc.ValidUntil, today);
                    if (anniv.HasValue)
                    {
                        var dta = (int)(anniv.Value - today).TotalDays;
                        if (NotificationThresholds.Contains(dta))
                        {
                            bool isPaid = doc.BhpChecklists?.Any(c => c.Year == anniv.Value.Year && c.IsPaid) ?? false;
                            if (!isPaid)
                            {
                                annivDocs.Add(doc);
                                groupDaysToAnniv = dta; 
                            }
                        }
                    }
                }

                if (annivDocs.Count > 0 && groupDaysToAnniv.HasValue)
                {
                    var repDocAnniv = annivDocs.First();
                    var annivDays = groupDaysToAnniv.Value;

                    var alreadySentAnniv = await db.OperationalDocumentNotificationHistories
                        .AnyAsync(h => h.OperationalDocumentId == repDocAnniv.Id
                                       && h.DaysRemaining == annivDays
                                       && h.NotifiedAt.Date == today, ct);

                    if (!alreadySentAnniv)
                    {
                        _logger.LogInformation("[DocExpiry] Kirim Grouped BHP Anniversary WA: Group='{Group}', H-{Days}",
                            group.Key.GroupName, annivDays);

                        // Cek apakah semua ISR → kirim grouped BHP reminder dengan detail
                        bool allIsr = annivDocs.All(d => d.Type?.Contains("ISR", StringComparison.OrdinalIgnoreCase) == true);

                        bool sentAnniv;
                        if (allIsr)
                        {
                            // Kirim grouped BHP reminder dengan detail per dokumen
                            var groupDetailItems = annivDocs.Select(d =>
                            {
                                var unpaidYears = d.BhpChecklists?
                                    .Where(c => !c.IsPaid)
                                    .Select(c => c.Year)
                                    .OrderBy(y => y)
                                    .ToList() ?? [];
                                return (
                                    DocName: d.Name,
                                    UnpaidCount: unpaidYears.Count,
                                    UnpaidYears: (IEnumerable<int>)unpaidYears
                                );
                            }).Where(x => x.UnpaidCount > 0).ToList();

                            if (groupDetailItems.Count > 0)
                            {
                                sentAnniv = await Telegram.SendGroupedBhpPaymentReminderAsync(
                                    chatId: group.Key.PicTelegramId,
                                    groupName: group.Key.GroupName,
                                    daysToAnniv: annivDays,
                                    groupItems: groupDetailItems
                                );
                            }
                            else
                            {
                                // Semua sudah lunas, tidak perlu kirim reminder BHP
                                sentAnniv = false;
                                _logger.LogInformation("[DocExpiry] Skip BHP reminder grup '{Group}' — semua sudah lunas.", group.Key.GroupName);
                            }
                        }
                        else
                        {
                            sentAnniv = await Telegram.SendGroupedDocumentAnniversaryMessageAsync(
                                chatId: group.Key.PicTelegramId,
                                groupName: group.Key.GroupName,
                                daysRemaining: annivDays,
                                validUntil: group.Key.ValidUntilDate,
                                documents: annivDocs.Select(d => (d.Name, d.Type ?? ""))
                            );
                        }

                        if (sentAnniv)
                        {
                            foreach (var doc in annivDocs)
                            {
                                db.OperationalDocumentNotificationHistories.Add(new OperationalDocumentNotificationHistory
                                {
                                    OperationalDocumentId = doc.Id,
                                    NotifiedAt = DateTime.UtcNow,
                                    DaysRemaining = annivDays
                                });
                            }
                            await db.SaveChangesAsync(ct);
                            totalProcessed += annivDocs.Count;

                            await notificationService.CreateForPermissionAsync(
                                "notification.operationaldocument.expiry",
                                new CreateNotificationDto
                                {
                                    Title = $"Peringatan BHP Tahunan (Grup {group.Key.GroupName})",
                                    Message = $"Terdapat {annivDocs.Count} dokumen ISR dalam grup '{group.Key.GroupName}' yang belum melunasi BHP tahunan. Jatuh tempo {annivDays} hari lagi.",
                                    Category = "OperationalDocument",
                                    LinkUrl = "/operational-documents"
                                }
                            );
                        }
                    }
                }
            }

            // ── B. Proses Individual Notifications ───────────────────────
            foreach (var doc in individualDocs)
            {
                var daysRemaining = (int)(doc.ValidUntil.Date - today).TotalDays;

                if (NotificationThresholds.Contains(daysRemaining))
                {
                    var alreadySent = await db.OperationalDocumentNotificationHistories
                        .AnyAsync(h => h.OperationalDocumentId == doc.Id
                                       && h.DaysRemaining == daysRemaining
                                       && h.NotifiedAt.Date == today, ct);

                    if (!alreadySent)
                    {
                        _logger.LogInformation("[DocExpiry] Kirim WA: DocId={Id}, Phone={Phone}, H-{Days}",
                            doc.Id, doc.PicTelegramId, daysRemaining);

                        var sent = await Telegram.SendDocumentExpiryMessageAsync(
                            chatId: doc.PicTelegramId!,
                            documentName: doc.Name,
                            daysRemaining: daysRemaining,
                            validUntil: doc.ValidUntil,
                            fileLink: doc.FileLink,
                            documentId: doc.Id.ToString()
                        );

                        if (sent)
                        {
                            db.OperationalDocumentNotificationHistories.Add(new OperationalDocumentNotificationHistory
                            {
                                OperationalDocumentId = doc.Id,
                                NotifiedAt = DateTime.UtcNow,
                                DaysRemaining = daysRemaining
                            });
                            await db.SaveChangesAsync(ct);
                            totalProcessed++;

                            await notificationService.CreateForPermissionAsync(
                                "notification.operationaldocument.expiry",
                                new CreateNotificationDto
                                {
                                    Title = $"Dokumen Hampir Expired ({daysRemaining} Hari)",
                                    Message = $"Dokumen {doc.Name} ({doc.Type}) akan berakhir pada {doc.ValidUntil:dd MMM yyyy}.",
                                    Category = "OperationalDocument",
                                    LinkUrl = "/operational-documents"
                                }
                            );
                        }
                    }
                    else
                    {
                        _logger.LogDebug("[DocExpiry] Skip (sudah terkirim): DocId={Id}, H-{Days}", doc.Id, daysRemaining);
                    }
                }

                // --- Pengecekan Anniversary (Individual) ---
                var anniv = GetCurrentYearAnniversary(doc.ValidFrom, doc.ValidUntil, today);
                if (anniv.HasValue)
                {
                    var dta = (int)(anniv.Value - today).TotalDays;
                    if (NotificationThresholds.Contains(dta))
                    {
                        bool isPaid = doc.BhpChecklists?.Any(c => c.Year == anniv.Value.Year && c.IsPaid) ?? false;
                        if (!isPaid)
                        {
                            var alreadySentAnniv = await db.OperationalDocumentNotificationHistories
                                .AnyAsync(h => h.OperationalDocumentId == doc.Id
                                               && h.DaysRemaining == dta
                                               && h.NotifiedAt.Date == today, ct);

                            if (!alreadySentAnniv)
                            {
                                _logger.LogInformation("[DocExpiry] Kirim Anniversary WA: DocId={Id}, H-{Days}", doc.Id, dta);

                                bool isIsr = doc.Type?.Contains("ISR", StringComparison.OrdinalIgnoreCase) == true;
                                bool sentAnniv;

                                if (isIsr && doc.BhpChecklists != null && doc.BhpChecklists.Count > 0)
                                {
                                    // Kirim BHP reminder detail untuk dokumen ISR
                                    var bhpItems = doc.BhpChecklists
                                        .OrderBy(c => c.Year)
                                        .Select(c => (c.Year, c.IsPaid, c.InvoiceNumber));

                                    // Hanya kirim jika masih ada yang belum bayar
                                    var hasUnpaid = doc.BhpChecklists.Any(c => !c.IsPaid);
                                    if (hasUnpaid)
                                    {
                                        sentAnniv = await Telegram.SendBhpPaymentReminderAsync(
                                            chatId: doc.PicTelegramId!,
                                            documentName: doc.Name,
                                            daysToAnniv: dta,
                                            currentYear: DateTime.UtcNow.Year,
                                            bhpItems: bhpItems
                                        );
                                    }
                                    else
                                    {
                                        sentAnniv = false;
                                        _logger.LogInformation("[DocExpiry] Skip BHP reminder DocId={Id} — semua sudah lunas.", doc.Id);
                                    }
                                }
                                else
                                {
                                    sentAnniv = await Telegram.SendDocumentAnniversaryMessageAsync(
                                        chatId: doc.PicTelegramId!,
                                        documentName: doc.Name,
                                        daysRemaining: dta,
                                        validUntil: doc.ValidUntil,
                                        fileLink: doc.FileLink,
                                        documentId: doc.Id.ToString(),
                                        documentType: doc.Type ?? ""
                                    );
                                }

                                if (sentAnniv)
                                {
                                    db.OperationalDocumentNotificationHistories.Add(new OperationalDocumentNotificationHistory
                                    {
                                        OperationalDocumentId = doc.Id,
                                        NotifiedAt = DateTime.UtcNow,
                                        DaysRemaining = dta
                                    });
                                    await db.SaveChangesAsync(ct);
                                    totalProcessed++;

                                    string annivTitle = isIsr
                                        ? $"Peringatan Pembayaran BHP ({dta} Hari)"
                                        : $"Peringatan Tahunan ({dta} Hari)";

                                    await notificationService.CreateForPermissionAsync(
                                        "notification.operationaldocument.expiry",
                                        new CreateNotificationDto
                                        {
                                            Title = annivTitle,
                                            Message = isIsr
                                                ? $"Dokumen {doc.Name} memiliki BHP yang belum dibayar. Jatuh tempo dalam {dta} hari."
                                                : $"Dokumen {doc.Name} ({doc.Type}) memasuki jadwal evaluasi/tahunan.",
                                            Category = "OperationalDocument",
                                            LinkUrl = "/operational-documents"
                                        }
                                    );
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogInformation(
                "[DocExpiry] Job selesai. Dokumen total: {Total}, Grup: {Groups}, Individual: {Individual}",
                documents.Count, groupedDocs.Count, individualDocs.Count);
        }

        /// <summary>
        /// Kirim notifikasi Telegram paksa untuk 1 dokumen tertentu (by ID).
        /// Mengabaikan threshold tanggal — khusus Super Admin.
        /// </summary>
        public async Task<(bool success, string message)> SendForceNotificationAsync(int documentId)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var Telegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();

            var doc = await db.OperationalDocuments
                .Include(d => d.BhpChecklists)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (doc == null)
                return (false, "Dokumen tidak ditemukan.");

            if (string.IsNullOrWhiteSpace(doc.PicTelegramId))
                return (false, "Dokumen ini tidak memiliki Telegram Chat ID PIC. Harap isi Telegram Chat ID terlebih dahulu.");

            var daysRemaining = (int)(doc.ValidUntil.Date - DateTime.UtcNow.Date).TotalDays;
            bool isIsr = doc.Type?.Contains("ISR", StringComparison.OrdinalIgnoreCase) == true;

            var chatIds = doc.PicTelegramId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            bool anySent = false;

            foreach (var chatId in chatIds)
            {
                bool sent;

                if (isIsr && doc.BhpChecklists != null && doc.BhpChecklists.Count > 0)
                {
                    // ISR: kirim pesan lengkap dengan detail BHP checklist
                    var bhpItems = doc.BhpChecklists
                        .OrderBy(c => c.Year)
                        .Select(c => (c.Year, c.IsPaid, c.InvoiceNumber));

                    sent = await Telegram.SendBhpPaymentReminderAsync(
                        chatId: chatId,
                        documentName: doc.Name,
                        daysToAnniv: daysRemaining,
                        currentYear: DateTime.UtcNow.Year,
                        bhpItems: bhpItems
                    );
                }
                else
                {
                    // Non-ISR: pesan expiry biasa
                    sent = await Telegram.SendDocumentExpiryMessageAsync(
                        chatId: chatId,
                        documentName: doc.Name,
                        daysRemaining: daysRemaining,
                        validUntil: doc.ValidUntil,
                        fileLink: doc.FileLink,
                        documentId: doc.Id.ToString()
                    );
                }

                if (sent) anySent = true;
            }

            if (anySent)
            {
                _logger.LogInformation("[DocExpiry] Force notification sent: DocId={Id}, ChatId={ChatId}, IsISR={IsIsr}",
                    doc.Id, doc.PicTelegramId, isIsr);
                return (true, $"Notifikasi Telegram berhasil dikirim ke {doc.PicTelegramId}.");
            }

            return (false, "Gagal mengirim notifikasi. Periksa koneksi Telegram Bot.");
        }

        public async Task<(bool isSuccess, string message, int sentCount)> SendForceNotificationBulkAsync(string? groupName, string? type, string? expiryStatus)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var Telegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();

            var query = db.OperationalDocuments.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(groupName))
                query = query.Where(d => d.GroupName == groupName);

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(d => d.Type == type);

            var today = DateTime.UtcNow.Date;
            if (!string.IsNullOrWhiteSpace(expiryStatus))
            {
                if (expiryStatus.Equals("Expired", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(d => d.ValidUntil.Date < today);
                else if (expiryStatus.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(d => d.ValidUntil.Date >= today && d.ValidUntil.Date <= today.AddDays(30));
                else if (expiryStatus.Equals("Aman", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(d => d.ValidUntil.Date > today.AddDays(30));
            }

            var docs = await query.ToListAsync();
            
            var groupedByPhone = docs
                .Where(d => !string.IsNullOrWhiteSpace(d.PicTelegramId))
                .SelectMany(d => d.PicTelegramId!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(p => new { Phone = p, Document = d }))
                .GroupBy(x => x.Phone)
                .ToList();

            int sentCount = 0;

            foreach (var phoneGroup in groupedByPhone)
            {
                var phone = phoneGroup.Key;
                var phoneDocs = phoneGroup.Select(x => x.Document).ToList();
                bool sent;

                if (phoneDocs.Count == 1)
                {
                    // Hanya 1 dokumen → kirim pesan individual
                    var doc = phoneDocs[0];
                    var daysRemaining = (int)(doc.ValidUntil.Date - today).TotalDays;
                    sent = await Telegram.SendDocumentExpiryMessageAsync(
                        chatId: phone,
                        documentName: doc.Name,
                        daysRemaining: daysRemaining,
                        validUntil: doc.ValidUntil,
                        fileLink: doc.FileLink,
                        documentId: doc.Id.ToString()
                    );
                }
                else
                {
                    // Lebih dari 1 dokumen dengan Telegram ID yang sama → kirim 1 pesan gabungan
                    var nearestDoc = phoneDocs.OrderBy(d => d.ValidUntil).First();
                    var daysRemaining = (int)(nearestDoc.ValidUntil.Date - today).TotalDays;
                    var groupLabel = !string.IsNullOrWhiteSpace(groupName)
                        ? groupName
                        : (!string.IsNullOrWhiteSpace(type) ? type : "Dokumen Terpilih");

                    sent = await Telegram.SendGroupedDocumentExpiryMessageAsync(
                        chatId: phone,
                        groupName: groupLabel,
                        daysRemaining: daysRemaining,
                        validUntil: nearestDoc.ValidUntil,
                        documents: phoneDocs.Select(d => (d.Name, d.ValidUntil))
                    );
                }

                if (sent) sentCount++;
            }

            return (true, $"Berhasil mengirim notifikasi ke {sentCount} Telegram Chat ID (dari {docs.Count} dokumen).", sentCount);
        }

        /// <summary>
        /// Hitung waktu tunggu hingga jam 07:00 WIB (UTC+7 = 00:00 UTC) hari berikutnya.
        /// Saat dev/testing, bisa override NotificationRunHour di appsettings.
        /// </summary>
        private TimeSpan GetDelayUntilNextRun()
        {
            var runHour = _configuration.GetValue("TelegramSettings:NotificationRunHour", 7);
            var nowWib = DateTime.UtcNow.AddHours(7); // UTC+7
            var nextRun = nowWib.Date.AddHours(runHour);

            if (nowWib >= nextRun)
                nextRun = nextRun.AddDays(1);

            return nextRun - nowWib;
        }

        private static DateTime? GetCurrentYearAnniversary(DateTime validFrom, DateTime validUntil, DateTime today)
        {
            // Jika durasi <= 1 tahun, tidak ada anniversary tahunan
            if ((validUntil - validFrom).TotalDays <= 365) return null;

            int currentYear = today.Year;

            // Jika tahun sekarang sama dengan atau lebih dari tahun ValidUntil,
            // biarkan notifikasi Expiry biasa yang menangani (tidak ada lagi anniversary tengah tahun)
            if (currentYear >= validUntil.Year) return null;

            int month = validUntil.Month;
            int day = validUntil.Day;

            // Handle kabisat (Feb 29)
            if (month == 2 && day == 29 && !DateTime.IsLeapYear(currentYear))
            {
                day = 28;
            }

            var annivDate = new DateTime(currentYear, month, day);

            // Jika hari ini sudah terlewat dari annivDate, lompat ke tahun depan
            if (today > annivDate)
            {
                int nextYear = currentYear + 1;
                if (nextYear < validUntil.Year)
                {
                    int nextDay = month == 2 && day == 29 && !DateTime.IsLeapYear(nextYear) ? 28 : day;
                    return new DateTime(nextYear, month, nextDay);
                }
                return null;
            }

            // Pastikan anniversary date valid sesudah ValidFrom
            if (annivDate <= validFrom)
            {
                int nextYear = currentYear + 1;
                if (nextYear < validUntil.Year)
                {
                    int nextDay = month == 2 && day == 29 && !DateTime.IsLeapYear(nextYear) ? 28 : day;
                    return new DateTime(nextYear, month, nextDay);
                }
                return null;
            }

            return annivDate;
        }
    }
}


