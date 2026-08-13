using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Models;
using Pm.Services.Telegram;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Pm.Services
#pragma warning restore IDE0130
{
    public class OperationalDocumentService(AppDbContext _context, ITelegramService _telegramService) : IOperationalDocumentService
    {
        public async Task<PagedResultDto<OperationalDocumentResponseDto>> GetAllAsync(OperationalDocumentQueryDto query)
        {
            var q = _context.OperationalDocuments.Include(d => d.BhpChecklists).AsSplitQuery().AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                q = q.Where(d => EF.Functions.Like(d.Name, $"%{query.Search}%") ||
                                 (d.ReferenceNumber != null && EF.Functions.Like(d.ReferenceNumber, $"%{query.Search}%")));
            }

            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                var types = query.Type.Split(',').ToList();
                q = q.Where(d => types.Contains(d.Type));
            }

            if (!string.IsNullOrWhiteSpace(query.GroupName))
            {
                var groups = query.GroupName.Split(',').ToList();
                q = q.Where(d => d.GroupName != null && groups.Contains(d.GroupName));
            }

            if (!string.IsNullOrWhiteSpace(query.FollowUpStatus))
            {
                var statuses = query.FollowUpStatus.Split(',').ToList();
                q = q.Where(d => statuses.Contains(d.FollowUpStatus));
            }

            // Expiry status filtering (On-the-fly logic)
            if (!string.IsNullOrWhiteSpace(query.ExpiryStatus))
            {
                var today = DateTime.UtcNow.Date;
                var warningDate = today.AddDays(30);
                var statuses = query.ExpiryStatus.Split(',').Select(s => s.Trim().ToLower()).ToList();

                var includeExpired = statuses.Contains("expired");
                var includeWarning = statuses.Contains("warning");
                var includeAman = statuses.Contains("aman");

                if (includeExpired || includeWarning || includeAman)
                {
                    q = q.Where(d =>
                        (includeExpired ? d.ValidUntil.Date < today : false) ||
                        (includeWarning ? (d.ValidUntil.Date >= today && d.ValidUntil.Date <= warningDate) : false) ||
                        (includeAman ? d.ValidUntil.Date > warningDate : false)
                    );
                }
            }

            var totalCount = await q.CountAsync();
            var sortField = string.IsNullOrWhiteSpace(query.SortBy) ? "ValidUntil" : query.SortBy;
            var sortDir  = string.IsNullOrWhiteSpace(query.SortDir) ? "asc" : query.SortDir;
            q = q.ApplySorting(sortField, sortDir);

            // Apply pagination
            var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

            List<OperationalDocumentResponseDto> dtos = [.. items.Select(MapToResponse)];
            return new PagedResultDto<OperationalDocumentResponseDto>(dtos, query, totalCount);
        }

        public async Task<OperationalDocumentSummaryDto> GetSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;
            var warningDate = today.AddDays(30);

            var q = _context.OperationalDocuments.AsNoTracking();

            var total = await q.CountAsync();
            var expired = await q.CountAsync(d => d.ValidUntil.Date < today);
            var warning = await q.CountAsync(d => d.ValidUntil.Date >= today && d.ValidUntil.Date <= warningDate);

            return new OperationalDocumentSummaryDto
            {
                TotalDocuments = total,
                Expired = expired,
                ExpiringSoon = warning
            };
        }

        public async Task<OperationalDocumentResponseDto> GetByIdAsync(int id)
        {
            var doc = await _context.OperationalDocuments.Include(d => d.BhpChecklists).AsNoTracking().FirstOrDefaultAsync(d => d.Id == id)
                      ?? throw new KeyNotFoundException("Dokumen tidak ditemukan.");
            return MapToResponse(doc);
        }

        public async Task<OperationalDocumentResponseDto> CreateAsync(OperationalDocumentCreateDto dto)
        {
            if (dto.ValidUntil <= dto.ValidFrom)
                throw new ArgumentException("Tanggal berakhir harus lebih besar dari tanggal berlaku.");

            var doc = new OperationalDocument
            {
                Name = dto.Name,
                Type = dto.Type,
                ReferenceNumber = dto.ReferenceNumber,
                GroupName = dto.GroupName,
                ValidFrom = dto.ValidFrom,
                ValidUntil = dto.ValidUntil,
                PicName = dto.PicName,
                PicTelegramId = dto.PicTelegramId,
                PicEmail = dto.PicEmail,
                FileLink = dto.FileLink,
                FollowUpStatus = "Tidak Ada"
            };

            await _context.OperationalDocuments.AddAsync(doc);
            await _context.SaveChangesAsync();
            await GenerateBhpChecklistAsync(doc);

            return MapToResponse(doc);
        }

        /// <summary>
        /// Upsert: cari dokumen berdasarkan Name + Type + ReferenceNumber.
        /// Jika sudah ada → update, jika belum → create baru.
        /// Digunakan oleh fitur Import Excel agar data tidak duplikat.
        /// </summary>
        public async Task<OperationalDocumentResponseDto> UpsertAsync(OperationalDocumentCreateDto dto)
        {
            if (dto.ValidUntil <= dto.ValidFrom)
                throw new ArgumentException("Tanggal berakhir harus lebih besar dari tanggal berlaku.");

            // Cari dokumen existing berdasarkan Name + Type + ReferenceNumber
            var existing = await _context.OperationalDocuments.FirstOrDefaultAsync(d =>
                d.Name == dto.Name &&
                d.Type == dto.Type &&
                d.ReferenceNumber == dto.ReferenceNumber);

            if (existing != null)
            {
                // UPDATE existing document
                existing.GroupName = dto.GroupName;
                // Jika tanggal berakhir berubah, reset follow up status
                if (existing.ValidUntil.Date != dto.ValidUntil.Date)
                {
                    existing.FollowUpStatus = "Tidak Ada";
                    existing.FollowUpRemark = null;
                }

                existing.ValidFrom = dto.ValidFrom;
                existing.ValidUntil = dto.ValidUntil;
                existing.PicName = dto.PicName;
                existing.PicTelegramId = dto.PicTelegramId;
                existing.PicEmail = dto.PicEmail;
                existing.FileLink = dto.FileLink;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await GenerateBhpChecklistAsync(existing);
                return MapToResponse(existing);
            }
            else
            {
                // CREATE new document
                var doc = new OperationalDocument
                {
                    Name = dto.Name,
                    Type = dto.Type,
                    ReferenceNumber = dto.ReferenceNumber,
                    GroupName = dto.GroupName,
                    ValidFrom = dto.ValidFrom,
                    ValidUntil = dto.ValidUntil,
                    PicName = dto.PicName,
                    PicTelegramId = dto.PicTelegramId,
                    PicEmail = dto.PicEmail,
                    FileLink = dto.FileLink,
                    FollowUpStatus = "Tidak Ada"
                };

                await _context.OperationalDocuments.AddAsync(doc);
                await _context.SaveChangesAsync();
                await GenerateBhpChecklistAsync(doc);
                return MapToResponse(doc);
            }
        }

        public async Task<OperationalDocumentResponseDto> UpdateAsync(int id, OperationalDocumentUpdateDto dto)
        {
            var doc = await _context.OperationalDocuments
                .Include(d => d.BhpChecklists)
                .FirstOrDefaultAsync(d => d.Id == id)
                      ?? throw new KeyNotFoundException("Dokumen tidak ditemukan.");

            if (dto.ValidUntil <= dto.ValidFrom)
                throw new ArgumentException("Tanggal berakhir harus lebih besar dari tanggal berlaku.");

            doc.Name = dto.Name;
            doc.Type = dto.Type;
            doc.ReferenceNumber = dto.ReferenceNumber;
            doc.GroupName = dto.GroupName;
            // If expiry date changes, reset the follow up status
            if (doc.ValidUntil.Date != dto.ValidUntil.Date)
            {
                doc.FollowUpStatus = "Tidak Ada";
                doc.FollowUpRemark = null;
            }
            doc.ValidFrom = dto.ValidFrom;
            doc.ValidUntil = dto.ValidUntil;
            doc.PicName = dto.PicName;
            doc.PicTelegramId = dto.PicTelegramId;
            doc.PicEmail = dto.PicEmail;
            doc.FileLink = dto.FileLink;
            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await GenerateBhpChecklistAsync(doc);
            return MapToResponse(doc);
        }

        public async Task<OperationalDocumentResponseDto> UpdateFollowUpStatusAsync(int id, string status, string? remark = null)
        {
            var allowedStatuses = new[] { "Tidak Ada", "Pending", "SedangDiproses", "Selesai" };
            if (!allowedStatuses.Contains(status))
                throw new ArgumentException("Status tidak valid.");

            var doc = await _context.OperationalDocuments.FirstOrDefaultAsync(d => d.Id == id)
                      ?? throw new KeyNotFoundException("Dokumen tidak ditemukan.");

            doc.FollowUpStatus = status;
            doc.FollowUpRemark = remark;
            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToResponse(doc);
        }

        public async Task DeleteAsync(int id)
        {
            var doc = await _context.OperationalDocuments.FirstOrDefaultAsync(d => d.Id == id)
                      ?? throw new KeyNotFoundException("Dokumen tidak ditemukan.");

            _context.OperationalDocuments.Remove(doc);
            await _context.SaveChangesAsync();
        }

        public async Task<OperationalDocumentResponseDto> MarkBhpPaymentAsync(int id, int year, string invoiceNumber, string userName)
        {
            var doc = await _context.OperationalDocuments
                .Include(d => d.BhpChecklists)
                .FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new KeyNotFoundException("Dokumen tidak ditemukan.");

            var checklist = doc.BhpChecklists.FirstOrDefault(c => c.Year == year);
            if (checklist == null)
            {
                checklist = new BhpPaymentChecklist { OperationalDocumentId = id, Year = year };
                _context.BhpPaymentChecklists.Add(checklist);
                doc.BhpChecklists.Add(checklist);
            }

            checklist.IsPaid = true;
            checklist.InvoiceNumber = invoiceNumber;
            checklist.PaidAt = DateTime.UtcNow;
            checklist.PaidByUserName = userName;

            await _context.SaveChangesAsync();

            // Kirim notif Telegram ke semua PIC jika ada
            if (!string.IsNullOrWhiteSpace(doc.PicTelegramId))
            {
                var paidCount = doc.BhpChecklists.Count(c => c.IsPaid);
                var totalCount = doc.BhpChecklists.Count;
                var isAllPaid = paidCount == totalCount && totalCount > 0;

                var chatIds = doc.PicTelegramId.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var chatId in chatIds)
                {
                    await _telegramService.SendBhpPaymentConfirmationAsync(
                        chatId, doc.Name, year, invoiceNumber, userName, isAllPaid, paidCount, totalCount);
                }
            }

            return MapToResponse(doc);
        }

        public async Task<OperationalDocumentResponseDto> UnmarkBhpPaymentAsync(int id, int year)
        {
            var doc = await _context.OperationalDocuments
                .Include(d => d.BhpChecklists)
                .FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new KeyNotFoundException("Dokumen tidak ditemukan.");

            var checklist = doc.BhpChecklists.FirstOrDefault(c => c.Year == year);
            if (checklist != null)
            {
                checklist.IsPaid = false;
                checklist.InvoiceNumber = null;
                checklist.PaidAt = null;
                checklist.PaidByUserName = null;
                await _context.SaveChangesAsync();
            }

            return MapToResponse(doc);
        }

        private static OperationalDocumentResponseDto MapToResponse(OperationalDocument doc)
        {
            var isIsr = doc.Type?.Contains("ISR", StringComparison.OrdinalIgnoreCase) is true;
            var (bhpChecklist, bhpPaidCount, bhpTotalCount) = isIsr
                ? BuildBhpChecklistResponse(doc)
                : (null, null, null);

            return new OperationalDocumentResponseDto
            {
                Id = doc.Id,
                Name = doc.Name,
                Type = doc.Type ?? "",
                ReferenceNumber = doc.ReferenceNumber,
                GroupName = doc.GroupName,
                ValidFrom = doc.ValidFrom,
                ValidUntil = doc.ValidUntil,
                PicName = doc.PicName,
                PicTelegramId = doc.PicTelegramId,
                PicEmail = doc.PicEmail,
                FileLink = doc.FileLink,
                FollowUpStatus = doc.FollowUpStatus,
                FollowUpRemark = doc.FollowUpRemark,
                CreatedAt = doc.CreatedAt,
                UpdatedAt = doc.UpdatedAt,
                BhpChecklist = bhpChecklist,
                BhpPaidCount = bhpPaidCount,
                BhpTotalCount = bhpTotalCount
            };
        }

        private static (List<BhpPaymentChecklistItemDto>? checklist, int? paidCount, int? totalCount) BuildBhpChecklistResponse(OperationalDocument doc)
        {
            if (doc.Type?.Contains("ISR", StringComparison.OrdinalIgnoreCase) is not true)
            {
                return (null, null, null);
            }

            var dbChecklists = doc.BhpChecklists?.ToList() ?? [];
            var currentYear = DateTime.UtcNow.Year;

            var validFrom = doc.ValidFrom.Year >= 2000
                ? doc.ValidFrom
                : (doc.ValidUntil.Year >= 2000 ? doc.ValidUntil.AddYears(-4) : DateTime.UtcNow);

            var validUntil = doc.ValidUntil.Year >= 2000
                ? doc.ValidUntil
                : validFrom.AddYears(4);

            int startYear = validFrom.Year + 1;
            if (startYear > validUntil.Year && validUntil.Year >= validFrom.Year)
            {
                startYear = validFrom.Year;
            }

            if (startYear < 2000) startYear = 2000;

            var resultList = new List<BhpPaymentChecklistItemDto>();

            if (validUntil.Year >= startYear)
            {
                for (int year = startYear; year <= validUntil.Year; year++)
                {
                    var existing = dbChecklists.FirstOrDefault(c => c.Year == year);
                    if (existing != null)
                    {
                        resultList.Add(new BhpPaymentChecklistItemDto
                        {
                            Id = existing.Id,
                            Year = existing.Year,
                            IsPaid = existing.IsPaid,
                            InvoiceNumber = existing.InvoiceNumber,
                            PaidAt = existing.PaidAt,
                            PaidByUserName = existing.PaidByUserName
                        });
                    }
                    else
                    {
                        var isPast = year < currentYear;
                        resultList.Add(new BhpPaymentChecklistItemDto
                        {
                            Id = 0,
                            Year = year,
                            IsPaid = isPast,
                            InvoiceNumber = isPast ? "Data Migrasi" : null,
                            PaidAt = isPast ? DateTime.UtcNow : null,
                            PaidByUserName = isPast ? "System" : null
                        });
                    }
                }
            }

            // Include any existing db checklists outside expected range if any
            foreach (var existing in dbChecklists)
            {
                if (!resultList.Any(r => r.Year == existing.Year))
                {
                    resultList.Add(new BhpPaymentChecklistItemDto
                    {
                        Id = existing.Id,
                        Year = existing.Year,
                        IsPaid = existing.IsPaid,
                        InvoiceNumber = existing.InvoiceNumber,
                        PaidAt = existing.PaidAt,
                        PaidByUserName = existing.PaidByUserName
                    });
                }
            }

            resultList = [.. resultList.OrderBy(r => r.Year)];
            int paidCount = resultList.Count(r => r.IsPaid);
            int totalCount = resultList.Count;

            return (resultList, paidCount, totalCount);
        }

        private async Task GenerateBhpChecklistAsync(OperationalDocument doc)
        {
            if (EnsureBhpChecklistForDoc(doc, _context))
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<(int processedCount, int generatedCount)> BackfillBhpChecklistsAsync()
        {
            var isrDocs = await _context.OperationalDocuments
                .Include(d => d.BhpChecklists)
                .Where(d => d.Type != null && d.Type.Contains("ISR"))
                .ToListAsync();

            int processedCount = 0;
            int generatedCount = 0;

            foreach (var doc in isrDocs)
            {
                int countBefore = doc.BhpChecklists?.Count ?? 0;
                if (EnsureBhpChecklistForDoc(doc, _context))
                {
                    processedCount++;
                    generatedCount += (doc.BhpChecklists?.Count ?? 0) - countBefore;
                }
            }

            if (generatedCount > 0)
                await _context.SaveChangesAsync();

            return (processedCount, generatedCount);
        }

        private static bool EnsureBhpChecklistForDoc(OperationalDocument doc, AppDbContext context)
        {
            if (doc.Type?.Contains("ISR", StringComparison.OrdinalIgnoreCase) is not true)
                return false;

            var existingYears = doc.BhpChecklists?.Select(c => c.Year).ToHashSet() ?? [];
            var currentYear = DateTime.UtcNow.Year;
            bool addedAny = false;

            var validFrom = doc.ValidFrom.Year >= 2000
                ? doc.ValidFrom
                : (doc.ValidUntil.Year >= 2000 ? doc.ValidUntil.AddYears(-4) : DateTime.UtcNow);

            var validUntil = doc.ValidUntil.Year >= 2000
                ? doc.ValidUntil
                : validFrom.AddYears(4);

            int startYear = validFrom.Year + 1;
            if (startYear > validUntil.Year && validUntil.Year >= validFrom.Year)
            {
                startYear = validFrom.Year;
            }

            if (startYear < 2000) startYear = 2000;
            if (validUntil.Year < startYear) return false;

            for (int year = startYear; year <= validUntil.Year; year++)
            {
                if (!existingYears.Contains(year))
                {
                    var isPast = year < currentYear;
                    var checklist = new BhpPaymentChecklist
                    {
                        OperationalDocumentId = doc.Id,
                        Year = year,
                        IsPaid = isPast,
                        InvoiceNumber = isPast ? "Data Migrasi" : null,
                        PaidAt = isPast ? DateTime.UtcNow : null,
                        PaidByUserName = isPast ? "System" : null
                    };
                    context.BhpPaymentChecklists.Add(checklist);
                    doc.BhpChecklists ??= [];
                    doc.BhpChecklists.Add(checklist);
                    addedAny = true;
                }
            }

            return addedAny;
        }
    }
}
