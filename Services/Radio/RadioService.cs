using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.Radio;
using Pm.Models;
using Pm.Services;

namespace Pm.Services.Radio
{
    public class RadioService : IRadioService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<RadioService> _logger;

        public RadioService(AppDbContext context, IActivityLogService activityLog, ILogger<RadioService> logger)
        {
            _context = context;
            _activityLog = activityLog;
            _logger = logger;
        }

        /// <summary>
        /// Resolves a userId to a display name: "FullName (Username)" or falls back to userId string.
        /// </summary>
        private async Task<string> GetUserDisplayNameAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new { u.FullName, u.Username })
                .FirstOrDefaultAsync();

            if (user == null) return userId.ToString();
            if (!string.IsNullOrWhiteSpace(user.FullName))
                return $"{user.FullName} ({user.Username})";
            return user.Username ?? userId.ToString();
        }

        // ============================================
        // HELPER: Map Entity to DTO
        // ============================================
        private static RadioDto MapToDto(Models.Radio r, bool isDuplicate = false)
        {
            return new RadioDto
            {
                Id = r.Id,
                Category = r.Category,
                SerialNumber = r.SerialNumber,
                Type = r.Type,
                Department = r.Department,
                Division = r.Division,
                Company = r.Company,
                Channel = r.Channel,
                Tanggal = r.Tanggal,
                NomorAset = r.NomorAset,
                NomorUnit = r.NomorUnit,
                NomorLv = r.NomorLv,
                IsTrunking = r.IsTrunking,
                IsConventional = r.IsConventional,
                Fleet = r.Fleet,
                RadioId = r.RadioId,
                IsScrap = r.IsScrap,
                ScrapJobNumber = r.ScrapJobNumber,
                DateScrapped = r.DateScrapped,
                Remarks = r.Remarks,
                Mark = r.Mark,
                IsDuplicateId = isDuplicate
            };
        }

        // ============================================
        // GET ALL (with duplicate detection) — PAGED
        // ============================================
        public async Task<PagedResultDto<RadioDto>> GetAllAsync(RadioQueryDto query)
        {
            var q = _context.Radios.AsNoTracking()
                .Where(r => r.IsScrap == query.IsScrap);

            if (!string.IsNullOrEmpty(query.Category))
                q = q.Where(r => r.Category == query.Category);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(r =>
                    (r.NomorAset != null && r.NomorAset.ToLower().Contains(s)) ||
                    (r.NomorUnit != null && r.NomorUnit.ToLower().Contains(s)) ||
                    (r.NomorLv != null && r.NomorLv.ToLower().Contains(s)) ||
                    (r.SerialNumber != null && r.SerialNumber.ToLower().Contains(s)) ||
                    (r.RadioId != null && r.RadioId.ToLower().Contains(s)) ||
                    (r.Division != null && r.Division.ToLower().Contains(s)) ||
                    (r.Type != null && r.Type.ToLower().Contains(s)) ||
                    (r.Fleet != null && r.Fleet.ToLower().Contains(s)) ||
                    (r.Company != null && r.Company.ToLower().Contains(s)) ||
                    (r.Department != null && r.Department.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(query.Division))
                q = q.Where(r => r.Division == query.Division);

            if (!string.IsNullOrWhiteSpace(query.Department))
                q = q.Where(r => r.Department == query.Department);

            if (!string.IsNullOrWhiteSpace(query.Type))
                q = q.Where(r => r.Type == query.Type);

            if (!string.IsNullOrWhiteSpace(query.Fleet))
                q = q.Where(r => r.Fleet != null && r.Fleet.Contains(query.Fleet));

            if (query.Jenis == "trunking")
                q = q.Where(r => r.IsTrunking);
            else if (query.Jenis == "konvensional")
                q = q.Where(r => r.IsConventional);

            if (query.IsNoGrafir == true)
                q = q.Where(r => string.IsNullOrWhiteSpace(r.NomorAset) ||
                                 r.NomorAset == "-" ||
                                 r.NomorAset.ToLower() == "no graf" ||
                                 r.NomorAset.ToLower() == "no grafir");

            if (query.DateFrom.HasValue)
                q = q.Where(r => r.DateScrapped >= query.DateFrom.Value);

            if (query.DateTo.HasValue)
            {
                var dateTo = query.DateTo.Value.Date.AddDays(1).AddTicks(-1);
                q = q.Where(r => r.DateScrapped <= dateTo);
            }

            // Hitung duplicate set
            var duplicateKeys = await _context.Radios
                .Where(r => r.Category == query.Category && !r.IsScrap &&
                            !string.IsNullOrWhiteSpace(r.RadioId) && r.RadioId != "-")
                .GroupBy(r => new { r.RadioId, r.Fleet })
                .Where(g => g.Count() > 1)
                .Select(g => new { g.Key.RadioId, g.Key.Fleet })
                .ToListAsync();

            var duplicateSet = duplicateKeys
                .Select(k => $"{k.RadioId}_{k.Fleet ?? ""}")
                .ToHashSet();

            int totalCount;
            List<Models.Radio> radios;

            if (query.IsDuplicate == true)
            {
                var allFiltered = await q.OrderBy(r => r.Id).ToListAsync();
                var dupFiltered = allFiltered.Where(r =>
                    !string.IsNullOrWhiteSpace(r.RadioId) && r.RadioId != "-" &&
                    duplicateSet.Contains($"{r.RadioId}_{r.Fleet ?? ""}")).ToList();

                totalCount = dupFiltered.Count;
                radios = dupFiltered
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();
            }
            else
            {
                totalCount = await q.CountAsync();
                radios = await q
                    .OrderBy(r => r.Id)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();
            }

            var items = radios.Select(r => MapToDto(r,
                !string.IsNullOrWhiteSpace(r.RadioId) && r.RadioId != "-" &&
                duplicateSet.Contains($"{r.RadioId}_{r.Fleet ?? ""}")
            )).ToList();

            return new PagedResultDto<RadioDto>(items, query, totalCount);
        }

        // ============================================
        // GET ALL UNPAGED — untuk keperluan internal (filter options, export)
        // ============================================
        public async Task<IEnumerable<RadioDto>> GetAllUnpagedAsync(string? category = null, bool isScrap = false)
        {
            var query = _context.Radios.AsNoTracking().Where(r => r.IsScrap == isScrap);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(r => r.Category == category);

            var radios = await query.OrderBy(r => r.Id).ToListAsync();

            var duplicateKeys = await _context.Radios
                .Where(r => r.Category == category && !r.IsScrap &&
                            !string.IsNullOrWhiteSpace(r.RadioId) && r.RadioId != "-")
                .GroupBy(r => new { r.RadioId, r.Fleet })
                .Where(g => g.Count() > 1)
                .Select(g => new { g.Key.RadioId, g.Key.Fleet })
                .ToListAsync();

            var duplicateSet = duplicateKeys.Select(k => $"{k.RadioId}_{k.Fleet ?? ""}").ToHashSet();

            return radios.Select(r => MapToDto(r,
                !string.IsNullOrWhiteSpace(r.RadioId) && r.RadioId != "-" &&
                duplicateSet.Contains($"{r.RadioId}_{r.Fleet ?? ""}")
            ));
        }

        // ============================================
        // GET BY ID
        // ============================================
        public async Task<RadioDto> GetByIdAsync(int id)
        {
            var r = await _context.Radios.FindAsync(id);
            if (r == null) throw new KeyNotFoundException("Radio not found");
            return MapToDto(r);
        }

        public async Task<IEnumerable<RadioHistoryDto>> GetHistoryAsync(int id)
        {
            var radio = await _context.Radios.FindAsync(id);
            if (radio == null)
                throw new KeyNotFoundException($"Radio with ID {id} not found");

            var histories = await _context.RadioHistories
                .Where(h => h.RadioId == id)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            return histories.Select(h => new RadioHistoryDto
            {
                Id = h.Id,
                RadioId = h.RadioId,
                Action = h.Action,
                Changes = h.Details,
                CreatedBy = h.CreatedBy ?? "System",
                CreatedAt = h.CreatedAt
            });
        }

        // ============================================
        // CREATE
        // ============================================
        public async Task<RadioDto> CreateAsync(CreateRadioDto dto, int userId)
        {
            var radio = new Models.Radio
            {
                Category = dto.Category,
                SerialNumber = dto.SerialNumber,
                Type = dto.Type,
                Department = dto.Department,
                Division = dto.Division,
                Company = dto.Company,
                Channel = dto.Channel,
                Tanggal = dto.Tanggal,
                NomorAset = dto.NomorAset,
                NomorUnit = dto.NomorUnit,
                NomorLv = dto.NomorLv,
                IsTrunking = dto.IsTrunking,
                IsConventional = dto.IsConventional,
                Fleet = dto.Fleet,
                RadioId = dto.RadioId,
                IsScrap = dto.IsScrap,
                ScrapJobNumber = dto.ScrapJobNumber,
                DateScrapped = dto.DateScrapped,
                Remarks = dto.Remarks,
                Mark = dto.Mark,
                CreatedAt = DateTime.UtcNow
            };

            _context.Radios.Add(radio);
            await _context.SaveChangesAsync();

            _context.RadioHistories.Add(new RadioHistory
            {
                RadioId = radio.Id,
                Action = "Created",
                CreatedBy = await GetUserDisplayNameAsync(userId),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio", radio.Id, "Create", userId, $"Radio {radio.NomorAset ?? radio.NomorLv ?? radio.SerialNumber} dibuat (Category: {radio.Category})");

            return await GetByIdAsync(radio.Id);
        }

        // ============================================
        // UPDATE
        // ============================================
        public async Task<RadioDto> UpdateAsync(int id, UpdateRadioDto dto, int userId)
        {
            var radio = await _context.Radios.FindAsync(id);
            if (radio == null) throw new KeyNotFoundException("Radio not found");

            // ── Build diff before applying changes ──────────────────────────
            var diffs = new List<string>();

            void Track(string label, string? oldVal, string? newVal)
            {
                var o = string.IsNullOrWhiteSpace(oldVal) ? "-" : oldVal.Trim();
                var n = string.IsNullOrWhiteSpace(newVal) ? "-" : newVal.Trim();
                if (!string.Equals(o, n, StringComparison.OrdinalIgnoreCase))
                    diffs.Add($"{label}: \"{o}\" → \"{n}\"");
            }

            void TrackBool(string label, bool oldVal, bool newVal)
            {
                if (oldVal != newVal)
                    diffs.Add($"{label}: {(oldVal ? "Ya" : "Tidak")} → {(newVal ? "Ya" : "Tidak")}");
            }

            void TrackDate(string label, DateTime? oldVal, DateTime? newVal)
            {
                var o = oldVal.HasValue ? oldVal.Value.ToString("dd/MM/yyyy") : "-";
                var n = newVal.HasValue ? newVal.Value.ToString("dd/MM/yyyy") : "-";
                if (o != n) diffs.Add($"{label}: \"{o}\" → \"{n}\"");
            }

            Track("Nomor Aset",   radio.NomorAset,    dto.NomorAset);
            Track("Nomor Unit",   radio.NomorUnit,    dto.NomorUnit);
            Track("Serial Number",radio.SerialNumber, dto.SerialNumber);
            Track("Type",         radio.Type,         dto.Type);
            Track("Divisi",       radio.Division,     dto.Division);
            Track("Dept",         radio.Department,   dto.Department);
            Track("Channel",      radio.Channel,      dto.Channel);
            Track("Fleet",        radio.Fleet,        dto.Fleet);
            Track("ID Radio",     radio.RadioId,      dto.RadioId);
            Track("Mark",         radio.Mark,         dto.Mark);
            TrackBool("Trunking",      radio.IsTrunking,     dto.IsTrunking);
            TrackBool("Konvensional",  radio.IsConventional, dto.IsConventional);
            TrackDate("Tanggal",       radio.Tanggal,        dto.Tanggal);

            // ── Apply changes ────────────────────────────────────────────────
            radio.Category       = dto.Category;
            radio.SerialNumber   = dto.SerialNumber;
            radio.Type           = dto.Type;
            radio.Department     = dto.Department;
            radio.Division       = dto.Division;
            radio.Company        = dto.Company;
            radio.Channel        = dto.Channel;
            radio.Tanggal        = dto.Tanggal;
            radio.NomorAset      = dto.NomorAset;
            radio.NomorUnit      = dto.NomorUnit;
            radio.NomorLv        = dto.NomorLv;
            radio.IsTrunking     = dto.IsTrunking;
            radio.IsConventional = dto.IsConventional;
            radio.Fleet          = dto.Fleet;
            radio.RadioId        = dto.RadioId;
            radio.IsScrap        = dto.IsScrap;
            radio.ScrapJobNumber = dto.ScrapJobNumber;
            radio.DateScrapped   = dto.DateScrapped;
            radio.Remarks        = dto.Remarks;
            radio.Mark           = dto.Mark;
            radio.UpdatedAt      = DateTime.UtcNow;

            var diffText = diffs.Count > 0
                ? string.Join("\n", diffs)
                : "Tidak ada perubahan terdeteksi";

            _context.RadioHistories.Add(new RadioHistory
            {
                RadioId   = radio.Id,
                Action    = "Updated",
                Details   = diffText,
                CreatedBy = await GetUserDisplayNameAsync(userId),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await _activityLog.LogAsync("Radio", radio.Id, "Update", userId,
                $"Radio {radio.NomorAset ?? radio.SerialNumber} diupdate ({diffs.Count} perubahan)");

            return await GetByIdAsync(radio.Id);
        }

        // ============================================
        // DELETE
        // ============================================
        public async Task DeleteAsync(int id, int userId)
        {
            var radio = await _context.Radios.FindAsync(id);
            if (radio == null) throw new KeyNotFoundException("Radio not found");

            var identifier = radio.NomorAset ?? radio.NomorLv ?? radio.SerialNumber ?? id.ToString();
            _context.Radios.Remove(radio);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio", id, "Delete", userId, $"Radio {identifier} dihapus");
        }

        public async Task DeleteAllAsync(int userId)
        {
            var allRadios = await _context.Radios.ToListAsync();
            _context.Radios.RemoveRange(allRadios);
            await _context.SaveChangesAsync();
            await _activityLog.LogAsync("Radio", 0, "DeleteAll", userId, $"Seluruh data radio dihapus");
        }

        public async Task<int> DeleteByCategoryAsync(string category, int userId)
        {
            var radios = await _context.Radios
                .Where(r => r.Category == category)
                .ToListAsync();
            
            int count = radios.Count;
            _context.Radios.RemoveRange(radios);
            await _context.SaveChangesAsync();
            await _activityLog.LogAsync("Radio", 0, "DeleteByCategory", userId,
                $"Seluruh data radio kategori '{category}' dihapus ({count} records)");
            return count;
        }

        // ============================================
        // SCRAP (mutasi radio aktif -> scrap)
        // ============================================
        public async Task<RadioDto> ScrapRadioAsync(int id, ScrapRadioDto dto, int userId)
        {
            var radio = await _context.Radios.FindAsync(id);
            if (radio == null) throw new KeyNotFoundException("Radio not found");

            radio.IsScrap = true;
            radio.ScrapJobNumber = dto.ScrapJobNumber;
            radio.DateScrapped = dto.DateScrapped;
            radio.Remarks = dto.Remarks;
            radio.UpdatedAt = DateTime.UtcNow;

            _context.RadioHistories.Add(new RadioHistory
            {
                RadioId = radio.Id,
                Action = "Scrapped",
                Details = $"Job Number: {dto.ScrapJobNumber}, Remarks: {dto.Remarks}",
                CreatedBy = await GetUserDisplayNameAsync(userId),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await _activityLog.LogAsync("Radio", radio.Id, "Scrap", userId, $"Radio {radio.NomorAset ?? radio.SerialNumber} di-scrap, Job: {dto.ScrapJobNumber}");

            return await GetByIdAsync(radio.Id);
        }

        // ============================================
        // IMPORT: Radio Internal
        // Kolom: NO, Nomor Aset, Nomor Unit, Serial Number, Type,
        //        TRUNGKING, KONV, DIV, Dept, Channel, Tanggal,
        //        Fleet (multi sub-kolom), ID Radio, Scrap, Mark
        // ============================================
        public async Task<ImportResultDto> ImportInternalAsync(IFormFile file, int userId)
        {
            return await ImportRadioExcelAsync(file, "Internal", userId);
        }

        // ============================================
        // IMPORT: Radio Contractor
        // Kolom: NO, Nomor Aset, Nomor Unit, Serial Number, Type,
        //        TRUNGKING, KONV, Dept, Perusahaan, Channel, Tanggal,
        //        Fleet (multi sub-kolom), ID Radio, Scrap, Mark
        // ============================================
        public async Task<ImportResultDto> ImportContractorAsync(IFormFile file, int userId)
        {
            return await ImportRadioExcelAsync(file, "Contractor", userId);
        }

        /// <summary>
        /// Shared import logic for Internal and Contractor radios.
        /// Smart import: detects column positions dynamically from header row.
        /// Supports multiple sheets — all sheets are processed.
        /// </summary>
        private async Task<ImportResultDto> ImportRadioExcelAsync(IFormFile file, string category, int userId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);

            if (package.Workbook.Worksheets.Count == 0)
                throw new Exception("File Excel tidak memiliki sheet.");

            int totalImported = 0;
            var sheetDetails = new List<SheetImportDetail>();

            // ── Loop semua sheet ──────────────────────────────────────────────
            foreach (var ws in package.Workbook.Worksheets)
            {
                if (ws.Dimension == null) continue; // skip sheet kosong

                int maxRow = ws.Dimension.End.Row;
                int maxCol = ws.Dimension.End.Column;

                // Smart detect header row
                int headerRow = 0;
                for (int r = 1; r <= Math.Min(5, maxRow); r++)
                {
                    for (int c = 1; c <= maxCol; c++)
                    {
                        var cellText = ws.Cells[r, c].Text?.Trim().ToLower();
                        if (cellText != null && (cellText.Contains("nomor aset") || cellText.Contains("serial")))
                        {
                            headerRow = r;
                            break;
                        }
                    }
                    if (headerRow > 0) break;
                }

                if (headerRow == 0)
                {
                    _logger.LogWarning("⚠️ Sheet '{Sheet}' dilewati: header row tidak ditemukan.", ws.Name);
                    continue;
                }

                var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var fleetColumns = new List<(int col, string fleetName)>();

                // ── Scan baris header utama (baris 1) ────────────────────────
                for (int c = 1; c <= maxCol; c++)
                {
                    var headerText = ws.Cells[headerRow, c].Text?.Trim();
                    if (string.IsNullOrEmpty(headerText)) continue;

                    var lower = headerText.ToLower().Trim().TrimEnd('.');

                    if (lower.Contains("nomor aset") || lower == "nomor aset") colMap["NomorAset"] = c;
                    else if (lower.Contains("nomor unit") || lower == "nomor unit") colMap["NomorUnit"] = c;
                    else if (lower.Contains("serial") || lower == "sn") colMap["SerialNumber"] = c;
                    else if (lower.Contains("type radio") || lower == "type" || lower == "tipe") colMap["Type"] = c;
                    else if (lower.Contains("trungking") || lower.Contains("trunking")) colMap["Trunking"] = c;
                    else if (lower == "konv" || lower.Contains("conventional") || lower.Contains("konvensional")) colMap["Konv"] = c;
                    else if (lower == "div" || lower.Contains("divis")) colMap["Division"] = c;
                    else if (lower == "dept" || lower.Contains("depart")) colMap["Department"] = c;
                    else if (lower.Contains("perusahaan") || lower.Contains("company")) colMap["Company"] = c;
                    else if (lower == "channel") colMap["Channel"] = c;
                    else if (lower.Contains("tanggal") || lower == "date") colMap["Tanggal"] = c;
                    else if (lower.Contains("id radio") || lower == "id radio") colMap["RadioId"] = c;
                    else if (lower == "scrap" || lower == "skrap") colMap["Scrap"] = c;
                    else if (lower == "mark" || lower == "keterangan") colMap["Mark"] = c;
                    else if (lower != "no" && lower != "fleet" && lower != "contraktor" && lower != "no.")
                    {
                        // Angka di baris 1 langsung = fleet sub-column
                        if (int.TryParse(headerText.Trim(), out _))
                            fleetColumns.Add((c, headerText.Trim()));
                    }
                }

                // ── Scan baris 2 (headerRow+1) untuk fleet angka ─────────────
                // Format: baris 1 ada "Fleet" sebagai label, baris 2 ada angka 2001, 2351, dst.
                // Juga scan baris 2 untuk kolom ID Radio, Scrap, Mark yang mungkin ada di sana
                if (headerRow < maxRow)
                {
                    for (int c = 1; c <= maxCol; c++)
                    {
                        var row2Text = ws.Cells[headerRow + 1, c].Text?.Trim();
                        if (string.IsNullOrEmpty(row2Text)) continue;

                        // Jika angka → fleet sub-column (hanya jika belum ada di fleetColumns)
                        if (int.TryParse(row2Text, out _))
                        {
                            if (!fleetColumns.Any(f => f.col == c))
                                fleetColumns.Add((c, row2Text));
                        }
                        else
                        {
                            // Kolom teks di baris 2 yang belum ter-map di baris 1
                            var lower2 = row2Text.ToLower().Trim().TrimEnd('.');
                            if (!colMap.ContainsKey("RadioId") && (lower2.Contains("id radio") || lower2 == "id radio")) colMap["RadioId"] = c;
                            else if (!colMap.ContainsKey("Scrap") && (lower2 == "scrap" || lower2 == "skrap")) colMap["Scrap"] = c;
                            else if (!colMap.ContainsKey("Mark") && (lower2 == "mark" || lower2 == "keterangan")) colMap["Mark"] = c;
                        }
                    }
                }

                int dataStartRow = headerRow + 1;
                if (fleetColumns.Count > 0)
                {
                    var testCell = ws.Cells[headerRow + 1, fleetColumns.First().col].Text?.Trim();
                    if (testCell == fleetColumns.First().fleetName)
                        dataStartRow = headerRow + 2;
                }

                _logger.LogInformation("📊 Import {Category} sheet '{Sheet}': Header row={Row}, Columns={Count}, Fleet cols={FleetCount}",
                    category, ws.Name, headerRow, colMap.Count, fleetColumns.Count);

                int sheetImported = 0;
                for (int row = dataStartRow; row <= maxRow; row++)
                {
                    var firstCell = ws.Cells[row, 1].Text?.Trim();
                    if (string.IsNullOrEmpty(firstCell)) continue;

                    var radio = new Models.Radio
                    {
                        Category = category,
                        NomorAset = GetCellValue(ws, row, colMap, "NomorAset"),
                        NomorUnit = GetCellValue(ws, row, colMap, "NomorUnit"),
                        SerialNumber = GetCellValue(ws, row, colMap, "SerialNumber"),
                        Type = GetCellValue(ws, row, colMap, "Type"),
                        IsTrunking = IsChecked(ws, row, colMap, "Trunking"),
                        IsConventional = IsChecked(ws, row, colMap, "Konv"),
                        Division = category == "Internal" ? GetCellValue(ws, row, colMap, "Division") : null,
                        Company = category == "Contractor" ? GetCellValue(ws, row, colMap, "Company") : null,
                        Department = GetCellValue(ws, row, colMap, "Department"),
                        Channel = GetCellValue(ws, row, colMap, "Channel"),
                        RadioId = GetCellValue(ws, row, colMap, "RadioId"),
                        Mark = GetCellValue(ws, row, colMap, "Mark"),
                        IsScrap = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    var tanggalStr = GetCellValue(ws, row, colMap, "Tanggal");
                    if (!string.IsNullOrEmpty(tanggalStr))
                    {
                        if (DateTime.TryParse(tanggalStr, out var tanggal))
                            radio.Tanggal = tanggal;
                    }

                    var fleetList = new List<string>();
                    foreach (var (col, fleetName) in fleetColumns)
                    {
                        var cellVal = ws.Cells[row, col].Text?.Trim().ToLower();
                        if (!string.IsNullOrEmpty(cellVal) && (cellVal == "✓" || cellVal == "√" || cellVal == "v" || cellVal == "1" || cellVal == "yes" || cellVal == "true" || cellVal == "y" || cellVal == "ü" || cellVal == "u" || cellVal == "\uf0fc" || cellVal == "p" || cellVal == "a" || cellVal == "ok" || cellVal.Contains("true")))
                            fleetList.Add(fleetName);
                    }
                    radio.Fleet = fleetList.Count > 0 ? string.Join(",", fleetList) : null;

                    var scrapVal = GetCellValue(ws, row, colMap, "Scrap");
                    if (!string.IsNullOrEmpty(scrapVal))
                        radio.IsScrap = scrapVal == "✓" || scrapVal == "√" || scrapVal.ToLower() == "yes" || scrapVal.ToLower() == "true" || scrapVal == "1";

                    _context.Radios.Add(radio);
                    sheetImported++;
                }

                totalImported += sheetImported;
                sheetDetails.Add(new SheetImportDetail { SheetName = ws.Name, RecordCount = sheetImported });
                _logger.LogInformation("✅ Sheet '{Sheet}': {Count} records", ws.Name, sheetImported);
            }

            await _context.SaveChangesAsync();

            try
            {
                await _activityLog.LogAsync("Radio", null, "Import", userId,
                    $"Import {category}: {totalImported} data dari {sheetDetails.Count} sheet");
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "⚠️ ActivityLog failed for import");
            }

            _logger.LogInformation("✅ Import {Category} completed: {Count} total records from {Sheets} sheets",
                category, totalImported, sheetDetails.Count);

            return new ImportResultDto
            {
                TotalImported = totalImported,
                SheetCount = sheetDetails.Count,
                SheetDetails = sheetDetails
            };
        }

        // ============================================
        // IMPORT: Radio Unit (LV)
        // Kolom: No, Nomor Aset, Nomor LV, Serial Number, LV Type, DIV., Dept., TRUNGKING, KONV, Scrap, Mark
        // ============================================
        public async Task<ImportResultDto> ImportUnitAsync(IFormFile file, int userId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);

            if (package.Workbook.Worksheets.Count == 0)
                throw new Exception("File Excel tidak memiliki sheet.");

            int totalImported = 0;
            var sheetDetails = new List<SheetImportDetail>();

            // ── Loop semua sheet ──────────────────────────────────────────────
            foreach (var ws in package.Workbook.Worksheets)
            {
                if (ws.Dimension == null) continue; // skip sheet kosong

                int maxRow = ws.Dimension.End.Row;
                int maxCol = ws.Dimension.End.Column;

                // Smart detect header row
                int headerRow = 0;
                for (int r = 1; r <= Math.Min(5, maxRow); r++)
                {
                    for (int c = 1; c <= maxCol; c++)
                    {
                        var cellText = ws.Cells[r, c].Text?.Trim().ToLower();
                        if (cellText != null && (
                            cellText.Contains("nomor lv") ||
                            cellText.Contains("lv type") ||
                            cellText.Contains("serial") ||
                            cellText.Contains("nomor aset")))
                        {
                            headerRow = r;
                            break;
                        }
                    }
                    if (headerRow > 0) break;
                }

                if (headerRow == 0)
                {
                    _logger.LogWarning("⚠️ Sheet '{Sheet}' dilewati: header row tidak ditemukan.", ws.Name);
                    continue; // skip sheet tanpa header yang dikenali
                }

                var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var fleetColumns = new List<(int col, string fleetName)>();

                for (int c = 1; c <= maxCol; c++)
                {
                    var headerText = ws.Cells[headerRow, c].Text?.Trim();
                    if (string.IsNullOrEmpty(headerText)) continue;

                    var lower = headerText.ToLower().Trim().TrimEnd('.');

                    if (lower.Contains("nomor aset") || lower == "nomor aset") colMap["NomorAset"] = c;
                    else if (lower.Contains("nomor lv") || lower == "nomor lv") colMap["NomorLv"] = c;
                    else if (lower == "sn" || lower.Contains("serial")) colMap["SerialNumber"] = c;
                    else if (lower.Contains("lv type") || lower.Contains("type radio") || lower == "type" || lower == "tipe") colMap["Type"] = c;
                    else if (lower == "div" || lower.Contains("divis")) colMap["Division"] = c;
                    else if (lower == "dept" || lower.Contains("depart")) colMap["Department"] = c;
                    else if (lower == "channel") colMap["Channel"] = c;
                    else if (lower.Contains("trungking") || lower.Contains("trunking")) colMap["Trunking"] = c;
                    else if (lower == "konv" || lower.Contains("konvensional") || lower.Contains("conventional")) colMap["Konv"] = c;
                    else if (lower.Contains("skrap") || lower.Contains("scrap")) colMap["Scrap"] = c;
                    else if (lower == "mark" || lower == "keterangan") colMap["Mark"] = c;
                    else if (lower.Contains("tanggal") || lower == "date") colMap["Tanggal"] = c;
                    else if (lower.Contains("id radio") || lower == "id radio") colMap["RadioId"] = c;
                    else if (lower != "no" && lower != "fleet" && lower != "no.")
                    {
                        if (int.TryParse(headerText.Trim(), out _))
                            fleetColumns.Add((c, headerText.Trim()));
                    }
                }

                // Also scan the row ABOVE headerRow for any missed columns (merged header scenario)
                if (headerRow > 1)
                {
                    for (int c = 1; c <= maxCol; c++)
                    {
                        var aboveText = ws.Cells[headerRow - 1, c].Text?.Trim().ToLower().TrimEnd('.');
                        if (string.IsNullOrEmpty(aboveText)) continue;
                        if (aboveText == "div" || aboveText.Contains("divis")) { if (!colMap.ContainsKey("Division")) colMap["Division"] = c; }
                        else if (aboveText == "dept" || aboveText.Contains("depart")) { if (!colMap.ContainsKey("Department")) colMap["Department"] = c; }
                        else if (aboveText == "channel") { if (!colMap.ContainsKey("Channel")) colMap["Channel"] = c; }
                        else if (aboveText.Contains("tanggal") || aboveText == "date") { if (!colMap.ContainsKey("Tanggal")) colMap["Tanggal"] = c; }
                        else if (aboveText.Contains("id radio")) { if (!colMap.ContainsKey("RadioId")) colMap["RadioId"] = c; }
                        else if (aboveText == "mark") { if (!colMap.ContainsKey("Mark")) colMap["Mark"] = c; }
                        else if (aboveText.Contains("trungking") || aboveText.Contains("trunking")) { if (!colMap.ContainsKey("Trunking")) colMap["Trunking"] = c; }
                        else if (aboveText == "konv" || aboveText.Contains("konvensional")) { if (!colMap.ContainsKey("Konv")) colMap["Konv"] = c; }
                    }
                }

                // Check row below for fleet sub-columns
                if (fleetColumns.Count == 0 && headerRow < maxRow)
                {
                    for (int c = 1; c <= maxCol; c++)
                    {
                        var text = ws.Cells[headerRow + 1, c].Text?.Trim();
                        if (!string.IsNullOrEmpty(text) && int.TryParse(text, out _))
                            fleetColumns.Add((c, text));
                    }
                }

                int dataStartRow = headerRow + 1;
                if (fleetColumns.Count > 0)
                {
                    var testCell = ws.Cells[headerRow + 1, fleetColumns.First().col].Text?.Trim();
                    if (testCell == fleetColumns.First().fleetName)
                        dataStartRow = headerRow + 2;
                }

                _logger.LogInformation("📊 Import Unit sheet '{Sheet}': Header row={Row}, Columns={Count}, Fleet cols={FleetCount}",
                    ws.Name, headerRow, colMap.Count, fleetColumns.Count);

                int sheetImported = 0;
                for (int row = dataStartRow; row <= maxRow; row++)
                {
                    var hasData = false;
                    for (int c = 1; c <= Math.Min(5, maxCol); c++)
                    {
                        if (!string.IsNullOrWhiteSpace(ws.Cells[row, c].Text)) { hasData = true; break; }
                    }
                    if (!hasData) continue;

                    var radio = new Models.Radio
                    {
                        Category = "Unit",
                        NomorAset = GetCellValue(ws, row, colMap, "NomorAset"),
                        NomorLv = GetCellValue(ws, row, colMap, "NomorLv"),
                        SerialNumber = GetCellValue(ws, row, colMap, "SerialNumber"),
                        Type = GetCellValue(ws, row, colMap, "Type"),
                        Division = GetCellValue(ws, row, colMap, "Division"),
                        Department = GetCellValue(ws, row, colMap, "Department"),
                        Channel = GetCellValue(ws, row, colMap, "Channel"),
                        IsTrunking = IsChecked(ws, row, colMap, "Trunking"),
                        IsConventional = IsChecked(ws, row, colMap, "Konv"),
                        RadioId = GetCellValue(ws, row, colMap, "RadioId"),
                        Mark = GetCellValue(ws, row, colMap, "Mark"),
                        IsScrap = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    var tanggalStr = GetCellValue(ws, row, colMap, "Tanggal");
                    if (!string.IsNullOrEmpty(tanggalStr))
                    {
                        if (DateTime.TryParse(tanggalStr, System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out var tanggal))
                        {
                            radio.Tanggal = tanggal;
                        }
                        else
                        {
                            var formats = new[] {
                                "d-MMM-yy", "d-MMM-yyyy", "dd-MMM-yy", "dd-MMM-yyyy",
                                "d/MM/yyyy", "dd/MM/yyyy", "M/d/yyyy", "MM/dd/yyyy",
                                "d-MM-yyyy", "dd-MM-yyyy", "yyyy-MM-dd"
                            };
                            if (DateTime.TryParseExact(tanggalStr, formats,
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out var parsed))
                                radio.Tanggal = parsed;
                        }
                    }

                    var fleetList = new List<string>();
                    foreach (var (col, fleetName) in fleetColumns)
                    {
                        var cellVal = ws.Cells[row, col].Text?.Trim().ToLower();
                        if (!string.IsNullOrEmpty(cellVal) && (cellVal == "✓" || cellVal == "√" || cellVal == "v" || cellVal == "1" || cellVal == "yes" || cellVal == "true" || cellVal == "y" || cellVal == "ü" || cellVal == "u" || cellVal == "\uf0fc" || cellVal == "p" || cellVal == "a" || cellVal == "ok" || cellVal.Contains("true")))
                            fleetList.Add(fleetName);
                    }
                    radio.Fleet = fleetList.Count > 0 ? string.Join(",", fleetList) : null;

                    var scrapVal = GetCellValue(ws, row, colMap, "Scrap");
                    if (!string.IsNullOrEmpty(scrapVal))
                        radio.IsScrap = scrapVal == "✓" || scrapVal == "√" || scrapVal.ToLower() == "yes" || scrapVal.ToLower() == "true" || scrapVal == "1";

                    _context.Radios.Add(radio);
                    sheetImported++;
                }

                totalImported += sheetImported;
                sheetDetails.Add(new SheetImportDetail
                {
                    SheetName = ws.Name,
                    RecordCount = sheetImported
                });
                _logger.LogInformation("✅ Sheet '{Sheet}': {Count} records", ws.Name, sheetImported);
            }

            await _context.SaveChangesAsync();

            try
            {
                await _activityLog.LogAsync("Radio", null, "Import", userId,
                    $"Import Unit: {totalImported} data radio unit berhasil diimport dari {package.Workbook.Worksheets.Count} sheet");
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "⚠️ ActivityLog failed for unit import");
            }

            _logger.LogInformation("✅ Import Unit completed: {Count} total records from {Sheets} sheets",
                totalImported, package.Workbook.Worksheets.Count);
            
            return new ImportResultDto
            {
                TotalImported = totalImported,
                SheetCount = sheetDetails.Count,
                SheetDetails = sheetDetails
            };
        }

        // ============================================
        // IMPORT: Legacy Scrap
        // Kolom: No, TYPE Radio, Serial Number, Job Number, Tanggal Scrap, Remark, TRUNGKING, KONV
        // ============================================
        public async Task<ImportResultDto> ImportLegacyScrapAsync(IFormFile file, int userId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);

            if (package.Workbook.Worksheets.Count == 0)
                throw new Exception("File Excel tidak memiliki sheet.");

            int totalImported = 0;
            var sheetDetails = new List<SheetImportDetail>();

            // ── Loop semua sheet ──────────────────────────────────────────────
            foreach (var ws in package.Workbook.Worksheets)
            {
                if (ws.Dimension == null) continue; // skip sheet kosong

                int maxRow = ws.Dimension.End.Row;
                int maxCol = ws.Dimension.End.Column;

                // Smart detect header row — look for "Serial Number", "Job Number", or "TYPE Radio"
                int headerRow = 0;
                for (int r = 1; r <= Math.Min(5, maxRow); r++)
                {
                    for (int c = 1; c <= maxCol; c++)
                    {
                        var cellText = ws.Cells[r, c].Text?.Trim().ToLower();
                        if (cellText != null && (
                            cellText.Contains("serial") ||
                            cellText.Contains("job number") ||
                            cellText.Contains("type radio") ||
                            cellText.Contains("date scrapped") ||
                            cellText.Contains("tanggal scrap")))
                        {
                            headerRow = r;
                            break;
                        }
                    }
                    if (headerRow > 0) break;
                }

                if (headerRow == 0)
                {
                    _logger.LogWarning("⚠️ Sheet '{Sheet}' dilewati: header row tidak ditemukan.", ws.Name);
                    continue; // skip sheet tanpa header yang dikenali
                }

                var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int c = 1; c <= maxCol; c++)
                {
                    var headerText = ws.Cells[headerRow, c].Text?.Trim();
                    if (string.IsNullOrEmpty(headerText)) continue;

                    var lower = headerText.ToLower().Trim().TrimEnd('.');

                    if (lower.Contains("type radio") || lower == "type" || lower == "tipe")
                        colMap["Type"] = c;
                    else if (lower.Contains("serial"))
                        colMap["SerialNumber"] = c;
                    else if (lower.Contains("job number") || lower.Contains("job no"))
                        colMap["JobNumber"] = c;
                    else if (lower.Contains("tanggal scrap") || lower.Contains("date scrap") || lower.Contains("tgl scrap"))
                        colMap["DateScrapped"] = c;
                    else if (lower.Contains("remark") || lower == "keterangan")
                        colMap["Remarks"] = c;
                    else if (lower.Contains("trungking") || lower.Contains("trunking"))
                        colMap["Trunking"] = c;
                    else if (lower == "konv" || lower.Contains("konvensional") || lower.Contains("conventional"))
                        colMap["Konv"] = c;
                }

                _logger.LogInformation("📊 Import Legacy Scrap sheet '{Sheet}': Header row={Row}, Columns={Count}",
                    ws.Name, headerRow, colMap.Count);

                int sheetImported = 0;
                for (int row = headerRow + 1; row <= maxRow; row++)
                {
                    var hasData = false;
                    for (int c = 1; c <= Math.Min(5, maxCol); c++)
                    {
                        if (!string.IsNullOrWhiteSpace(ws.Cells[row, c].Text)) { hasData = true; break; }
                    }
                    if (!hasData) continue;

                    var radio = new Models.Radio
                    {
                        Category = "LegacyScrap",
                        Type = GetCellValue(ws, row, colMap, "Type"),
                        SerialNumber = GetCellValue(ws, row, colMap, "SerialNumber"),
                        ScrapJobNumber = GetCellValue(ws, row, colMap, "JobNumber"),
                        Remarks = GetCellValue(ws, row, colMap, "Remarks"),
                        IsTrunking = IsChecked(ws, row, colMap, "Trunking"),
                        IsConventional = IsChecked(ws, row, colMap, "Konv"),
                        IsScrap = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var dateStr = GetCellValue(ws, row, colMap, "DateScrapped");
                    if (!string.IsNullOrEmpty(dateStr))
                    {
                        if (DateTime.TryParse(dateStr, out var dateScrapped))
                        {
                            radio.DateScrapped = dateScrapped;
                        }
                        else
                        {
                            var formats = new[] {
                                "d-MMM-yy", "d-MMM-yyyy", "dd-MMM-yy", "dd-MMM-yyyy",
                                "d/MM/yyyy", "dd/MM/yyyy", "M/d/yyyy", "MM/dd/yyyy",
                                "d-MM-yyyy", "dd-MM-yyyy"
                            };
                            if (DateTime.TryParseExact(dateStr, formats,
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out var parsed))
                            {
                                radio.DateScrapped = parsed;
                            }
                        }
                    }

                    _context.Radios.Add(radio);
                    sheetImported++;
                }

                totalImported += sheetImported;
                sheetDetails.Add(new SheetImportDetail
                {
                    SheetName = ws.Name,
                    RecordCount = sheetImported
                });
                _logger.LogInformation("✅ Sheet '{Sheet}': {Count} records", ws.Name, sheetImported);
            }

            await _context.SaveChangesAsync();

            try
            {
                await _activityLog.LogAsync("Radio", null, "Import", userId,
                    $"Import Legacy Scrap: {totalImported} data radio scrap legacy berhasil diimport dari {package.Workbook.Worksheets.Count} sheet");
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "⚠️ ActivityLog failed for legacy scrap import");
            }

            _logger.LogInformation("✅ Import Legacy Scrap completed: {Count} total records from {Sheets} sheets",
                totalImported, package.Workbook.Worksheets.Count);
            
            return new ImportResultDto
            {
                TotalImported = totalImported,
                SheetCount = sheetDetails.Count,
                SheetDetails = sheetDetails
            };
        }

        // ============================================
        // HELPER: Read cell value by column name
        // ============================================
        private static string? GetCellValue(ExcelWorksheet ws, int row, Dictionary<string, int> colMap, string key)
        {
            if (!colMap.TryGetValue(key, out int col)) return null;
            var value = ws.Cells[row, col].Text?.Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static bool IsChecked(ExcelWorksheet ws, int row, Dictionary<string, int> colMap, string key)
        {
            var val = GetCellValue(ws, row, colMap, key)?.ToLower();
            if (string.IsNullOrEmpty(val)) return false;
            return val == "✓" || val == "√" || val == "v" || val == "1" || val == "yes" || val == "true" || val == "y" || val == "ü" || val == "u" || val == "\uf0fc" || val == "p" || val == "a" || val == "ok" || val.Contains("true");
        }
    }
}
