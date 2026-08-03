using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Models;
using Pm.Services.Telegram;

namespace Pm.Services
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
                q = q.Where(d => d.Type == query.Type);
            }

            if (!string.IsNullOrWhiteSpace(query.GroupName))
            {
                q = q.Where(d => d.GroupName != null && EF.Functions.Like(d.GroupName, $"%{query.GroupName}%"));
            }

            if (!string.IsNullOrWhiteSpace(query.FollowUpStatus))
            {
                q = q.Where(d => d.FollowUpStatus == query.FollowUpStatus);
            }

            // Expiry status filtering (On-the-fly logic)
            if (!string.IsNullOrWhiteSpace(query.ExpiryStatus))
            {
                var today = DateTime.UtcNow.Date;
                if (query.ExpiryStatus.Equals("Expired", StringComparison.OrdinalIgnoreCase))
                {
                    q = q.Where(d => d.ValidUntil.Date < today);
                }
                else if (query.ExpiryStatus.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                {
                    var warningDate = today.AddDays(30);
                    q = q.Where(d => d.ValidUntil.Date >= today && d.ValidUntil.Date <= warningDate);
                }
                else if (query.ExpiryStatus.Equals("Aman", StringComparison.OrdinalIgnoreCase))
                {
                    var warningDate = today.AddDays(30);
                    q = q.Where(d => d.ValidUntil.Date > warningDate);
                }
            }

            var totalCount = await q.CountAsync();
            var sortField = string.IsNullOrWhiteSpace(query.SortBy) ? "ValidUntil" : query.SortBy;
            var sortDir  = string.IsNullOrWhiteSpace(query.SortDir) ? "asc" : query.SortDir;
            q = q.ApplySorting(sortField, sortDir);

            // Apply pagination
            var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();

            var dtos = items.Select(MapToResponse).ToList();
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
            var doc = await _context.OperationalDocuments.FirstOrDefaultAsync(d => d.Id == id)
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
            return new OperationalDocumentResponseDto
            {
                Id = doc.Id,
                Name = doc.Name,
                Type = doc.Type,
                ReferenceNumber = doc.ReferenceNumber,
                GroupName = doc.GroupName,
                ValidFrom = doc.ValidFrom,
                ValidUntil = doc.ValidUntil,
                PicName = doc.PicName,
                PicTelegramId = doc.PicTelegramId,
                FileLink = doc.FileLink,
                FollowUpStatus = doc.FollowUpStatus,
                FollowUpRemark = doc.FollowUpRemark,
                CreatedAt = doc.CreatedAt,
                UpdatedAt = doc.UpdatedAt,
                BhpChecklist = doc.BhpChecklists?.Select(c => new BhpPaymentChecklistItemDto
                {
                    Id = c.Id,
                    Year = c.Year,
                    IsPaid = c.IsPaid,
                    InvoiceNumber = c.InvoiceNumber,
                    PaidAt = c.PaidAt,
                    PaidByUserName = c.PaidByUserName
                }).OrderBy(c => c.Year).ToList(),
                BhpPaidCount = doc.Type != null && doc.Type.Contains("ISR", StringComparison.OrdinalIgnoreCase) 
                    ? doc.BhpChecklists?.Count(c => c.IsPaid) ?? 0 : null,
                BhpTotalCount = doc.Type != null && doc.Type.Contains("ISR", StringComparison.OrdinalIgnoreCase) 
                    ? doc.BhpChecklists?.Count ?? 0 : null
            };
        }

        private async Task GenerateBhpChecklistAsync(OperationalDocument doc)
        {
            if (doc.Type == null || !doc.Type.Contains("ISR", StringComparison.OrdinalIgnoreCase))
                return;

            var existingChecklists = await _context.BhpPaymentChecklists
                .Where(c => c.OperationalDocumentId == doc.Id)
                .ToListAsync();

            var currentYear = DateTime.UtcNow.Year;
            bool addedAny = false;

            // ValidFrom.Year+1 s/d ValidUntil.Year (inklusif) — semua tahun aktif ISR wajib bayar BHP
            for (int year = doc.ValidFrom.Year + 1; year <= doc.ValidUntil.Year; year++)
            {
                if (!existingChecklists.Any(c => c.Year == year))
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
                    _context.BhpPaymentChecklists.Add(checklist);
                    doc.BhpChecklists.Add(checklist);
                    addedAny = true;
                }
            }

            if (addedAny)
                await _context.SaveChangesAsync();
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
                var existingYears = doc.BhpChecklists.Select(c => c.Year).ToHashSet();
                var currentYear = DateTime.UtcNow.Year;
                bool addedAny = false;

                // ValidFrom.Year+1 s/d ValidUntil.Year (inklusif)
                for (int year = doc.ValidFrom.Year + 1; year <= doc.ValidUntil.Year; year++)
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
                        _context.BhpPaymentChecklists.Add(checklist);
                        doc.BhpChecklists.Add(checklist);
                        generatedCount++;
                        addedAny = true;
                    }
                }

                if (addedAny)
                    processedCount++;
            }

            if (generatedCount > 0)
                await _context.SaveChangesAsync();

            return (processedCount, generatedCount);
        }
    }
}
