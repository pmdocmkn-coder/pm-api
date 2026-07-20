using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.Models;

namespace Pm.Services
{
    /// <summary>
    /// Hosted service yang berjalan SEKALI saat server start.
    /// Auto-generate BHP checklist rows untuk semua dokumen ISR yang belum punya data.
    /// Idempotent: aman dijalankan berulang, tidak akan duplikasi.
    /// </summary>
    public class BhpBackfillStartupService(IServiceScopeFactory _scopeFactory, ILogger<BhpBackfillStartupService> _logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var isrDocs = await context.OperationalDocuments
                    .Include(d => d.BhpChecklists)
                    .Where(d => d.Type != null && d.Type.Contains("ISR"))
                    .ToListAsync(cancellationToken);

                int generated = 0;
                var currentYear = DateTime.UtcNow.Year;

                foreach (var doc in isrDocs)
                {
                    var existingYears = doc.BhpChecklists.Select(c => c.Year).ToHashSet();

                    // ValidFrom.Year+1 s/d ValidUntil.Year (inklusif)
                    for (int year = doc.ValidFrom.Year + 1; year <= doc.ValidUntil.Year; year++)
                    {
                        if (existingYears.Contains(year)) continue;

                        var isPast = year < currentYear;
                        context.BhpPaymentChecklists.Add(new BhpPaymentChecklist
                        {
                            OperationalDocumentId = doc.Id,
                            Year = year,
                            IsPaid = isPast,
                            InvoiceNumber = isPast ? "Data Migrasi" : null,
                            PaidAt = isPast ? DateTime.UtcNow : null,
                            PaidByUserName = isPast ? "System" : null
                        });
                        generated++;
                    }
                }

                if (generated > 0)
                {
                    await context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("[BHP Backfill] ✅ Auto-generated {Count} checklist rows untuk {DocCount} dokumen ISR.", generated, isrDocs.Count);
                }
                else
                {
                    _logger.LogInformation("[BHP Backfill] Semua dokumen ISR sudah memiliki checklist. Tidak ada yang di-generate.");
                }
            }
            catch (Exception ex)
            {
                // Jangan crash server jika backfill gagal — cukup log
                _logger.LogError(ex, "[BHP Backfill] Gagal saat auto-generate BHP checklist.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
