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
        // GET ALL (with duplicate detection)
        // ============================================
        public async Task<IEnumerable<RadioDto>> GetAllAsync(string? category = null, bool isScrap = false)
        {
            var query = _context.Radios.AsNoTracking().Where(r => r.IsScrap == isScrap);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(r => r.Category == category);
            }

            var radios = await query.OrderBy(r => r.Id).ToListAsync();

            // Find duplicate RadioIds AND Fleets across the current category
            var duplicateKeys = await _context.Radios
                .Where(r => r.Category == category && !r.IsScrap && !string.IsNullOrWhiteSpace(r.RadioId) && r.RadioId != "-")
                .GroupBy(r => new { r.RadioId, r.Fleet })
                .Where(g => g.Count() > 1)
                .Select(g => new { g.Key.RadioId, g.Key.Fleet })
                .ToListAsync();

            var duplicateSet = duplicateKeys.Select(k => $"{k.RadioId}_{k.Fleet ?? ""}").ToHashSet();

            return radios.Select(r => MapToDto(r,
                !string.IsNullOrWhiteSpace(r.RadioId) && r.RadioId != "-" && duplicateSet.Contains($"{r.RadioId}_{r.Fleet ?? ""}")
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
        public async Task<int> ImportInternalAsync(IFormFile file, int userId)
        {
            return await ImportRadioExcelAsync(file, "Internal", userId);
        }

        // ============================================
        // IMPORT: Radio Contractor
        // Kolom: NO, Nomor Aset, Nomor Unit, Serial Number, Type,
        //        TRUNGKING, KONV, Dept, Perusahaan, Channel, Tanggal,
        //        Fleet (multi sub-kolom), ID Radio, Scrap, Mark
        // ============================================
        public async Task<int> ImportContractorAsync(IFormFile file, int userId)
        {
            return await ImportRadioExcelAsync(file, "Contractor", userId);
        }

        /// <summary>
        /// Shared import logic for Internal and Contractor radios.
        /// Smart import: detects column positions dynamically from header row.
        /// </summary>
        private async Task<int> ImportRadioExcelAsync(IFormFile file, string category, int userId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var ws = package.Workbook.Worksheets[0];

            if (ws == null || ws.Dimension == null)
                throw new Exception("File Excel kosong atau tidak valid");

            int maxRow = ws.Dimension.End.Row;
            int maxCol = ws.Dimension.End.Column;

            // Smart detect header row (find row that contains "Nomor Aset" or "Serial Number")
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
                throw new Exception("Header row tidak ditemukan. Pastikan ada kolom 'Nomor Aset' atau 'Serial Number'.");

            // Build column map from header
            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var fleetColumns = new List<(int col, string fleetName)>();

            // Detect "Fleet" header row (might be one row above or the same row)
            int fleetHeaderRow = headerRow;
            for (int c = 1; c <= maxCol; c++)
            {
                var headerText = ws.Cells[headerRow, c].Text?.Trim();
                if (string.IsNullOrEmpty(headerText)) continue;

                var lower = headerText.ToLower();

                if (lower.Contains("nomor aset") || lower == "nomor aset") colMap["NomorAset"] = c;
                else if (lower.Contains("nomor unit") || lower == "nomor unit") colMap["NomorUnit"] = c;
                else if (lower.Contains("serial") || lower == "sn") colMap["SerialNumber"] = c;
                else if (lower == "type" || lower == "tipe") colMap["Type"] = c;
                else if (lower.Contains("trungking") || lower.Contains("trunking")) colMap["Trunking"] = c;
                else if (lower == "konv" || lower.Contains("conventional") || lower.Contains("konvensional")) colMap["Konv"] = c;
                else if (lower == "div" || lower == "divisi" || lower == "division") colMap["Division"] = c;
                else if (lower == "dept" || lower == "department") colMap["Department"] = c;
                else if (lower.Contains("perusahaan") || lower.Contains("company")) colMap["Company"] = c;
                else if (lower == "channel") colMap["Channel"] = c;
                else if (lower.Contains("tanggal") || lower == "date") colMap["Tanggal"] = c;
                else if (lower.Contains("id radio") || lower == "id radio") colMap["RadioId"] = c;
                else if (lower == "scrap" || lower == "skrap") colMap["Scrap"] = c;
                else if (lower == "mark" || lower == "keterangan") colMap["Mark"] = c;
                else if (lower != "no" && lower != "fleet" && lower != "contraktor")
                {
                    // Could be a fleet sub-column (e.g., "2001", "2351", etc.)
                    if (int.TryParse(headerText.Trim(), out _))
                    {
                        fleetColumns.Add((c, headerText.Trim()));
                    }
                }
            }

            // Also check one row BELOW for fleet sub-columns (some formats have "Fleet" header on row 1 and numbers on row 2)
            if (fleetColumns.Count == 0 && headerRow < maxRow)
            {
                for (int c = 1; c <= maxCol; c++)
                {
                    var text = ws.Cells[headerRow + 1, c].Text?.Trim();
                    if (!string.IsNullOrEmpty(text) && int.TryParse(text, out _))
                    {
                        fleetColumns.Add((c, text));
                    }
                }
            }

            // Determine data start row: if we found fleet on headerRow + 1, data starts at headerRow + 2
            int dataStartRow = headerRow + 1;
            if (fleetColumns.Count > 0)
            {
                // Check if the first fleet column name is actually on headerRow + 1
                var testCell = ws.Cells[headerRow + 1, fleetColumns.First().col].Text?.Trim();
                if (testCell == fleetColumns.First().fleetName)
                {
                    dataStartRow = headerRow + 2;
                }
            }

            _logger.LogInformation("📊 Import {Category}: Header row={Row}, Columns mapped={Count}, Fleet columns={FleetCount}",
                category, headerRow, colMap.Count, fleetColumns.Count);

            int imported = 0;
            for (int row = dataStartRow; row <= maxRow; row++)
            {
                // Skip empty rows
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

                // Parse tanggal
                var tanggalStr = GetCellValue(ws, row, colMap, "Tanggal");
                if (!string.IsNullOrEmpty(tanggalStr))
                {
                    if (DateTime.TryParse(tanggalStr, out var tanggal))
                        radio.Tanggal = tanggal;
                }

                // Parse fleet: find which fleet sub-column has a checkmark
                var fleetList = new List<string>();
                foreach (var (col, fleetName) in fleetColumns)
                {
                    var cellVal = ws.Cells[row, col].Text?.Trim().ToLower();
                    if (!string.IsNullOrEmpty(cellVal) && (cellVal == "✓" || cellVal == "√" || cellVal == "v" || cellVal == "1" || cellVal == "yes" || cellVal == "true" || cellVal == "y" || cellVal == "ü" || cellVal == "u" || cellVal == "\uf0fc" || cellVal == "p" || cellVal == "a" || cellVal == "ok" || cellVal.Contains("true")))
                    {
                        fleetList.Add(fleetName);
                    }
                }
                radio.Fleet = fleetList.Count > 0 ? string.Join(",", fleetList) : null;

                // Check scrap column
                var scrapVal = GetCellValue(ws, row, colMap, "Scrap");
                if (!string.IsNullOrEmpty(scrapVal))
                {
                    radio.IsScrap = scrapVal == "✓" || scrapVal == "√" || scrapVal.ToLower() == "yes" || scrapVal.ToLower() == "true" || scrapVal == "1";
                }

                _context.Radios.Add(radio);
                imported++;
            }

            await _context.SaveChangesAsync();

            try
            {
                await _activityLog.LogAsync("Radio", null, "Import", userId,
                    $"Import {category}: {imported} data radio berhasil diimport");
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "⚠️ ActivityLog failed for import");
            }

            _logger.LogInformation("✅ Import {Category} completed: {Count} records imported", category, imported);
            return imported;
        }

        // ============================================
        // IMPORT: Radio Unit (LV)
        // Kolom: No, Nomor LV, SN, LV Type, Div, Dept, Skrap, Mark
        // ============================================
        public async Task<int> ImportUnitAsync(IFormFile file, int userId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var ws = package.Workbook.Worksheets[0];

            if (ws == null || ws.Dimension == null)
                throw new Exception("File Excel kosong atau tidak valid");

            int maxRow = ws.Dimension.End.Row;
            int maxCol = ws.Dimension.End.Column;

            // Smart detect header
            int headerRow = 0;
            for (int r = 1; r <= Math.Min(5, maxRow); r++)
            {
                for (int c = 1; c <= maxCol; c++)
                {
                    var cellText = ws.Cells[r, c].Text?.Trim().ToLower();
                    if (cellText != null && (cellText.Contains("nomor lv") || cellText.Contains("lv type")))
                    {
                        headerRow = r;
                        break;
                    }
                }
                if (headerRow > 0) break;
            }

            if (headerRow == 0)
                throw new Exception("Header row tidak ditemukan. Pastikan ada kolom 'Nomor LV' atau 'LV Type'.");

            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= maxCol; c++)
            {
                var headerText = ws.Cells[headerRow, c].Text?.Trim();
                if (string.IsNullOrEmpty(headerText)) continue;

                var lower = headerText.ToLower();
                if (lower.Contains("nomor lv") || lower == "nomor lv") colMap["NomorLv"] = c;
                else if (lower == "sn" || lower.Contains("serial")) colMap["SerialNumber"] = c;
                else if (lower.Contains("lv type") || lower == "lv type") colMap["Type"] = c;
                else if (lower == "div" || lower.Contains("divis")) colMap["Division"] = c;
                else if (lower == "dept" || lower.Contains("depart")) colMap["Department"] = c;
                else if (lower.Contains("skrap") || lower.Contains("scrap")) colMap["Scrap"] = c;
                else if (lower == "mark" || lower == "keterangan") colMap["Mark"] = c;
            }

            _logger.LogInformation("📊 Import Unit: Header row={Row}, Columns mapped={Count}", headerRow, colMap.Count);

            int imported = 0;
            for (int row = headerRow + 1; row <= maxRow; row++)
            {
                var firstCell = ws.Cells[row, 1].Text?.Trim();
                if (string.IsNullOrEmpty(firstCell)) continue;

                var radio = new Models.Radio
                {
                    Category = "Unit",
                    NomorLv = GetCellValue(ws, row, colMap, "NomorLv"),
                    SerialNumber = GetCellValue(ws, row, colMap, "SerialNumber"),
                    Type = GetCellValue(ws, row, colMap, "Type"),
                    Division = GetCellValue(ws, row, colMap, "Division"),
                    Department = GetCellValue(ws, row, colMap, "Department"),
                    Mark = GetCellValue(ws, row, colMap, "Mark"),
                    IsScrap = false,
                    CreatedAt = DateTime.UtcNow
                };

                var scrapVal = GetCellValue(ws, row, colMap, "Scrap");
                if (!string.IsNullOrEmpty(scrapVal))
                {
                    radio.IsScrap = scrapVal == "✓" || scrapVal == "√" || scrapVal.ToLower() == "yes" || scrapVal == "1";
                }

                _context.Radios.Add(radio);
                imported++;
            }

            await _context.SaveChangesAsync();

            try
            {
                await _activityLog.LogAsync("Radio", null, "Import", userId,
                    $"Import Unit: {imported} data radio unit berhasil diimport");
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "⚠️ ActivityLog failed for unit import");
            }

            _logger.LogInformation("✅ Import Unit completed: {Count} records imported", imported);
            return imported;
        }

        // ============================================
        // IMPORT: Legacy Scrap
        // Kolom: No, TYPE Radio, Serial Number, Job Number, Tanggal Scrap, Remark, TRUNGKING, KONV
        // ============================================
        public async Task<int> ImportLegacyScrapAsync(IFormFile file, int userId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var ws = package.Workbook.Worksheets[0];

            if (ws == null || ws.Dimension == null)
                throw new Exception("File Excel kosong atau tidak valid");

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
                throw new Exception("Header row tidak ditemukan. Pastikan ada kolom 'Serial Number', 'Job Number', atau 'TYPE Radio'.");

            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= maxCol; c++)
            {
                var headerText = ws.Cells[headerRow, c].Text?.Trim();
                if (string.IsNullOrEmpty(headerText)) continue;

                var lower = headerText.ToLower().Trim();

                // Type Radio — handles "TYPE Radio", "Type", "Tipe"
                if (lower.Contains("type radio") || lower == "type" || lower == "tipe")
                    colMap["Type"] = c;
                // Serial Number
                else if (lower.Contains("serial"))
                    colMap["SerialNumber"] = c;
                // Job Number
                else if (lower.Contains("job number") || lower.Contains("job no"))
                    colMap["JobNumber"] = c;
                // Tanggal Scrap — handles "Tanggal Scrap", "Date Scrapped", "Tanggal"
                else if (lower.Contains("tanggal scrap") || lower.Contains("date scrap") || lower.Contains("tgl scrap"))
                    colMap["DateScrapped"] = c;
                // Remark — handles "Remark", "Remarks", "Keterangan"
                else if (lower.Contains("remark") || lower == "keterangan")
                    colMap["Remarks"] = c;
                // Trunking — handles "TRUNGKING", "TRUNKING", "Trunking"
                else if (lower.Contains("trungking") || lower.Contains("trunking"))
                    colMap["Trunking"] = c;
                // Konvensional — handles "KONV", "Konvensional", "Conventional"
                else if (lower == "konv" || lower.Contains("konvensional") || lower.Contains("conventional"))
                    colMap["Konv"] = c;
            }

            _logger.LogInformation("📊 Import Legacy Scrap: Header row={Row}, Columns mapped={Count}, Keys={Keys}",
                headerRow, colMap.Count, string.Join(", ", colMap.Keys));

            int imported = 0;
            for (int row = headerRow + 1; row <= maxRow; row++)
            {
                // Skip empty rows (check first few cells)
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

                // Parse date scrapped — handles multiple formats
                var dateStr = GetCellValue(ws, row, colMap, "DateScrapped");
                if (!string.IsNullOrEmpty(dateStr))
                {
                    // Try standard parse first
                    if (DateTime.TryParse(dateStr, out var dateScrapped))
                    {
                        radio.DateScrapped = dateScrapped;
                    }
                    else
                    {
                        // Try common formats: "2-Aug-06", "21-Sep-06", "4-Oct-06"
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
                imported++;
            }

            await _context.SaveChangesAsync();

            try
            {
                await _activityLog.LogAsync("Radio", null, "Import", userId,
                    $"Import Legacy Scrap: {imported} data radio scrap legacy berhasil diimport");
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "⚠️ ActivityLog failed for legacy scrap import");
            }

            _logger.LogInformation("✅ Import Legacy Scrap completed: {Count} records imported", imported);
            return imported;
        }
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var package = new ExcelPackage(stream);
            var ws = package.Workbook.Worksheets[0];

            if (ws == null || ws.Dimension == null)
                throw new Exception("File Excel kosong atau tidak valid");

            int maxRow = ws.Dimension.End.Row;
            int maxCol = ws.Dimension.End.Column;

            // Smart detect header
            int headerRow = 0;
            for (int r = 1; r <= Math.Min(5, maxRow); r++)
            {
                for (int c = 1; c <= maxCol; c++)
                {
                    var cellText = ws.Cells[r, c].Text?.Trim().ToLower();
                    if (cellText != null && (cellText.Contains("job number") || cellText.Contains("date scrapped")))
                    {
                        headerRow = r;
                        break;
                    }
                }
                if (headerRow > 0) break;
            }

            if (headerRow == 0)
                throw new Exception("Header row tidak ditemukan. Pastikan ada kolom 'Job Number' atau 'Date Scrapped'.");

            var colMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int c = 1; c <= maxCol; c++)
            {
                var headerText = ws.Cells[headerRow, c].Text?.Trim();
                if (string.IsNullOrEmpty(headerText)) continue;

                var lower = headerText.ToLower();
                if (lower == "type" || lower == "tipe") colMap["Type"] = c;
                else if (lower.Contains("serial")) colMap["SerialNumber"] = c;
                else if (lower.Contains("job number") || lower.Contains("job no")) colMap["JobNumber"] = c;
                else if (lower.Contains("date scrapped") || lower.Contains("date scrap")) colMap["DateScrapped"] = c;
                else if (lower.Contains("remark") || lower == "remarks") colMap["Remarks"] = c;
            }

            _logger.LogInformation("📊 Import Legacy Scrap: Header row={Row}, Columns mapped={Count}", headerRow, colMap.Count);

            int imported = 0;
            for (int row = headerRow + 1; row <= maxRow; row++)
            {
                var firstCell = ws.Cells[row, 1].Text?.Trim();
                if (string.IsNullOrEmpty(firstCell)) continue;

                var radio = new Models.Radio
                {
                    Category = "LegacyScrap",
                    Type = GetCellValue(ws, row, colMap, "Type"),
                    SerialNumber = GetCellValue(ws, row, colMap, "SerialNumber"),
                    ScrapJobNumber = GetCellValue(ws, row, colMap, "JobNumber"),
                    Remarks = GetCellValue(ws, row, colMap, "Remarks"),
                    IsScrap = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Parse date scrapped
                var dateStr = GetCellValue(ws, row, colMap, "DateScrapped");
                if (!string.IsNullOrEmpty(dateStr))
                {
                    if (DateTime.TryParse(dateStr, out var dateScrapped))
                        radio.DateScrapped = dateScrapped;
                }

                _context.Radios.Add(radio);
                imported++;
            }

            await _context.SaveChangesAsync();

            try
            {
                await _activityLog.LogAsync("Radio", null, "Import", userId,
                    $"Import Legacy Scrap: {imported} data radio scrap legacy berhasil diimport");
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "⚠️ ActivityLog failed for legacy scrap import");
            }

            _logger.LogInformation("✅ Import Legacy Scrap completed: {Count} records imported", imported);
            return imported;
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
