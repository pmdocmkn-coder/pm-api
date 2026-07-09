using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.Models;
using Pm.Services.Notification;
using Pm.DTOs.Notification;

namespace Pm.Services
{
    /// <summary>
    /// Background Service: Cron job harian untuk kirim notifikasi WhatsApp
    /// saat dokumen operasional mendekati tanggal berakhir.
    ///
    /// Threshold: H-30, H-14, H-7, H-3, H-1, H-0
    /// Skip dokumen dengan FollowUpStatus "SedangDiproses" atau "Selesai".
    /// Anti-duplikat: cek NotificationHistory sebelum kirim.
    ///
    /// Grouped Notification:
    ///   Dokumen yang punya GroupName yang sama + ValidUntil yang sama
    ///   akan digabung menjadi 1 notifikasi WA (tidak dikirim satupersatu).
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

        private async Task RunNotificationJobAsync(CancellationToken ct)
        {
            _logger.LogInformation("[DocExpiry] Job berjalan pada {Time}", DateTime.Now);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var whatsApp = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var today = DateTime.UtcNow.Date;

            // Ambil semua dokumen yang belum selesai ditindaklanjuti dan punya no WA
            var documents = await db.OperationalDocuments
                .AsNoTracking()
                .Where(d => d.PicPhone != null && d.PicPhone != ""
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
                    PicPhone = d.PicPhone!
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
                            group.Key.GroupName, group.Key.PicPhone, daysRemaining, group.Count());

                        var sent = await whatsApp.SendGroupedDocumentExpiryMessageAsync(
                            phone: group.Key.PicPhone,
                            groupName: group.Key.GroupName,
                            daysRemaining: daysRemaining,
                            validUntil: group.Key.ValidUntilDate,
                            documentNames: group.Select(d => d.Name)
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
                            annivDocs.Add(doc);
                            groupDaysToAnniv = dta; 
                        }
                    }
                }

                if (annivDocs.Any() && groupDaysToAnniv.HasValue)
                {
                    var repDocAnniv = annivDocs.First();
                    var annivDays = groupDaysToAnniv.Value;

                    var alreadySentAnniv = await db.OperationalDocumentNotificationHistories
                        .AnyAsync(h => h.OperationalDocumentId == repDocAnniv.Id
                                       && h.DaysRemaining == annivDays
                                       && h.NotifiedAt.Date == today, ct);

                    if (!alreadySentAnniv)
                    {
                        _logger.LogInformation("[DocExpiry] Kirim Grouped Anniversary WA: Group='{Group}', H-{Days}",
                            group.Key.GroupName, annivDays);

                        var sentAnniv = await whatsApp.SendGroupedDocumentAnniversaryMessageAsync(
                            phone: group.Key.PicPhone,
                            groupName: group.Key.GroupName,
                            daysRemaining: annivDays,
                            validUntil: group.Key.ValidUntilDate,
                            documents: annivDocs.Select(d => (d.Name, d.Type ?? ""))
                        );

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
                            totalProcessed += annivDocs.Count();

                            await notificationService.CreateForPermissionAsync(
                                "notification.operationaldocument.expiry",
                                new CreateNotificationDto
                                {
                                    Title = $"Peringatan Tahunan (Grup {group.Key.GroupName})",
                                    Message = $"Terdapat {annivDocs.Count} dokumen dalam grup '{group.Key.GroupName}' yang masuk jadwal tahunan dalam {annivDays} hari.",
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
                            doc.Id, doc.PicPhone, daysRemaining);

                        var sent = await whatsApp.SendDocumentExpiryMessageAsync(
                            phone: doc.PicPhone!,
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
                        var alreadySentAnniv = await db.OperationalDocumentNotificationHistories
                            .AnyAsync(h => h.OperationalDocumentId == doc.Id
                                           && h.DaysRemaining == dta
                                           && h.NotifiedAt.Date == today, ct);

                        if (!alreadySentAnniv)
                        {
                            _logger.LogInformation("[DocExpiry] Kirim Anniversary WA: DocId={Id}, H-{Days}", doc.Id, dta);

                            var sentAnniv = await whatsApp.SendDocumentAnniversaryMessageAsync(
                                phone: doc.PicPhone!,
                                documentName: doc.Name,
                                daysRemaining: dta,
                                validUntil: doc.ValidUntil,
                                fileLink: doc.FileLink,
                                documentId: doc.Id.ToString(),
                                documentType: doc.Type ?? ""
                            );

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

                                bool isIsr = doc.Type?.Contains("ISR", StringComparison.OrdinalIgnoreCase) == true;
                                string annivTitle = isIsr ? $"Pembayaran Tahunan BHP ({dta} Hari)" : $"Peringatan Tahunan ({dta} Hari)";

                                await notificationService.CreateForPermissionAsync(
                                    "notification.operationaldocument.expiry",
                                    new CreateNotificationDto
                                    {
                                        Title = annivTitle,
                                        Message = $"Dokumen {doc.Name} ({doc.Type}) memasuki jadwal evaluasi/tahunan.",
                                        Category = "OperationalDocument",
                                        LinkUrl = "/operational-documents"
                                    }
                                );
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
        /// Hitung waktu tunggu hingga jam 07:00 WIB (UTC+7 = 00:00 UTC) hari berikutnya.
        /// Saat dev/testing, bisa override NotificationRunHour di appsettings.
        /// </summary>
        private TimeSpan GetDelayUntilNextRun()
        {
            var runHour = _configuration.GetValue("WhatsAppSettings:NotificationRunHour", 7);
            var nowWib = DateTime.UtcNow.AddHours(7); // UTC+7
            var nextRun = nowWib.Date.AddHours(runHour);

            if (nowWib >= nextRun)
                nextRun = nextRun.AddDays(1);

            return nextRun - nowWib;
        }

        private DateTime? GetCurrentYearAnniversary(DateTime validFrom, DateTime validUntil, DateTime today)
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
