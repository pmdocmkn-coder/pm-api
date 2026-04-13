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

            if (entity.DateApproved.HasValue && !string.IsNullOrEmpty(entity.Remarks) && entity.Remarks.ToUpper().Contains("TIDAK SUBMIT"))
                return "Selesai (Approved)";

            if (entity.DateApproved.HasValue)
                return "Approved";

            if (entity.DateSubmittedToReviewer.HasValue)
            {
                // Mengecek ke judul grup (AreaGroup), misal: "BAO VIA EMAIL"
                if (!string.IsNullOrEmpty(entity.AreaGroup) && entity.AreaGroup.ToUpper().Contains("EMAIL"))
                {
                    return "Menunggu Sign User ( Email )";
                }
                return "Menunggu Sign User";
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
                    GroupTag = item.GroupTag,
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

        public async Task DeleteMonthDataAsync(DateTime targetMonth, int userId)
        {
            var targetDate = new DateTime(targetMonth.Year, targetMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var items = await _context.KpiDocuments
                .Where(k => k.PeriodMonth.Year == targetDate.Year && k.PeriodMonth.Month == targetDate.Month)
                .ToListAsync();

            if (!items.Any()) return;

            // Periksa apakah sudah ada data yang diproses
            var hasProcessedData = items.Any(k =>
                k.DateReceived != null ||
                k.DateSubmittedToReviewer != null ||
                k.DateApproved != null ||
                k.DateSubmittedToRqm != null ||
                !string.IsNullOrWhiteSpace(k.Remarks));

            if (hasProcessedData)
            {
                throw new InvalidOperationException("Tidak bisa menghapus data bulan ini karena sudah ada dokumen yang diisi progres/tanggal.");
            }

            _context.KpiDocuments.RemoveRange(items);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("KPI Document", 0, "Delete Month", userId, $"Deleted all {items.Count} items for {targetDate:MMM yyyy}");
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
                query = query.OrderBy(k => k.AreaGroup).ThenBy(k => k.GroupTag).ThenBy(k => k.Id);
            else
                query = query.ApplySorting(queryDto.SortBy, queryDto.SortDir);

            var items = await query.ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("KPI Tracking");

            // Columns (10 total, NO Periode, NO Area/Group column):
            // 1=No | 2=Nama Dokumen | 3=Asal Data | 4=Date Received
            // 5=Submitted To User | 6=Approved By User | 7=Submitted RQM | 8=Status | 9=Remarks
            const int TOTAL_COLS = 9;
            var headers = new[] { "No", "Nama Dokumen", "Asal Data", "Date Received",
                                   "Submitted To User", "Approved By User", "Submitted RQM", "Status", "Remarks" };

            for (int c = 0; c < headers.Length; c++)
            {
                var hCell = ws.Cells[1, c + 1];
                hCell.Value = headers[c];
                hCell.Style.Font.Bold = true;
                hCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                hCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(63, 81, 181));
                hCell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                hCell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                hCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                hCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                hCell.Style.WrapText = true;
            }
            ws.Row(1).Height = 28;

            int row = 2;
            int no = 1;

            // ─── Helpers ─────────────────────────────────────────────────────────────
            List<KpiDocument> SortItems(IEnumerable<KpiDocument> src) =>
                src.OrderBy(a => string.IsNullOrWhiteSpace(a.GroupTag) ? 1 : 0)
                   .ThenBy(a => a.GroupTag ?? "")
                   .ThenBy(a => a.Id)
                   .ToList();

            void WriteCell(int r, int col, object? value, bool center = false)
            {
                var cell = ws.Cells[r, col];
                cell.Value = value;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                if (center) cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Write an Area/Group separator row (like indigo header in web)
            void WriteAreaHeader(string areaName)
            {
                // Merge all 9 columns into one row
                ws.Cells[row, 1, row, TOTAL_COLS].Merge = true;
                var areaCell = ws.Cells[row, 1];
                areaCell.Value = areaName.ToUpper();
                areaCell.Style.Font.Bold = true;
                areaCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                areaCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(232, 234, 246)); // light indigo
                areaCell.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(40, 53, 147)); // dark indigo text
                areaCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                areaCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                areaCell.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                areaCell.Style.Font.Size = 9;
                ws.Row(row).Height = 18;
                row++;
            }

            // ─── Group by AreaGroup ───────────────────────────────────────────────────
            var byArea = items.GroupBy(k => k.AreaGroup).OrderBy(g => g.Key);

            foreach (var areaGrp in byArea)
            {
                bool isGeneral = (areaGrp.Key ?? "").ToUpper() == "GENERAL";
                var sorted = SortItems(areaGrp);

                // Area/Group as separator row (matches web indigo header row)
                WriteAreaHeader(areaGrp.Key ?? "");

                // ═══════════════════════════════════════════════════════════════════
                // STANDARD GROUP (non-GENERAL) — mirrors processGroupsForMerge()
                // Merge: No + Asal Data per tag-group. ALL dates INDIVIDUAL per row.
                // ═══════════════════════════════════════════════════════════════════
                if (!isGeneral)
                {
                    var stdGroups = new List<KpiExportGroup>();
                    foreach (var item in sorted)
                    {
                        var tag = string.IsNullOrWhiteSpace(item.GroupTag) ? null : item.GroupTag.Trim();
                        if (tag != null)
                        {
                            var existing = stdGroups.Find(g => g.Tag == tag);
                            if (existing != null) { existing.Items.Add(item); continue; }
                        }
                        stdGroups.Add(new KpiExportGroup { Tag = tag, Items = new List<KpiDocument> { item } });
                    }

                    foreach (var grp in stdGroups)
                    {
                        int grpRowStart = row;
                        var asalData = grp.Items[0].DataSource;

                        foreach (var item in grp.Items)
                        {
                            WriteCell(row, 1, no, center: true);          // No
                            WriteCell(row, 2, item.DocumentName);          // Nama Dokumen
                            WriteCell(row, 3, asalData, center: true);                   // Asal Data (same for all in group)
                            WriteCell(row, 4, item.DateReceived?.ToString("dd/MM/yyyy"), center: true);
                            WriteCell(row, 5, item.DateSubmittedToReviewer?.ToString("dd/MM/yyyy"), center: true);
                            WriteCell(row, 6, item.DateApproved?.ToString("dd/MM/yyyy"), center: true);
                            WriteCell(row, 7, item.DateSubmittedToRqm?.ToString("dd/MM/yyyy"), center: true);
                            WriteCell(row, 8, DetermineStatus(item), center: true);
                            WriteCell(row, 9, item.Remarks, center: true);
                            row++;
                        }

                        if (grp.Items.Count > 1)
                        {
                            MergeCells(ws, grpRowStart, row - 1, 1); // No
                            MergeCells(ws, grpRowStart, row - 1, 3); // Asal Data
                        }
                        no++;
                    }
                }
                // ═══════════════════════════════════════════════════════════════════
                // GENERAL GROUP — mirrors processMultiLevelGroups()
                // No merged per topGroup. Asal Data merged per subGroup.
                // Submitted/Approved/RQM/Status MERGED per topGroup (from refDoc).
                // ═══════════════════════════════════════════════════════════════════
                else
                {
                    var topGroups = new List<KpiExportTopGroup>();
                    foreach (var item in sorted)
                    {
                        var tag = string.IsNullOrWhiteSpace(item.GroupTag) ? null : item.GroupTag.Trim();
                        var uniqueKey = tag != null ? $"TAG_{tag}" : $"ID_{item.Id}";

                        var existingTop = topGroups.Find(g => g.GroupKey == uniqueKey);
                        if (existingTop == null)
                        {
                            existingTop = new KpiExportTopGroup { GroupKey = uniqueKey, Tag = tag };
                            topGroups.Add(existingTop);
                        }

                        var dsKey = (item.DataSource ?? "").ToLowerInvariant();
                        var existingSub = existingTop.SubGroups.Find(s => (s.DataSource ?? "").ToLowerInvariant() == dsKey);
                        if (existingSub == null)
                        {
                            existingSub = new KpiExportSubGroup { DataSource = item.DataSource ?? "" };
                            existingTop.SubGroups.Add(existingSub);
                        }
                        existingSub.Items.Add(item);
                    }

                    foreach (var topGroup in topGroups)
                    {
                        int topRowStart = row;
                        int totalItems = topGroup.SubGroups.Sum(s => s.Items.Count);
                        var refDoc = topGroup.SubGroups[0].Items[0];

                        foreach (var subGrp in topGroup.SubGroups)
                        {
                            int subRowStart = row;
                            foreach (var item in subGrp.Items)
                            {
                                WriteCell(row, 1, no, center: true);
                                WriteCell(row, 2, item.DocumentName);
                                WriteCell(row, 3, subGrp.DataSource, center: true);
                                WriteCell(row, 4, item.DateReceived?.ToString("dd/MM/yyyy"), center: true);
                                WriteCell(row, 5, refDoc.DateSubmittedToReviewer?.ToString("dd/MM/yyyy"), center: true);
                                WriteCell(row, 6, refDoc.DateApproved?.ToString("dd/MM/yyyy"), center: true);
                                WriteCell(row, 7, refDoc.DateSubmittedToRqm?.ToString("dd/MM/yyyy"), center: true);
                                WriteCell(row, 8, DetermineStatus(refDoc), center: true);
                                WriteCell(row, 9, refDoc.Remarks);
                                row++;
                            }

                            if (subGrp.Items.Count > 1)
                                MergeCells(ws, subRowStart, row - 1, 3); // Asal Data per subGroup
                        }

                        if (totalItems > 1)
                        {
                            MergeCells(ws, topRowStart, row - 1, 1); // No
                            MergeCells(ws, topRowStart, row - 1, 5); // Submitted To User
                            MergeCells(ws, topRowStart, row - 1, 6); // Approved By User
                            MergeCells(ws, topRowStart, row - 1, 7); // Submitted RQM
                            MergeCells(ws, topRowStart, row - 1, 8); // Status
                            MergeCells(ws, topRowStart, row - 1, 9); // Remarks
                        }
                        no++;
                    }
                }
            }

            // Column widths
            ws.Column(1).Width = 5;   // No
            ws.Column(2).Width = 34;  // Nama Dokumen
            ws.Column(3).Width = 22;  // Asal Data
            ws.Column(4).Width = 13;  // Date Received
            ws.Column(5).Width = 15;  // Submitted To User
            ws.Column(6).Width = 15;  // Approved By User
            ws.Column(7).Width = 13;  // Submitted RQM
            ws.Column(8).Width = 26;  // Status
            ws.Column(9).Width = 22;  // Remarks
            ws.View.FreezePanes(2, 1);

            return package.GetAsByteArray();
        }

        private void MergeCells(ExcelWorksheet worksheet, int rowStart, int rowEnd, int col)
        {
            if (rowEnd > rowStart)
            {
                var mergeRange = worksheet.Cells[rowStart, col, rowEnd, col];
                mergeRange.Merge = true;
                mergeRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                mergeRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
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

    // Helper classes for Excel export grouping
    internal class KpiExportGroup
    {
        public string? Tag { get; set; }
        public List<Pm.Models.KpiDocument> Items { get; set; } = new();
    }

    internal class KpiExportTopGroup
    {
        public string GroupKey { get; set; } = "";
        public string? Tag { get; set; }
        public List<KpiExportSubGroup> SubGroups { get; set; } = new();
    }

    internal class KpiExportSubGroup
    {
        public string DataSource { get; set; } = "";
        public List<Pm.Models.KpiDocument> Items { get; set; } = new();
    }
}
