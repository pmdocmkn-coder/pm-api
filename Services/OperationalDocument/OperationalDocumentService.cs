using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Models;

namespace Pm.Services
{
    public class OperationalDocumentService(AppDbContext _context) : IOperationalDocumentService
    {
        public async Task<PagedResultDto<OperationalDocumentResponseDto>> GetAllAsync(OperationalDocumentQueryDto query)
        {
            var q = _context.OperationalDocuments.AsNoTracking().AsQueryable();

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
            var doc = await _context.OperationalDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id)
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

            return MapToResponse(doc);
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
                UpdatedAt = doc.UpdatedAt
            };
        }
    }
}
