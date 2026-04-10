using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.DTOs.KpiDocument;
using Pm.Models;
using Pm.Helper;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Pm.Services
{
    public class KpiDocumentService : IKpiDocumentService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;

        public KpiDocumentService(AppDbContext context, IActivityLogService activityLog)
        {
            _context = context;
            _activityLog = activityLog;
        }

        private string DetermineStatus(KpiDocument entity)
        {
            if (entity.DateSubmittedToRqm.HasValue)
                return "Selesai (Submitted RQM)";
            
            if (entity.DateApproved.HasValue)
                return "Approved";
                
            if (entity.DateSubmittedToReviewer.HasValue)
            {
                // Mengecek ke judul grup (AreaGroup), misal: "BAO VIA EMAIL"
                if (!string.IsNullOrEmpty(entity.AreaGroup) && entity.AreaGroup.ToUpper().Contains("EMAIL"))
                {
                    return "Menunggu Balasan (Email)";
                }
                return "Menunggu Sign (Office)";
            }
                
            if (entity.DateReceived.HasValue)
                return "Data Diterima";
                
            return "Menunggu Data";
        }

        private KpiDocumentDto MapToDto(KpiDocument entity)
        {
            return new KpiDocumentDto
            {
                Id = entity.Id,
                PeriodMonth = entity.PeriodMonth,
                AreaGroup = entity.AreaGroup,
                DocumentName = entity.DocumentName,
                DataSource = entity.DataSource,
                GroupTag = entity.GroupTag,
                DateReceived = entity.DateReceived,
                DateSubmittedToReviewer = entity.DateSubmittedToReviewer,
                DateApproved = entity.DateApproved,
                DateSubmittedToRqm = entity.DateSubmittedToRqm,
                Remarks = entity.Remarks ?? null,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Status = DetermineStatus(entity)
            };
        }

        public async Task<PagedResultDto<KpiDocumentDto>> GetAllAsync(KpiDocumentQueryDto queryDto)
        {
            var query = _context.KpiDocuments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryDto.PeriodMonth) && DateTime.TryParse(queryDto.PeriodMonth, out var parsedMonth))
            {
                query = query.Where(k => k.PeriodMonth.Year == parsedMonth.Year && k.PeriodMonth.Month == parsedMonth.Month);
            }
            else
            {
                var now = DateTime.UtcNow;
                query = query.Where(k => k.PeriodMonth.Year == now.Year && k.PeriodMonth.Month == now.Month);
            }

            if (!string.IsNullOrWhiteSpace(queryDto.AreaGroup))
            {
                query = query.Where(k => k.AreaGroup == queryDto.AreaGroup);
            }

            if (!string.IsNullOrWhiteSpace(queryDto.Search))
            {
                var lowerSearch = queryDto.Search.ToLower();
                query = query.Where(k => 
                    k.DocumentName.ToLower().Contains(lowerSearch) || 
                    k.DataSource.ToLower().Contains(lowerSearch)
                );
            }

            // Default order by AreaGroup to maintain visual sections, then by Id
            if (string.IsNullOrWhiteSpace(queryDto.SortBy))
            {
                query = query.OrderBy(k => k.AreaGroup).ThenBy(k => k.Id);
            }
            else
            {
                query = query.ApplySorting(queryDto.SortBy, queryDto.SortDir);
            }

            var totalCount = await query.CountAsync();
            var items = await query.Skip((queryDto.Page - 1) * queryDto.PageSize).Take(queryDto.PageSize).ToListAsync();

            var data = items.Select(MapToDto).ToList();

            return new PagedResultDto<KpiDocumentDto>(data, queryDto.Page, queryDto.PageSize, totalCount);
        }

        public async Task<KpiDocumentDto> CreateAsync(CreateKpiDocumentDto dto, int userId)
        {
            var entity = new KpiDocument
            {
                PeriodMonth = new DateTime(dto.PeriodMonth.Year, dto.PeriodMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                AreaGroup = dto.AreaGroup,
                DocumentName = dto.DocumentName,
                DataSource = dto.DataSource,
                GroupTag = string.IsNullOrWhiteSpace(dto.GroupTag) ? null : dto.GroupTag.Trim(),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.KpiDocuments.Add(entity);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("KPI Document", entity.Id, "Create", userId, $"Created document tracking '{entity.DocumentName}' for {entity.PeriodMonth:MMM yyyy}");

            return MapToDto(entity);
        }

        public async Task<KpiDocumentDto> UpdateAsync(int id, UpdateKpiDocumentDto dto, int userId)
        {
            var entity = await _context.KpiDocuments.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Dokumen tidak ditemukan");

            entity.AreaGroup = dto.AreaGroup;
            entity.DocumentName = dto.DocumentName;
            entity.DataSource = dto.DataSource;
            entity.GroupTag = string.IsNullOrWhiteSpace(dto.GroupTag) ? null : dto.GroupTag.Trim();
            entity.Remarks = dto.Remarks;
            
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId;

            await _context.SaveChangesAsync();
            await _activityLog.LogAsync("KPI Document", entity.Id, "Update Info", userId, $"Updated document tracking info '{entity.DocumentName}'");

            return MapToDto(entity);
        }

        public async Task<KpiDocumentDto> UpdateDatesAsync(int id, UpdateKpiDocumentDatesDto dto, int userId)
        {
            var entity = await _context.KpiDocuments.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Dokumen tidak ditemukan");

            entity.DateReceived = dto.DateReceived;
            entity.DateSubmittedToReviewer = dto.DateSubmittedToReviewer;
            entity.DateApproved = dto.DateApproved;
            entity.DateSubmittedToRqm = dto.DateSubmittedToRqm;
            if (dto.Remarks != null) entity.Remarks = dto.Remarks;

            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId;

            await _context.SaveChangesAsync();
            await _activityLog.LogAsync("KPI Document", entity.Id, "Update Progress", userId, $"Updated progress/dates for '{entity.DocumentName}'");

            return MapToDto(entity);
        }

        public async Task DeleteAsync(int id, int userId)
        {
            var entity = await _context.KpiDocuments.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Dokumen tidak ditemukan");

            _context.KpiDocuments.Remove(entity);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("KPI Document", id, "Delete", userId, $"Deleted tracking '{entity.DocumentName}'");
        }

        public async Task<List<KpiDocumentDto>> CloneFromPreviousMonthAsync(DateTime sourceMonth, DateTime targetMonth, int userId)
        {
            var sourceDate = new DateTime(sourceMonth.Year, sourceMonth.Month, 1);
            var sourceItems = await _context.KpiDocuments
                .Where(k => k.PeriodMonth.Year == sourceDate.Year && k.PeriodMonth.Month == sourceDate.Month)
                .ToListAsync();

            if (!sourceItems.Any())
                throw new InvalidOperationException($"Tidak ada data pada bulan {sourceDate:MMM yyyy} untuk disalin.");

            var targetDate = new DateTime(targetMonth.Year, targetMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            
            // Periksa apakah bulan target sudah ada data
            var existingTarget = await _context.KpiDocuments
                .AnyAsync(k => k.PeriodMonth.Year == targetDate.Year && k.PeriodMonth.Month == targetDate.Month);
                
            if (existingTarget)
                throw new InvalidOperationException($"Bulan {targetDate:MMM yyyy} sudah memiliki data. Tidak bisa menyalin ulang.");

            var newItems = new List<KpiDocument>();

            foreach (var item in sourceItems)
            {
                newItems.Add(new KpiDocument
                {
                    PeriodMonth = targetDate,
                    AreaGroup = item.AreaGroup,
                    DocumentName = item.DocumentName,
                    DataSource = item.DataSource,
                    // Seluruh tanggal dikosongkan untuk bulan baru
                    DateReceived = null,
                    DateSubmittedToReviewer = null,
                    DateApproved = null,
                    DateSubmittedToRqm = null,
                    Remarks = null, // Remarks juga dikosongkan karena spesifik status
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.KpiDocuments.AddRange(newItems);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("KPI Document", 0, "Clone", userId, $"Cloned {newItems.Count} items from {sourceDate:MMM yyyy} to {targetDate:MMM yyyy}");

            return newItems.Select(MapToDto).ToList();
        }

        public async Task<byte[]> ExportExcelAsync(KpiDocumentQueryDto queryDto)
        {
            var query = _context.KpiDocuments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryDto.PeriodMonth) && DateTime.TryParse(queryDto.PeriodMonth, out var parsedMonth))
                query = query.Where(k => k.PeriodMonth.Year == parsedMonth.Year && k.PeriodMonth.Month == parsedMonth.Month);

            if (!string.IsNullOrWhiteSpace(queryDto.AreaGroup))
                query = query.Where(k => k.AreaGroup == queryDto.AreaGroup);

            if (!string.IsNullOrWhiteSpace(queryDto.Search))
            {
                var lowerSearch = queryDto.Search.ToLower();
                query = query.Where(k => 
                    k.DocumentName.ToLower().Contains(lowerSearch) || 
                    k.DataSource.ToLower().Contains(lowerSearch)
                );
            }

            if (string.IsNullOrWhiteSpace(queryDto.SortBy))
                query = query.OrderBy(k => k.AreaGroup).ThenBy(k => k.Id);
            else
                query = query.ApplySorting(queryDto.SortBy, queryDto.SortDir);

            var items = await query.ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("KPI Tracking");

            var headers = new[] { "No", "Area/Group", "Nama Dokumen", "Asal Data", "Periode", "Date Received", "Submitted To User", "Approved By User", "Submitted RQM", "Status", "Remarks" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            for (int i = 0; i < items.Count; i++)
            {
                var row = i + 2;
                var item = items[i];
                
                worksheet.Cells[row, 1].Value = i + 1;
                worksheet.Cells[row, 2].Value = item.AreaGroup;
                worksheet.Cells[row, 3].Value = item.DocumentName;
                worksheet.Cells[row, 4].Value = item.DataSource;
                worksheet.Cells[row, 5].Value = item.PeriodMonth.ToString("MMM yyyy");
                worksheet.Cells[row, 6].Value = item.DateReceived?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 7].Value = item.DateSubmittedToReviewer?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 8].Value = item.DateApproved?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 9].Value = item.DateSubmittedToRqm?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 10].Value = DetermineStatus(item);
                worksheet.Cells[row, 11].Value = item.Remarks;

                // Add simple borders
                for(int col = 1; col <= headers.Length; col++)
                {
                    worksheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }

        public async Task<int> ImportExcelAsync(Microsoft.AspNetCore.Http.IFormFile file, int userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File tidak valid.");

            using var stream = new System.IO.MemoryStream();
            await file.CopyToAsync(stream);

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            
            if (worksheet == null)
                throw new ArgumentException("Worksheet tidak ditemukan di dalam Excel.");

            int lastRow = worksheet.Dimension.Rows;
            if (lastRow < 2) return 0; // Hanya ada header

            var newItems = new List<KpiDocument>();

            for (int row = 2; row <= lastRow; row++)
            {
                var areaGroup = worksheet.Cells[row, 1].Text?.Trim();
                var documentName = worksheet.Cells[row, 2].Text?.Trim();
                var dataSource = worksheet.Cells[row, 3].Text?.Trim();
                var periodStr = worksheet.Cells[row, 4].Text?.Trim(); // Membaca string tanggal misal "2024-05"

                if (string.IsNullOrWhiteSpace(documentName)) continue;

                DateTime periodDate = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(periodStr) && DateTime.TryParse(periodStr, out var parsed))
                {
                    periodDate = parsed;
                }

                periodDate = new DateTime(periodDate.Year, periodDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                newItems.Add(new KpiDocument
                {
                    AreaGroup = string.IsNullOrWhiteSpace(areaGroup) ? "GENERAL" : areaGroup,
                    DocumentName = documentName,
                    DataSource = string.IsNullOrWhiteSpace(dataSource) ? "Unknown" : dataSource,
                    PeriodMonth = periodDate,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                });
            }

            if (newItems.Any())
            {
                _context.KpiDocuments.AddRange(newItems);
                await _context.SaveChangesAsync();
                await _activityLog.LogAsync("KPI Document", 0, "Import", userId, $"Imported {newItems.Count} KPI templates from Excel");
            }

            return newItems.Count;
        }
    }
}
