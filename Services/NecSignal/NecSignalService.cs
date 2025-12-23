using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting;
using OfficeOpenXml.Style;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;
using Pm.Models.NEC;
using System.Drawing;
using System.Globalization;
using Pm.Helper;
using Microsoft.Extensions.Logging;
using Pm.Enums;

namespace Pm.Services
{
    public class NecSignalService : INecSignalService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<NecSignalService> _logger;
        
        private const decimal TOO_STRONG_MAX = -30m;    // -30 sampai -45
        private const decimal TOO_STRONG_MIN = -45m;
        private const decimal OPTIMAL_MAX = -45m;       // -45 sampai -55
        private const decimal OPTIMAL_MIN = -55m;
        private const decimal WARNING_MAX = -55m;       // -55 sampai -60
        private const decimal WARNING_MIN = -60m;
        private const decimal SUB_OPTIMAL_MAX = -60m;   // -60 sampai -65
        private const decimal SUB_OPTIMAL_MIN = -65m;
        private const decimal CRITICAL = -65m;          // < -65 (lebih negatif dari -65)

        // ✅ PERBAIKAN: Logika comparison untuk RSL
        private string GetRslStatus(decimal rsl)
        {
            // Too Strong: -30 sampai -44.9 (nilai lebih besar/kurang negatif dari -45)
            if (rsl > TOO_STRONG_MIN && rsl <= TOO_STRONG_MAX) 
                return "too_strong";
            
            // Optimal: -45 sampai -54.9 (range optimal)
            if (rsl > OPTIMAL_MIN && rsl <= OPTIMAL_MAX) 
                return "optimal";
            
            // Warning: -55 sampai -59.9 (mulai perlu perhatian)
            if (rsl > WARNING_MIN && rsl <= WARNING_MAX) 
                return "warning";
            
            // Sub-optimal: -60 sampai -64.9 (di bawah optimal tapi belum critical)
            if (rsl > SUB_OPTIMAL_MIN && rsl <= SUB_OPTIMAL_MAX) 
                return "sub_optimal";
            
            // Critical: <= -65 (nilai -65, -66, -67, dst = semakin lemah = CRITICAL!)
            return "critical";
        }

        private string GetStatusMessage(decimal rsl, string status)
        {
            return status switch
            {
                "too_strong" => $"Terlalu kuat ({rsl:F1} dBm)",
                "optimal" => $"Optimal ({rsl:F1} dBm)",
                "warning" => $"Warning ({rsl:F1} dBm)",
                "sub_optimal" => $"Sub-optimal ({rsl:F1} dBm)",
                "critical" => $"Critical ({rsl:F1} dBm)",
                _ => null
            };
        }

        private NecOperationalStatus ParseStatus(string? statusString)
        {
            if (string.IsNullOrWhiteSpace(statusString))
                return NecOperationalStatus.Active;

            return statusString.ToLower().Trim() switch
            {
                "active" => NecOperationalStatus.Active,
                "dismantled" => NecOperationalStatus.Dismantled,
                "removed" => NecOperationalStatus.Removed,
                "obstacle" => NecOperationalStatus.Obstacle,
                _ => NecOperationalStatus.Active
            };
        }

        private string GetWarningMessage(decimal avgRsl)
        {
            var status = GetRslStatus(avgRsl);
            return status != "optimal" ? GetStatusMessage(avgRsl, status) : null;
        }

        public NecSignalService(
            AppDbContext context, 
            IActivityLogService activityLog,
            ILogger<NecSignalService> logger)
        {
            _context = context;
            _activityLog = activityLog;
            _logger = logger;
        }

        // ============================================
        // MONTHLY & YEARLY SUMMARY (TIDAK ADA PERUBAHAN)
        // ============================================
        
        public async Task<NecMonthlyHistoryResponseDto> GetMonthlyAsync(int year, int month)
        {
            try
            {
                _logger.LogInformation("📊 GetMonthlyAsync - Year: {Year}, Month: {Month}", year, month);
                if (month < 1 || month > 12) throw new ArgumentException("Bulan tidak valid.");

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1);

                // Ambil data + ambil status operasional terbaru per link
                var rawData = await _context.NecRslHistories
                    .AsNoTracking()
                    .Where(h => h.Date >= startDate && h.Date < endDate)
                    .GroupBy(h => new { 
                        TowerName = h.NecLink.NearEndTower.Name, 
                        LinkName = h.NecLink.LinkName 
                    })
                    .Select(g => new
                    {
                        TowerName = g.Key.TowerName,
                        LinkName = g.Key.LinkName,
                        AvgRsl = Math.Round(g.Average(h => h.RslNearEnd), 1),
                        // Ambil status operasional dari entri terbaru di bulan ini
                        OperationalStatus = g.OrderByDescending(h => h.Date)
                            .Select(h => h.Status)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var result = rawData
                    .GroupBy(x => x.TowerName)
                    .Select(tg => new NecTowerMonthlyDto
                    {
                        TowerName = tg.Key,
                        Links = tg.Select(x => new NecLinkMonthlyDto
                        {
                            LinkName = x.LinkName,
                            AvgRsl = x.AvgRsl,
                            Status = x.OperationalStatus.ToString(), // ✅ Kirim sebagai string ke frontend
                            WarningMessage = GetWarningMessage(x.AvgRsl) // Tetap berdasarkan RSL
                        }).OrderBy(l => l.LinkName).ToList()
                    })
                    .OrderBy(t => t.TowerName)
                    .ToList();

                _logger.LogInformation("✅ GetMonthlyAsync completed - {Count} towers found", result.Count);
                return new NecMonthlyHistoryResponseDto
                {
                    Period = startDate.ToString("MMMM yyyy", new CultureInfo("id-ID")),
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetMonthlyAsync");
                throw;
            }
        }

        public async Task<NecYearlySummaryDto> GetYearlyAsync(int year)
        {
            try
            {
                _logger.LogInformation("📊 GetYearlyAsync - Year: {Year}", year);
                
                var start = new DateTime(year, 1, 1);
                var end = start.AddYears(1);

                var rawData = await _context.NecRslHistories
                    .AsNoTracking()
                    .Where(h => h.Date >= start && h.Date < end)
                    .Select(h => new
                    {
                        Tower = h.NecLink.NearEndTower.Name,
                        Link = h.NecLink.LinkName,
                        Month = h.Date.Month,
                        Rsl = h.RslNearEnd
                    })
                    .ToListAsync();

                var result = rawData
                    .GroupBy(x => x.Tower)
                    .Select(tg => new NecTowerYearlyDto
                    {
                        TowerName = tg.Key,
                        Links = tg.GroupBy(l => l.Link)
                            .ToDictionary(
                                lg => lg.Key,
                                lg => new NecLinkYearlyDto
                                {
                                    MonthlyAvg = lg.GroupBy(m => m.Month)
                                        .ToDictionary(
                                            m => new DateTime(year, m.Key, 1).ToString("MMM", new CultureInfo("en-US")),
                                            m => Math.Round(m.Average(x => x.Rsl), 1)
                                        ),
                                    YearlyAvg = Math.Round(lg.Average(x => x.Rsl), 1),
                                    Warnings = lg.GroupBy(m => m.Month)
                                        .Select(m =>
                                        {
                                            var avg = Math.Round(m.Average(x => x.Rsl), 1);
                                            var status = GetRslStatus(avg);
                                            
                                            if (status != "optimal")
                                            {
                                                var monthName = new DateTime(year, m.Key, 1).ToString("MMM", new CultureInfo("id-ID"));
                                                return $"{monthName}: {GetStatusMessage(avg, status)}";
                                            }
                                            return null;
                                        })
                                        .Where(w => w != null)
                                        .ToList()!
                                })
                    })
                    .OrderBy(t => t.TowerName)
                    .ToList();

                _logger.LogInformation("✅ GetYearlyAsync completed - {Count} towers found", result.Count);
                
                return new NecYearlySummaryDto { Year = year, Towers = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetYearlyAsync");
                throw;
            }
        }

        // ============================================
        // ✅ NEW: YEARLY PIVOT (TIDAK ADA PERUBAHAN)
        // ============================================
        
        public async Task<List<NecYearlyPivotDto>> GetYearlyPivotAsync(int year, string? towerName = null)
        {
            try
            {
                _logger.LogInformation("📊 GetYearlyPivotAsync - Year: {Year}, Tower: {TowerName}", year, towerName);
                
                var start = new DateTime(year, 1, 1);
                var end = start.AddYears(1);

                var query = _context.NecRslHistories
                    .AsNoTracking()
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.NearEndTower)
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.FarEndTower)
                    .Where(h => h.Date >= start && h.Date < end);

                // ✅ Filter by tower if provided
                if (!string.IsNullOrEmpty(towerName))
                {
                    query = query.Where(h => h.NecLink.NearEndTower.Name == towerName);
                }

                var rawData = await query
                    .Select(h => new
                    {
                        LinkId = h.NecLink.Id,
                        LinkName = h.NecLink.LinkName,  // ✅ Use LinkName from NecLink
                        Tower = h.NecLink.NearEndTower.Name,
                        Month = h.Date.Month,
                        Year = h.Date.Year,
                        Rsl = h.RslNearEnd,
                        ExpectedMin = h.NecLink.ExpectedRslMin,
                        ExpectedMax = h.NecLink.ExpectedRslMax,
                        Notes = h.Notes,  // ✅ Include notes
                        Status = h.Status, // ✅ Include status
                    })
                    .ToListAsync();

                _logger.LogInformation("📊 Raw data count: {Count}", rawData.Count);
                
                // ✅ Group by LinkId + LinkName
                var grouped = rawData
                    .GroupBy(x => new { x.LinkId, x.LinkName, x.Tower })
                    .Select(g => 
                    {
                        var monthlyValues = new Dictionary<string, decimal?>();
                        var monthlyNotes = new Dictionary<string, string>();
                        
                        // Generate all months (Jan-Dec)
                        for (int month = 1; month <= 12; month++)
                        {
                            var monthKey = new DateTime(year, month, 1).ToString("MMM-yy", new CultureInfo("en-US"));
                            var monthData = g.Where(x => x.Month == month).ToList();
                            
                            if (monthData.Any())
                            {
                                monthlyValues[monthKey] = Math.Round(monthData.Average(x => x.Rsl), 1);
                                
                                // ✅ Collect notes for this month
                                var note = monthData.FirstOrDefault(x => !string.IsNullOrEmpty(x.Notes))?.Notes;
                                if (!string.IsNullOrEmpty(note))
                                {
                                    monthlyNotes[monthKey] = note;
                                }
                            }
                            else
                            {
                                monthlyValues[monthKey] = null;
                            }
                        }

                        return new NecYearlyPivotDto
                        {
                            LinkName = g.Key.LinkName,
                            Tower = g.Key.Tower,
                            MonthlyValues = monthlyValues,
                            ExpectedRslMin = g.First().ExpectedMin,
                            ExpectedRslMax = g.First().ExpectedMax,
                            Notes = monthlyNotes // ✅ Include notes as Dictionary
                        };
                    })
                    .OrderBy(x => x.Tower)
                    .ThenBy(x => x.LinkName)
                    .ToList();

                _logger.LogInformation("✅ GetYearlyPivotAsync completed - {Count} links found", grouped.Count);
                
                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetYearlyPivotAsync");
                throw;
            }
        }

        // ============================================
        // IMPORT & EXPORT - PERLU DIPERBAIKI
        // ============================================

        public async Task<NecSignalImportResultDto> ImportFromPivotExcelAsync(IFormFile file, int userId)
        {
            _logger.LogInformation("📤 ImportFromPivotExcelAsync - User: {UserId}, File: {FileName}", userId, file?.FileName);
            
            var result = new NecSignalImportResultDto();
            var errors = new List<string>();

            if (file == null || file.Length == 0)
            {
                errors.Add("File Excel tidak boleh kosong.");
                result.Errors = errors;
                result.Message = "Import gagal.";
                return result;
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                errors.Add("Format file harus .xlsx atau .xls");
                result.Errors = errors;
                return result;
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(file.OpenReadStream());
            var ws = package.Workbook.Worksheets.FirstOrDefault();

            if (ws == null || ws.Dimension == null)
            {
                errors.Add("File Excel kosong atau tidak ada sheet.");
                result.Errors = errors;
                return result;
            }

            var rowCount = ws.Dimension.Rows;
            var colCount = ws.Dimension.Columns;
            
            if (rowCount < 2 || colCount < 3)
            {
                errors.Add("Format Excel tidak sesuai. Minimal harus ada kolom No, Link, dan data bulan.");
                result.Errors = errors;
                return result;
            }

            // ✅ Parse header untuk ambil bulan-bulan
            var monthColumns = new Dictionary<int, DateTime>(); // colIndex -> Date

            for (int col = 3; col <= colCount; col++)
            {
                var headerCell = ws.Cells[1, col].Value; // ✅ Ambil raw value, bukan string
                
                if (headerCell == null) continue;
                
                DateTime monthDate;
                
                // ✅ PERBAIKAN: Handle 3 format
                if (headerCell is DateTime dateTimeValue)
                {
                    // Format 1: Excel menyimpan sebagai DateTime object
                    monthDate = new DateTime(dateTimeValue.Year, dateTimeValue.Month, 15);
                    monthColumns[col] = monthDate;
                    _logger.LogInformation("📅 Found DateTime column at col {Col} -> {Date}", col, monthDate);
                }
                else if (headerCell is double doubleValue)
                {
                    // Format 2: Excel numeric date (OLE Automation Date)
                    var excelDate = DateTime.FromOADate(doubleValue);
                    monthDate = new DateTime(excelDate.Year, excelDate.Month, 15);
                    monthColumns[col] = monthDate;
                    _logger.LogInformation("📅 Found numeric date at col {Col} -> {Date}", col, monthDate);
                }
                else if (headerCell is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    // Format 3: String format "Jan-25", "MMM-yy"
                    if (DateTime.TryParseExact(stringValue.Trim(), 
                        new[] { "MMM-yy", "MMM-yyyy", "MMMM-yy", "M/d/yyyy", "MM/dd/yyyy" }, 
                        CultureInfo.InvariantCulture, 
                        DateTimeStyles.None, 
                        out monthDate))
                    {
                        monthColumns[col] = new DateTime(monthDate.Year, monthDate.Month, 15);
                        _logger.LogInformation("📅 Found string date at col {Col}: '{Header}' -> {Date}", 
                            col, stringValue, monthDate);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Could not parse header: {Header} at col {Col}", stringValue, col);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Unknown header type: {Type} at col {Col}", headerCell?.GetType().Name, col);
                }
            }

            if (monthColumns.Count == 0)
            {
                errors.Add("Tidak ditemukan kolom bulan yang valid. Pastikan baris 1 berisi tanggal bulan.");
                result.Errors = errors;
                return result;
            }

            _logger.LogInformation("✅ Found {Count} month columns: {Months}", 
                monthColumns.Count, 
                string.Join(", ", monthColumns.Values.Select(d => d.ToString("MMM-yyyy"))));

            // ✅ Load all links untuk matching
            var linksDict = await _context.NecLinks
                .AsNoTracking()
                .ToDictionaryAsync(l => l.LinkName.Trim(), l => l, StringComparer.OrdinalIgnoreCase);

            var histories = new List<NecRslHistory>();
            int totalDataPoints = 0;

            // ✅ Process each row (link)
            for (int row = 2; row <= rowCount; row++)
            {
                var linkNameCell = ws.Cells[row, 2].GetValue<string>()?.Trim();
                
                if (string.IsNullOrWhiteSpace(linkNameCell))
                {
                    _logger.LogWarning("⚠️ Row {Row}: Empty link name, skipping", row);
                    continue;
                }

                // Find link in database
                if (!linksDict.TryGetValue(linkNameCell, out var link))
                {
                    errors.Add($"Baris {row}: Link '{linkNameCell}' tidak ditemukan di database.");
                    result.FailedRows++;
                    continue;
                }

                // ✅ Process each month column for this link
                foreach (var (colIndex, monthDate) in monthColumns)
                {
                    totalDataPoints++;
                    
                    var rslValue = ws.Cells[row, colIndex].GetValue<double?>();
                    
                    // Skip jika cell kosong atau 0
                    if (!rslValue.HasValue || rslValue.Value == 0)
                    {
                        _logger.LogDebug("⚠️ Row {Row}, Col {Col}: Empty or zero RSL, skipping", row, colIndex);
                        continue;
                    }

                    // Validasi range RSL
                    if (rslValue.Value > -0 || rslValue.Value < -100)
                    {
                        errors.Add($"Baris {row}, Kolom {colIndex}: RSL {rslValue.Value} di luar range (-100 to -0)");
                        result.FailedRows++;
                        continue;
                    }

                    // ✅ Check duplicate
                    var duplicate = await _context.NecRslHistories
                        .AnyAsync(h => h.NecLinkId == link.Id && h.Date.Date == monthDate.Date);

                    if (duplicate)
                    {
                        _logger.LogDebug("⚠️ Duplicate found: {Link} on {Date}, skipping", 
                            linkNameCell, monthDate.ToString("yyyy-MM-dd"));
                        result.FailedRows++;
                        continue;
                    }

                    // ✅ Add to list
                    histories.Add(new NecRslHistory
                    {
                        NecLinkId = link.Id,
                        Date = monthDate.Date,
                        RslNearEnd = (decimal)rslValue.Value,
                        RslFarEnd = null // Pivot format biasanya tidak ada RSL Far End
                    });

                    result.SuccessfulInserts++;
                }
            }

            result.TotalRowsProcessed = totalDataPoints;

            // ✅ Save ke database
            if (histories.Any())
            {
                _logger.LogInformation("💾 Saving {Count} history records from pivot...", histories.Count);
                
                var executionStrategy = _context.Database.CreateExecutionStrategy();
                
                await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.NecRslHistories.AddRange(histories);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        _logger.LogInformation("✅ Successfully saved {Count} history records", histories.Count);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Failed to save history records");
                        throw;
                    }
                });
            }

            result.Errors = errors.Take(50).ToList();
            if (errors.Count > 50) result.Errors.Add("... dan lebih banyak error.");

            result.Message = result.FailedRows == 0
                ? $"Import berhasil! {result.SuccessfulInserts} data points tersimpan."
                : $"Import selesai dengan {result.FailedRows} data points gagal dari total {result.TotalRowsProcessed}.";

            // Activity Log
            try
            {
                await _activityLog.LogAsync(
                    module: "NEC Signal",
                    entityId: null,
                    action: result.FailedRows == 0 ? "ImportPivotSuccess" : "ImportPivotPartial",
                    userId: userId,
                    description: result.Message
                );
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "⚠️ ActivityLog failed for pivot import");
            }

            return result;
        }
        
        public async Task<byte[]> ExportYearlyToExcelAsync(int year, string? towerName = null, int? userId = null)
        {
            try
            {
                _logger.LogInformation("📥 ExportYearlyToExcelAsync - Year: {Year}, Tower: {TowerName}", year, towerName);
                
                var summary = await GetYearlyAsync(year);
                var towers = string.IsNullOrEmpty(towerName)
                    ? summary.Towers
                    : summary.Towers.Where(t => t.TowerName == towerName).ToList();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add($"RSL History {year}");

                // Header
                ws.Cells[1, 1].Value = "No";
                ws.Cells[1, 2].Value = "Link";
                for (int m = 1; m <= 12; m++)
                {
                    ws.Cells[1, m + 2].Value = new DateTime(year, m, 1).ToString("MMM-yy", new CultureInfo("id-ID"));
                }
                ws.Cells[1, 15].Value = "Yearly Avg";
                ws.Cells[1, 16].Value = "Status";

                using (var range = ws.Cells[1, 1, 1, 16])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 102, 204));
                    range.Style.Font.Color.SetColor(Color.White);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int currentRow = 2;
                int no = 1;

                foreach (var tower in towers)
                {
                    foreach (var link in tower.Links)
                    {
                        ws.Cells[currentRow, 1].Value = no++;
                        ws.Cells[currentRow, 2].Value = link.Key;

                        for (int m = 1; m <= 12; m++)
                        {
                            var monthKey = new DateTime(year, m, 1).ToString("MMM", new CultureInfo("en-US"));
                            if (link.Value.MonthlyAvg.TryGetValue(monthKey, out decimal val))
                            {
                                ws.Cells[currentRow, m + 2].Value = val;
                            }
                        }

                        ws.Cells[currentRow, 15].Value = link.Value.YearlyAvg;
                        
                        // ✅ Add Status Column
                        var yearlyStatus = GetRslStatus(link.Value.YearlyAvg);
                        ws.Cells[currentRow, 16].Value = yearlyStatus.ToUpper();
                        
                        currentRow++;
                    }
                }

                // ✅ UPDATED: Conditional Formatting dengan New Thresholds
                if (currentRow > 2)
                {
                    var dataRange = ws.Cells[2, 3, currentRow - 1, 14];
                    var cf = dataRange.ConditionalFormatting.AddThreeColorScale();
                    
                    cf.LowValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                    cf.LowValue.Value = -70;  // Critical range
                    cf.LowValue.Color = Color.DarkRed;

                    cf.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                    cf.MiddleValue.Value = -55;  // Warning/Optimal boundary
                    cf.MiddleValue.Color = Color.Yellow;

                    cf.HighValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                    cf.HighValue.Value = -40;  // Strong range
                    cf.HighValue.Color = Color.Green;
                }

                ws.Cells.AutoFitColumns();
                ws.View.FreezePanes(2, 1);

                if (userId.HasValue)
                {
                    try
                    {
                        await _activityLog.LogAsync(
                            module: "NEC Signal",
                            entityId: null,
                            action: "ExportExcel",
                            userId: userId.Value,
                            description: $"Export RSL History tahun {year} {(towerName == null ? "" : $"Tower: {towerName}")}"
                        );
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogWarning(logEx, "⚠️ ActivityLog failed for export");
                    }
                }

                _logger.LogInformation("✅ ExportYearlyToExcelAsync completed successfully");
                
                return await package.GetAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in ExportYearlyToExcelAsync");
                throw;
            }
        }

        // ============================================
        // CRUD TOWER (SUDAH BENAR - TIDAK PERLU PERUBAHAN)
        // ============================================
        
        public async Task<List<TowerListDto>> GetTowersAsync()
        {
            try
            {
                _logger.LogInformation("📊 GetTowersAsync");
                
                var towers = await _context.Towers
                    .AsNoTracking()
                    .Select(t => new TowerListDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Location = t.Location,
                        LinkCount = t.NearEndLinks.Count + t.FarEndLinks.Count
                    })
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                _logger.LogInformation("✅ GetTowersAsync completed - {Count} towers found", towers.Count);
                
                return towers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetTowersAsync");
                throw;
            }
        }

        public async Task<TowerListDto> CreateTowerAsync(TowerCreateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 CREATE Tower - Name: {Name}, User: {UserId}", dto.Name, userId);

                var exists = await _context.Towers.AnyAsync(t => t.Name == dto.Name.Trim());
                if (exists) throw new ArgumentException("Nama tower sudah ada.");

                var tower = new Tower
                {
                    Name = dto.Name.Trim(),
                    Location = dto.Location?.Trim()
                };

                _context.Towers.Add(tower);
                
                var executionStrategy = _context.Database.CreateExecutionStrategy();
                
                await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("💾 Tower created successfully - ID: {Id}", tower.Id);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Failed to save tower");
                        throw;
                    }
                });

                try
                {
                    await _activityLog.LogAsync(
                        module: "NEC Signal - Tower",
                        entityId: tower.Id,
                        action: "Create",
                        userId: userId,
                        description: $"Membuat tower baru: {tower.Name}"
                    );
                    _logger.LogInformation("✅ ActivityLog recorded for Tower ID: {Id}", tower.Id);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                }

                return new TowerListDto
                {
                    Id = tower.Id,
                    Name = tower.Name,
                    Location = tower.Location,
                    LinkCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CREATE FAILED for Tower: {Name}", dto.Name);
                throw;
            }
        }

        public async Task<TowerListDto> UpdateTowerAsync(TowerUpdateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 UPDATE Tower - ID: {Id}, User: {UserId}", dto.Id, userId);

                var tower = await _context.Towers
                    .AsTracking()
                    .Include(t => t.NearEndLinks)
                    .Include(t => t.FarEndLinks)
                    .FirstOrDefaultAsync(t => t.Id == dto.Id);

                if (tower == null)
                {
                    _logger.LogWarning("❌ Tower not found: {Id}", dto.Id);
                    throw new KeyNotFoundException("Tower tidak ditemukan.");
                }

                var changes = new List<string>();
                var oldName = tower.Name;

                if (!string.IsNullOrWhiteSpace(dto.Name) && tower.Name != dto.Name.Trim())
                {
                    var exists = await _context.Towers
                        .AnyAsync(t => t.Name == dto.Name.Trim() && t.Id != dto.Id);
                    
                    if (exists) 
                        throw new ArgumentException("Nama tower sudah digunakan.");
                    
                    tower.Name = dto.Name.Trim();
                    changes.Add($"Nama: '{oldName}' → '{tower.Name}'");
                }

                if (dto.Location != null)
                {
                    if (tower.Location != dto.Location.Trim())
                    {
                        tower.Location = dto.Location.Trim();
                        changes.Add("Location updated");
                    }
                }
                else
                {
                    _logger.LogInformation("⚠️ Location not provided in request, keeping existing value: {Location}", tower.Location);
                }

                _logger.LogInformation("📊 Update details - Name: {Name}, Location: {Location} (from DTO: {DtoLocation})", 
                    tower.Name, tower.Location, dto.Location);

                var executionStrategy = _context.Database.CreateExecutionStrategy();
                
                return await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var rowsAffected = await _context.SaveChangesAsync();
                        _logger.LogInformation("💾 SaveChanges: {RowsAffected} rows affected", rowsAffected);

                        await transaction.CommitAsync();

                        if (rowsAffected > 0)
                        {
                            try
                            {
                                await _activityLog.LogAsync(
                                    "NEC Signal - Tower",
                                    tower.Id,
                                    "Updated",
                                    userId,
                                    $"Updated Tower: {string.Join(", ", changes.Take(3))}"
                                );
                                _logger.LogInformation("✅ Activity log recorded for Tower ID: {Id}", tower.Id);
                            }
                            catch (Exception logEx)
                            {
                                _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical): {Message}", logEx.Message);
                            }

                            return new TowerListDto
                            {
                                Id = tower.Id,
                                Name = tower.Name,
                                Location = tower.Location,
                                LinkCount = tower.NearEndLinks.Count + tower.FarEndLinks.Count
                            };
                        }

                        throw new Exception("Gagal menyimpan perubahan ke database.");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Transaction failed for Tower ID: {Id}", tower.Id);
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ UPDATE FAILED for Tower ID: {Id}", dto.Id);
                throw;
            }
        }

        public async Task DeleteTowerAsync(int id, int userId)
        {
            _logger.LogInformation($"🗑️ DELETE Tower - ID: {id}, User: {userId}");

            // ✅ Gunakan execution strategy untuk handle retry + transaction
            var strategy = _context.Database.CreateExecutionStrategy();
            
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var tower = await _context.Towers
                        .FirstOrDefaultAsync(t => t.Id == id);
                    
                    if (tower == null)
                    {
                        throw new KeyNotFoundException($"Tower dengan ID {id} tidak ditemukan");
                    }

                    // Check apakah ada link yang menggunakan tower ini
                    var hasLinks = await _context.NecLinks
                        .AnyAsync(l => l.NearEndTowerId == id || l.FarEndTowerId == id);
                    
                    if (hasLinks)
                    {
                        var linkCount = await _context.NecLinks
                            .CountAsync(l => l.NearEndTowerId == id || l.FarEndTowerId == id);
                        
                        throw new InvalidOperationException(
                            $"Tidak dapat menghapus tower '{tower.Name}' karena masih memiliki {linkCount} link terkait. " +
                            "Silakan hapus link terlebih dahulu."
                        );
                    }
                    
                    _context.Towers.Remove(tower);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation($"✅ Tower '{tower.Name}' berhasil dihapus");
                    
                    // Log activity
                    await _activityLog.LogAsync(
                        module: "NEC Signal",
                        action: "DELETE",
                        description: $"Menghapus tower: {tower.Name}",
                        entityId: id,
                        userId: userId
                    );
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"❌ Error saat menghapus tower ID {id}");
                    throw;
                }
            });
        }

        // ============================================
        // CRUD LINK - PERLU DIPERBAIKI
        // ============================================
        
        public async Task<List<NecLinkListDto>> GetLinksAsync()
        {
            try
            {
                _logger.LogInformation("📊 GetLinksAsync");
                
                var links = await _context.NecLinks
                    .AsNoTracking()
                    .Select(l => new NecLinkListDto
                    {
                        Id = l.Id,
                        LinkName = l.LinkName,
                        NearEndTower = l.NearEndTower.Name,
                        FarEndTower = l.FarEndTower.Name,
                        NearEndTowerId = l.NearEndTowerId,  
                        FarEndTowerId = l.FarEndTowerId,    
                        ExpectedRslMin = l.ExpectedRslMin,
                        ExpectedRslMax = l.ExpectedRslMax
                    })
                    .OrderBy(l => l.LinkName)
                    .ToListAsync();

                _logger.LogInformation("✅ GetLinksAsync completed - {Count} links found", links.Count);
                
                return links;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetLinksAsync");
                throw;
            }
        }

        public async Task<NecLinkListDto> CreateLinkAsync(NecLinkCreateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 CREATE Link - Name: {Name}, User: {UserId}", dto.LinkName, userId);

                // ✅ Cukup validasi kombinasi tower unik
                var exists = await _context.NecLinks
                    .AnyAsync(l => l.NearEndTowerId == dto.NearEndTowerId 
                        && l.FarEndTowerId == dto.FarEndTowerId);
                
                if (exists) 
                    throw new ArgumentException($"Sudah ada link dari Tower ID {dto.NearEndTowerId} ke Tower ID {dto.FarEndTowerId}.");

                if (dto.NearEndTowerId == dto.FarEndTowerId)
                    throw new ArgumentException("Near dan Far End tower tidak boleh sama.");

                var nearExists = await _context.Towers.AnyAsync(t => t.Id == dto.NearEndTowerId);
                var farExists = await _context.Towers.AnyAsync(t => t.Id == dto.FarEndTowerId);
                
                if (!nearExists || !farExists) 
                    throw new KeyNotFoundException("Tower tidak ditemukan.");

                var link = new NecLink
                {
                    LinkName = dto.LinkName.Trim(),
                    NearEndTowerId = dto.NearEndTowerId,
                    FarEndTowerId = dto.FarEndTowerId,
                    ExpectedRslMin = dto.ExpectedRslMin,
                    ExpectedRslMax = dto.ExpectedRslMax
                };

                // ✅ PERBAIKAN: Gunakan execution strategy
                var executionStrategy = _context.Database.CreateExecutionStrategy();
                
                return await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.NecLinks.Add(link);
                        await _context.SaveChangesAsync();
                        
                        await _context.Entry(link).Reference(l => l.NearEndTower).LoadAsync();
                        await _context.Entry(link).Reference(l => l.FarEndTower).LoadAsync();
                        
                        await transaction.CommitAsync();
                        
                        _logger.LogInformation("💾 Link created successfully - ID: {Id}", link.Id);

                        try
                        {
                            await _activityLog.LogAsync(
                                module: "NEC Signal - Link",
                                entityId: link.Id,
                                action: "Create",
                                userId: userId,
                                description: $"Membuat link: {link.LinkName} ({link.NearEndTower.Name} → {link.FarEndTower.Name})"
                            );
                            _logger.LogInformation("✅ ActivityLog recorded for Link ID: {Id}", link.Id);
                        }
                        catch (Exception logEx)
                        {
                            _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                        }

                        return new NecLinkListDto
                        {
                            Id = link.Id,
                            LinkName = link.LinkName,
                            NearEndTower = link.NearEndTower.Name,
                            FarEndTower = link.FarEndTower.Name,
                            ExpectedRslMin = link.ExpectedRslMin,
                            ExpectedRslMax = link.ExpectedRslMax
                        };
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Failed to save link");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CREATE FAILED for Link: {Name}", dto.LinkName);
                throw;
            }
        }

        public async Task<NecLinkListDto> UpdateLinkAsync(NecLinkUpdateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 UPDATE Link - ID: {Id}, User: {UserId}", dto.Id, userId);

                // ✅ FIX 1: AsTracking() untuk memastikan entity di-track
                var link = await _context.NecLinks
                    .AsTracking()
                    .Include(l => l.NearEndTower)
                    .Include(l => l.FarEndTower)
                    .FirstOrDefaultAsync(l => l.Id == dto.Id);

                if (link == null) 
                    throw new KeyNotFoundException("Link tidak ditemukan.");

                var changes = new List<string>();

                // ✅ FIX 2: Update LinkName tanpa validasi duplikat
                if (link.LinkName != dto.LinkName.Trim())
                {
                    var oldName = link.LinkName;
                    link.LinkName = dto.LinkName.Trim();
                    changes.Add($"Nama: '{oldName}' → '{link.LinkName}'");
                }

                // ✅ FIX 3: Validasi Near End ≠ Far End
                if (dto.NearEndTowerId == dto.FarEndTowerId)
                    throw new ArgumentException("Near dan Far End tidak boleh sama.");

                // ✅ FIX 4: Validasi tower exists
                var nearExists = await _context.Towers.AnyAsync(t => t.Id == dto.NearEndTowerId);
                var farExists = await _context.Towers.AnyAsync(t => t.Id == dto.FarEndTowerId);
                if (!nearExists || !farExists) 
                    throw new KeyNotFoundException("Tower tidak ditemukan.");

                // ✅ FIX 5: Validasi kombinasi tower unik (jika tower berubah)
                if (link.NearEndTowerId != dto.NearEndTowerId || link.FarEndTowerId != dto.FarEndTowerId)
                {
                    var combinationExists = await _context.NecLinks
                        .AnyAsync(l => l.NearEndTowerId == dto.NearEndTowerId 
                            && l.FarEndTowerId == dto.FarEndTowerId
                            && l.Id != dto.Id);
                    
                    if (combinationExists)
                        throw new ArgumentException($"Sudah ada link dari Tower ID {dto.NearEndTowerId} ke Tower ID {dto.FarEndTowerId}.");
                }

                // ✅ FIX 6: Update properties
                if (link.NearEndTowerId != dto.NearEndTowerId)
                {
                    link.NearEndTowerId = dto.NearEndTowerId;
                    changes.Add("NearEndTower updated");
                }

                if (link.FarEndTowerId != dto.FarEndTowerId)
                {
                    link.FarEndTowerId = dto.FarEndTowerId;
                    changes.Add("FarEndTower updated");
                }

                if (link.ExpectedRslMin != dto.ExpectedRslMin)
                {
                    link.ExpectedRslMin = dto.ExpectedRslMin;
                    changes.Add("ExpectedRslMin updated");
                }

                if (link.ExpectedRslMax != dto.ExpectedRslMax)
                {
                    link.ExpectedRslMax = dto.ExpectedRslMax;
                    changes.Add("ExpectedRslMax updated");
                }

                // ✅ FIX 7: Check apakah ada perubahan
                if (changes.Count == 0)
                {
                    _logger.LogInformation("⚠️ No changes detected for Link ID: {Id}", dto.Id);
                    
                    // Return current state tanpa save
                    return new NecLinkListDto
                    {
                        Id = link.Id,
                        LinkName = link.LinkName,
                        NearEndTower = link.NearEndTower.Name,
                        FarEndTower = link.FarEndTower.Name,
                        NearEndTowerId = link.NearEndTowerId,
                        FarEndTowerId = link.FarEndTowerId,
                        ExpectedRslMin = link.ExpectedRslMin,
                        ExpectedRslMax = link.ExpectedRslMax
                    };
                }

                _logger.LogInformation("📝 Changes detected: {Changes}", string.Join(", ", changes));

                // ✅ FIX 8: Simplified save WITHOUT nested transaction
                try
                {
                    var rowsAffected = await _context.SaveChangesAsync();
                    _logger.LogInformation("💾 SaveChanges: {RowsAffected} rows affected", rowsAffected);

                    // ✅ Activity log (dengan try-catch terpisah)
                    try
                    {
                        await _activityLog.LogAsync(
                            "NEC Signal - Link",
                            link.Id,
                            "Updated",
                            userId,
                            $"Updated Link: {string.Join(", ", changes.Take(3))}"
                        );
                        _logger.LogInformation("✅ ActivityLog recorded for Link ID: {Id}", link.Id);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                    }

                    // ✅ Reload navigation properties untuk ensure fresh data
                    await _context.Entry(link).Reference(l => l.NearEndTower).LoadAsync();
                    await _context.Entry(link).Reference(l => l.FarEndTower).LoadAsync();

                    return new NecLinkListDto
                    {
                        Id = link.Id,
                        LinkName = link.LinkName,
                        NearEndTower = link.NearEndTower.Name,
                        FarEndTower = link.FarEndTower.Name,
                        NearEndTowerId = link.NearEndTowerId,
                        FarEndTowerId = link.FarEndTowerId,
                        ExpectedRslMin = link.ExpectedRslMin,
                        ExpectedRslMax = link.ExpectedRslMax
                    };
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "❌ Database update error for Link ID: {Id}", dto.Id);
                    throw new Exception($"Gagal menyimpan perubahan: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ UPDATE FAILED for Link ID: {Id}", dto.Id);
                throw;
            }
        }

        public async Task DeleteLinkAsync(int id, int userId)
        {
            _logger.LogInformation($"🗑️ DELETE Link - ID: {id}, User: {userId}");

            // ✅ Gunakan execution strategy untuk handle retry + transaction
            var strategy = _context.Database.CreateExecutionStrategy();
            
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var link = await _context.NecLinks
                        .FirstOrDefaultAsync(l => l.Id == id);
                    
                    if (link == null)
                    {
                        throw new KeyNotFoundException($"Link dengan ID {id} tidak ditemukan");
                    }

                    // Hapus semua histories terkait terlebih dahulu (karena FK constraint)
                    var histories = await _context.NecRslHistories
                        .Where(h => h.NecLinkId == id)
                        .ToListAsync();
                    
                    if (histories.Any())
                    {
                        _context.NecRslHistories.RemoveRange(histories);
                        _logger.LogInformation($"📊 Menghapus {histories.Count} history terkait");
                    }
                    
                    // Hapus link
                    _context.NecLinks.Remove(link);
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation($"✅ Link '{link.LinkName}' berhasil dihapus dengan {histories.Count} histories");
                    
                    // Log activity
                    await _activityLog.LogAsync(
                        module: "NEC Signal",
                        action: "DELETE",
                        description: $"Menghapus link: {link.LinkName} dengan {histories.Count} histories",
                        entityId: id,
                        userId: userId
                    );
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"❌ Error saat menghapus link ID {id}");
                    throw;
                }
            });
        }

        // ============================================
        // CRUD HISTORY RSL - PERLU DIPERBAIKI
        // ============================================
        
        public async Task<PagedResultDto<NecRslHistoryItemDto>> GetHistoriesAsync(NecRslHistoryQueryDto query)
        {
            try
            {
                _logger.LogInformation("📊 GetHistoriesAsync - Page: {Page}, PageSize: {PageSize}", query.Page, query.PageSize);

                // ✅ CRITICAL: Use AsSplitQuery to prevent LEFT JOIN issues
                var queryable = _context.NecRslHistories
                    .AsNoTracking()
                    .AsSplitQuery() // ✅ This prevents cartesian explosion and NULL issues
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.NearEndTower)
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.FarEndTower)
                    .AsQueryable();

                // Filters
                if (query.NecLinkId.HasValue)
                {
                    queryable = queryable.Where(h => h.NecLinkId == query.NecLinkId.Value);
                }

                if (!string.IsNullOrWhiteSpace(query.FiltersJson))
                {
                    queryable = queryable.ApplyDynamicFiltersNew<NecRslHistory>(query.FiltersJson);
                }

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var term = query.Search.ToLower();
                    queryable = queryable.Where(h =>
                        (h.NecLink != null && h.NecLink.LinkName != null && h.NecLink.LinkName.ToLower().Contains(term)) ||
                        (h.NecLink != null && h.NecLink.NearEndTower != null && h.NecLink.NearEndTower.Name != null && h.NecLink.NearEndTower.Name.ToLower().Contains(term)) ||
                        (h.NecLink != null && h.NecLink.FarEndTower != null && h.NecLink.FarEndTower.Name != null && h.NecLink.FarEndTower.Name.ToLower().Contains(term))
                    );
                }

                var totalCount = await queryable.CountAsync();

                // Sorting
                IQueryable<NecRslHistory> sorted = queryable.OrderByDescending(h => h.Date);

                if (!string.IsNullOrWhiteSpace(query.SortBy))
                {
                    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Date", "Date" },
                        { "RslNearEnd", "RslNearEnd" },
                        { "RslFarEnd", "RslFarEnd" },
                        { "LinkName", "NecLink.LinkName" },
                        { "NearEndTower", "NecLink.NearEndTower.Name" },
                        { "FarEndTower", "NecLink.FarEndTower.Name" }
                    };

                    if (map.TryGetValue(query.SortBy, out var field))
                    {
                        var dir = string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
                        sorted = sorted.ApplySorting(field, dir);
                    }
                }

                // ✅ Pagination
                _logger.LogInformation("🔍 Executing query to load {Count} entities...", query.PageSize);
                
                List<NecRslHistory> entities;
                try
                {
                    entities = await sorted
                        .Skip((query.Page - 1) * query.PageSize)
                        .Take(query.PageSize)
                        .ToListAsync();
                    
                    _logger.LogInformation("✅ Successfully loaded {Count} entities", entities.Count);
                }
                catch (InvalidCastException castEx)
                {
                    _logger.LogError(castEx, "❌ InvalidCastException detected - likely NULL column issue");
                    
                    // ✅ Fallback: Try without includes to identify the problem
                    _logger.LogWarning("⚠️ Attempting fallback query without navigation properties...");
                    
                    var simpleQuery = _context.NecRslHistories
                        .AsNoTracking()
                        .OrderByDescending(h => h.Date)
                        .Skip((query.Page - 1) * query.PageSize)
                        .Take(query.PageSize);
                    
                    entities = await simpleQuery.ToListAsync();
                    
                    // Load navigation properties separately
                    foreach (var entity in entities)
                    {
                        await _context.Entry(entity).Reference(h => h.NecLink).LoadAsync();
                        if (entity.NecLink != null)
                        {
                            await _context.Entry(entity.NecLink).Reference(l => l.NearEndTower).LoadAsync();
                            await _context.Entry(entity.NecLink).Reference(l => l.FarEndTower).LoadAsync();
                        }
                    }
                    
                    _logger.LogInformation("✅ Fallback successful - loaded {Count} entities", entities.Count);
                }

                // ✅ Map to DTO (safe from NULL issues in memory)
                var items = entities.Select(h => new NecRslHistoryItemDto
                {
                    Id = h.Id,
                    NecLinkId = h.NecLinkId,
                    LinkName = h.NecLink?.LinkName ?? "[Missing Link]",
                    NearEndTower = h.NecLink?.NearEndTower?.Name ?? "[Missing Tower]",
                    FarEndTower = h.NecLink?.FarEndTower?.Name ?? "[Missing Tower]",
                    Date = h.Date,
                    RslNearEnd = h.RslNearEnd,
                    RslFarEnd = h.RslFarEnd,
                    Notes = h.Notes, // ✅ Safe - already in memory
                    Status = h.Status
                }).ToList();

                NecRslHistoryItemDto.ApplyListNumbers(items, (query.Page - 1) * query.PageSize);

                _logger.LogInformation("✅ GetHistoriesAsync completed - {Count} items returned", items.Count);

                return new PagedResultDto<NecRslHistoryItemDto>(
                    data: items,
                    page: query.Page,
                    pageSize: query.PageSize,
                    totalCount: totalCount
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetHistoriesAsync");
                throw;
            }
        }

        public async Task<NecRslHistoryItemDto?> GetHistoryByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("📊 GetHistoryByIdAsync - ID: {Id}", id);

                var h = await _context.NecRslHistories
                    .AsNoTracking()
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.NearEndTower)
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.FarEndTower)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (h == null)
                {
                    _logger.LogWarning("⚠️ History not found - ID: {Id}", id);
                    return null;
                }

                _logger.LogInformation("✅ GetHistoryByIdAsync completed - Found history for Link: {LinkName}", h.NecLink.LinkName);
                
                return new NecRslHistoryItemDto
                {
                    Id = h.Id,
                    NecLinkId = h.NecLinkId,
                    LinkName = h.NecLink.LinkName,
                    NearEndTower = h.NecLink.NearEndTower.Name,
                    FarEndTower = h.NecLink.FarEndTower.Name,
                    Date = h.Date,
                    RslNearEnd = h.RslNearEnd,
                    RslFarEnd = h.RslFarEnd,
                    Notes = h.Notes,
                    Status = h.Status,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetHistoryByIdAsync - ID: {Id}", id);
                throw;
            }
        }

        public async Task<NecRslHistoryItemDto> CreateHistoryAsync(NecRslHistoryCreateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 CREATE History - LinkId: {LinkId}, Date: {Date}, User: {UserId}", 
                    dto.NecLinkId, dto.Date, userId);

                var link = await _context.NecLinks
                    .Include(l => l.NearEndTower)
                    .Include(l => l.FarEndTower)
                    .FirstOrDefaultAsync(l => l.Id == dto.NecLinkId)
                    ?? throw new KeyNotFoundException("Link tidak ditemukan.");

                var duplicate = await _context.NecRslHistories
                    .AnyAsync(h => h.NecLinkId == dto.NecLinkId && h.Date.Date == dto.Date.Date);

                if (duplicate)
                    throw new InvalidOperationException($"Data RSL untuk link {link.LinkName} tanggal {dto.Date:yyyy-MM-dd} sudah ada.");

                // ✅ Parse status dari string
                var status = ParseStatus(dto.Status);

                // ✅ Validasi RSL hanya jika status Active
                if (status == NecOperationalStatus.Active)
                {
                    if (dto.RslNearEnd > -10 || dto.RslNearEnd < -100)
                        throw new ArgumentException("RSL Near End harus antara -100 hingga -10 dBm.");
                }

                var history = new NecRslHistory
                {
                    NecLinkId = dto.NecLinkId,
                    Date = dto.Date.Date,
                    RslNearEnd = status == NecOperationalStatus.Active ? dto.RslNearEnd : -100m,
                    RslFarEnd = status == NecOperationalStatus.Active ? dto.RslFarEnd : null,
                    Notes = dto.Notes,
                    Status = status
                };

                var executionStrategy = _context.Database.CreateExecutionStrategy();
                
                return await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.NecRslHistories.Add(history);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        _logger.LogInformation("💾 History created successfully - ID: {Id}", history.Id);

                        try
                        {
                            await _activityLog.LogAsync("NEC Signal - History", history.Id, "Create", userId,
                                $"Tambah RSL: {link.LinkName} - {dto.Date:yyyy-MM-dd} - Status: {status}");
                        }
                        catch (Exception logEx)
                        {
                            _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                        }

                        return new NecRslHistoryItemDto
                        {
                            Id = history.Id,
                            NecLinkId = history.NecLinkId,
                            LinkName = link.LinkName,
                            NearEndTower = link.NearEndTower.Name,
                            FarEndTower = link.FarEndTower.Name,
                            Date = history.Date,
                            RslNearEnd = history.RslNearEnd,
                            RslFarEnd = history.RslFarEnd,
                            Notes = history.Notes,
                            Status = history.Status
                        };
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Failed to save history");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CREATE FAILED for History - LinkId: {LinkId}", dto.NecLinkId);
                throw;
            }
        }


        public async Task<NecRslHistoryItemDto> UpdateHistoryAsync(int id, NecRslHistoryUpdateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 UPDATE History - ID: {Id}, User: {UserId}", id, userId);

                var history = await _context.NecRslHistories
                    .AsTracking()
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.NearEndTower)
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.FarEndTower)
                    .FirstOrDefaultAsync(h => h.Id == id)
                    ?? throw new KeyNotFoundException("History tidak ditemukan.");

                // ✅ Parse status dari string
                var status = ParseStatus(dto.Status);

                // ✅ Validasi RSL hanya jika status Active
                if (status == NecOperationalStatus.Active)
                {
                    if (dto.RslNearEnd > -10 || dto.RslNearEnd < -100)
                        throw new ArgumentException("RSL Near End harus antara -100 hingga -10 dBm.");
                }

                var changes = new List<string>();

                // ✅ Update RSL hanya jika Active
                if (status == NecOperationalStatus.Active)
                {
                    if (history.RslNearEnd != dto.RslNearEnd)
                    {
                        changes.Add($"RslNearEnd: {history.RslNearEnd} → {dto.RslNearEnd}");
                        history.RslNearEnd = dto.RslNearEnd;
                    }

                    if (history.RslFarEnd != dto.RslFarEnd)
                    {
                        changes.Add($"RslFarEnd: {history.RslFarEnd?.ToString() ?? "null"} → {dto.RslFarEnd?.ToString() ?? "null"}");
                        history.RslFarEnd = dto.RslFarEnd;
                    }
                }
                else
                {
                    history.RslNearEnd = -100m;
                    history.RslFarEnd = null;
                }

                if (history.Notes != dto.Notes)
                {
                    changes.Add($"Notes updated");
                    history.Notes = dto.Notes;
                }

                if (history.Status != status)
                {
                    changes.Add($"Status: {history.Status} → {status}");
                    history.Status = status;
                }

                var executionStrategy = _context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var rowsAffected = await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        _logger.LogInformation("💾 History updated successfully - ID: {Id}", id);

                        if (rowsAffected > 0 && changes.Any())
                        {
                            try
                            {
                                await _activityLog.LogAsync("NEC Signal - History", history.Id, "Update", userId,
                                    $"Update RSL: {history.NecLink.LinkName} - {string.Join(", ", changes)}");
                            }
                            catch (Exception logEx)
                            {
                                _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                            }
                        }

                        return new NecRslHistoryItemDto
                        {
                            Id = history.Id,
                            NecLinkId = history.NecLinkId,
                            LinkName = history.NecLink.LinkName,
                            NearEndTower = history.NecLink.NearEndTower.Name,
                            FarEndTower = history.NecLink.FarEndTower.Name,
                            Date = history.Date,
                            RslNearEnd = history.RslNearEnd,
                            RslFarEnd = history.RslFarEnd,
                            Notes = history.Notes,
                            Status = history.Status
                        };
                    }
                    catch (DbUpdateException dbEx)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(dbEx, "❌ Database update error for History ID: {Id}", id);
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ UPDATE FAILED for History ID: {Id}", id);
                throw;
            }
        }

        public async Task DeleteHistoryAsync(int id, int userId)
        {
            _logger.LogInformation($"🗑️ DELETE History - ID: {id}, User: {userId}");

            var history = await _context.NecRslHistories
                .Include(h => h.NecLink)
                .FirstOrDefaultAsync(h => h.Id == id);
            
            if (history == null)
            {
                throw new KeyNotFoundException($"History dengan ID {id} tidak ditemukan");
            }

            try
            {
                _context.NecRslHistories.Remove(history);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"✅ History ID {id} berhasil dihapus");
                
                // Log activity
                await _activityLog.LogAsync(
                    module: "NEC Signal",
                    action: "DELETE",
                    description: $"Menghapus history RSL: {history.NecLink?.LinkName} - {history.Date:yyyy-MM-dd}",
                    entityId: id,
                    userId: userId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error saat menghapus history ID {id}");
                throw;
            }
        }

        //==================  Link Name =================//
        public async Task FixLinkNamesAsync()
        {
            try
            {
                _logger.LogInformation("🔧 Starting FixLinkNamesAsync...");
                
                var links = await _context.NecLinks
                    .Include(l => l.NearEndTower)
                    .Include(l => l.FarEndTower)
                    .ToListAsync();

                int updated = 0;
                
                foreach (var link in links)
                {
                    var expectedName = $"{link.NearEndTower.Name} to {link.FarEndTower.Name}";
                    
                    if (link.LinkName != expectedName)
                    {
                        _logger.LogInformation("🔄 Updating link ID {Id}: '{OldName}' → '{NewName}'", 
                            link.Id, link.LinkName, expectedName);
                        
                        link.LinkName = expectedName;
                        updated++;
                    }
                }

                if (updated > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Fixed {Count} link names", updated);
                }
                else
                {
                    _logger.LogInformation("✅ All link names already correct");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in FixLinkNamesAsync");
                throw;
            }
        }

    }
}