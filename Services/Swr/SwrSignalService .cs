using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting;
using OfficeOpenXml.Style;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models.SWR;
using System.Drawing;
using System.Globalization;
using Pm.Helper;
using Microsoft.Extensions.Logging;
using Pm.Enums;
using Microsoft.AspNetCore.Http;

namespace Pm.Services
{
    public class SwrSignalService : ISwrSignalService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<SwrSignalService> _logger;

        private const decimal GOOD_THRESHOLD = 1.5m; // VSWR < 1.5 = Good, >= 1.5 = Bad

        public SwrSignalService(
            AppDbContext context,
            IActivityLogService activityLog,
            ILogger<SwrSignalService> logger)
        {
            _context = context;
            _activityLog = activityLog;
            _logger = logger;
        }

        private string GetVswrStatus(decimal vswr, decimal expectedMax = 1.5m)
        {
            return vswr < expectedMax ? "good" : "bad";
        }

        private string? GetWarningMessage(decimal avgVswr, decimal expectedMax = 1.5m)
        {
            return avgVswr >= expectedMax ? $"Warning: VSWR {avgVswr:F1} (threshold: {expectedMax:F1})" : null;
        }

        private SwrOperationalStatus ParseStatus(string? statusString)
        {
            if (string.IsNullOrWhiteSpace(statusString))
                return SwrOperationalStatus.Active;

            return statusString.ToLower().Trim() switch
            {
                "active" => SwrOperationalStatus.Active,
                "dismantled" => SwrOperationalStatus.Dismantled,
                "removed" => SwrOperationalStatus.Removed,
                "obstacle" => SwrOperationalStatus.Obstacle,
                _ => SwrOperationalStatus.Active
            };
        }

        private SwrSiteType ParseSiteType(string typeString)
        {
            return typeString.ToLower().Trim() switch
            {
                "trunking" => SwrSiteType.Trunking,
                "conventional" => SwrSiteType.Conventional,
                _ => SwrSiteType.Trunking
            };
        }

        // ============================================
        // MONTHLY & YEARLY SUMMARY REPORTS
        // ============================================

        public async Task<SwrMonthlyHistoryResponseDto> GetMonthlyAsync(int year, int month)
        {
            try
            {
                _logger.LogInformation("📊 GetMonthlyAsync - Year: {Year}, Month: {Month}", year, month);
                if (month < 1 || month > 12) throw new ArgumentException("Bulan tidak valid.");

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1);

                var rawData = await _context.SwrHistories
                    .AsNoTracking()
                    .Where(h => h.Date >= startDate && h.Date < endDate)
                    .GroupBy(h => new
                    {
                        SiteName = h.SwrChannel.SwrSite.Name,
                        SiteType = h.SwrChannel.SwrSite.Type,
                        ChannelName = h.SwrChannel.ChannelName,
                        ExpectedMax = h.SwrChannel.ExpectedSwrMax
                    })
                    .Select(g => new
                    {
                        SiteName = g.Key.SiteName,
                        SiteType = g.Key.SiteType,
                        ChannelName = g.Key.ChannelName,
                        ExpectedMax = g.Key.ExpectedMax,
                        AvgFpwr = g.Average(h => h.Fpwr),
                        AvgVswr = g.Average(h => h.Vswr),
                        OperationalStatus = g.OrderByDescending(h => h.Date)
                            .Select(h => h.Status)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var result = rawData
                    .GroupBy(x => new { x.SiteName, x.SiteType })
                    .Select(sg => new SwrSiteMonthlyDto
                    {
                        SiteName = sg.Key.SiteName,
                        SiteType = sg.Key.SiteType.ToString(),
                        Channels = sg.Select(x => new SwrChannelMonthlyDto
                        {
                            ChannelName = x.ChannelName,
                            AvgFpwr = x.AvgFpwr.HasValue ? Math.Round(x.AvgFpwr.Value, 1) : null,
                            AvgVswr = Math.Round(x.AvgVswr, 1),
                            Status = GetVswrStatus(x.AvgVswr, x.ExpectedMax),
                            WarningMessage = GetWarningMessage(Math.Round(x.AvgVswr, 1), x.ExpectedMax)
                        }).OrderBy(c => c.ChannelName).ToList()
                    })
                    .OrderBy(s => s.SiteName)
                    .ToList();

                _logger.LogInformation("✅ GetMonthlyAsync completed - {Count} sites found", result.Count);
                return new SwrMonthlyHistoryResponseDto
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

        public async Task<SwrYearlySummaryDto> GetYearlyAsync(int year)
        {
            try
            {
                _logger.LogInformation("📊 GetYearlyAsync - Year: {Year}", year);

                var start = new DateTime(year, 1, 1);
                var end = start.AddYears(1);

                var rawData = await _context.SwrHistories
                    .AsNoTracking()
                    .Where(h => h.Date >= start && h.Date < end)
                    .Select(h => new
                    {
                        Site = h.SwrChannel.SwrSite.Name,
                        SiteType = h.SwrChannel.SwrSite.Type,
                        Channel = h.SwrChannel.ChannelName,
                        ExpectedMax = h.SwrChannel.ExpectedSwrMax,
                        Month = h.Date.Month,
                        Fpwr = h.Fpwr,
                        Vswr = h.Vswr
                    })
                    .ToListAsync();

                var result = rawData
                    .GroupBy(x => new { x.Site, x.SiteType })
                    .Select(sg => new SwrSiteYearlyDto
                    {
                        SiteName = sg.Key.Site,
                        SiteType = sg.Key.SiteType.ToString(),
                        Channels = sg.GroupBy(c => new { c.Channel, c.ExpectedMax })
                            .ToDictionary(
                                cg => cg.Key.Channel,
                                cg =>
                                {
                                    var monthlyGroups = cg.GroupBy(m => m.Month)
                                        .Select(mg => new
                                        {
                                            Month = mg.Key,
                                            AvgFpwr = mg.Where(x => x.Fpwr.HasValue)
                                                .Select(x => x.Fpwr!.Value)
                                                .DefaultIfEmpty()
                                                .Average(),
                                            AvgVswr = mg.Average(x => x.Vswr)
                                        })
                                        .ToList();

                                    var yearlyAvgFpwr = monthlyGroups.Any(mg => mg.AvgFpwr != 0)
                                        ? monthlyGroups.Where(mg => mg.AvgFpwr != 0).Average(mg => mg.AvgFpwr)
                                        : (decimal?)null;

                                    var yearlyAvgVswr = monthlyGroups.Average(mg => mg.AvgVswr);

                                    return new SwrChannelYearlyDto
                                    {
                                        MonthlyAvgFpwr = monthlyGroups.ToDictionary(
                                            mg => new DateTime(year, mg.Month, 1).ToString("MMM", CultureInfo.InvariantCulture),
                                            mg => mg.AvgFpwr != 0 ? (decimal?)Math.Round(mg.AvgFpwr, 1) : null
                                        ),
                                        MonthlyAvgVswr = monthlyGroups.ToDictionary(
                                            mg => new DateTime(year, mg.Month, 1).ToString("MMM", CultureInfo.InvariantCulture),
                                            mg => Math.Round(mg.AvgVswr, 1)
                                        ),
                                        YearlyAvgFpwr = yearlyAvgFpwr.HasValue ? Math.Round(yearlyAvgFpwr.Value, 1) : null,
                                        YearlyAvgVswr = Math.Round(yearlyAvgVswr, 1),
                                        Warnings = monthlyGroups
                                            .Select(mg =>
                                            {
                                                var status = GetVswrStatus((decimal)mg.AvgVswr, cg.Key.ExpectedMax);
                                                if (status == "bad")
                                                {
                                                    var monthName = new DateTime(year, mg.Month, 1).ToString("MMM", CultureInfo.InvariantCulture);
                                                    return $"{monthName}: VSWR {mg.AvgVswr:F1} (threshold: {cg.Key.ExpectedMax:F1})";
                                                }
                                                return null;
                                            })
                                            .Where(w => w != null)
                                            .ToList()!
                                    };
                                })
                    })
                    .OrderBy(s => s.SiteName)
                    .ToList();

                _logger.LogInformation("✅ GetYearlyAsync completed - {Count} sites found", result.Count);

                return new SwrYearlySummaryDto
                {
                    Year = year,
                    Sites = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetYearlyAsync");
                throw;
            }
        }

        public async Task<List<SwrYearlyPivotDto>> GetYearlyPivotAsync(int year, string? siteName = null)
        {
            try
            {
                _logger.LogInformation("📊 GetYearlyPivotAsync - Year: {Year}, Site: {SiteName}", year, siteName);

                var start = new DateTime(year, 1, 1);
                var end = start.AddYears(1);

                var query = _context.SwrHistories
                    .AsNoTracking()
                    .Include(h => h.SwrChannel)
                        .ThenInclude(c => c.SwrSite)
                    .Where(h => h.Date >= start && h.Date < end);

                if (!string.IsNullOrEmpty(siteName))
                {
                    query = query.Where(h => h.SwrChannel.SwrSite.Name == siteName);
                }

                var rawData = await query
                    .Select(h => new
                    {
                        ChannelId = h.SwrChannel.Id,
                        ChannelName = h.SwrChannel.ChannelName,
                        Site = h.SwrChannel.SwrSite.Name,
                        SiteType = h.SwrChannel.SwrSite.Type,
                        Month = h.Date.Month,
                        Fpwr = h.Fpwr,
                        Vswr = h.Vswr,
                        ExpectedMax = h.SwrChannel.ExpectedSwrMax,
                        Notes = h.Notes
                    })
                    .ToListAsync();

                _logger.LogInformation("📊 Raw data count: {Count}", rawData.Count);

                var grouped = rawData
                    .GroupBy(x => new { x.ChannelId, x.ChannelName, x.Site, x.SiteType, x.ExpectedMax })
                    .Select(g =>
                    {
                        var monthlyFpwr = new Dictionary<string, decimal?>();
                        var monthlyVswr = new Dictionary<string, decimal?>();
                        var monthlyNotes = new Dictionary<string, string>();

                        for (int month = 1; month <= 12; month++)
                        {
                            var monthKey = new DateTime(year, month, 1).ToString("MMM-yy", CultureInfo.InvariantCulture);
                            var monthData = g.Where(x => x.Month == month).ToList();

                            if (monthData.Any())
                            {
                                var validFpwr = monthData.Where(x => x.Fpwr.HasValue).ToList();
                                monthlyFpwr[monthKey] = validFpwr.Any()
                                    ? Math.Round(validFpwr.Average(x => x.Fpwr!.Value), 1)
                                    : null;

                                monthlyVswr[monthKey] = Math.Round(monthData.Average(x => x.Vswr), 1);

                                var note = monthData.FirstOrDefault(x => !string.IsNullOrEmpty(x.Notes))?.Notes;
                                if (!string.IsNullOrEmpty(note))
                                {
                                    monthlyNotes[monthKey] = note;
                                }
                            }
                            else
                            {
                                monthlyFpwr[monthKey] = null;
                                monthlyVswr[monthKey] = null;
                            }
                        }

                        return new SwrYearlyPivotDto
                        {
                            ChannelName = g.Key.ChannelName,
                            SiteName = g.Key.Site,
                            SiteType = g.Key.SiteType.ToString(),
                            MonthlyFpwr = monthlyFpwr,
                            MonthlyVswr = monthlyVswr,
                            ExpectedSwrMax = g.Key.ExpectedMax,
                            Notes = monthlyNotes
                        };
                    })
                    .OrderBy(x => x.SiteName)
                    .ThenBy(x => x.ChannelName)
                    .ToList();

                _logger.LogInformation("✅ GetYearlyPivotAsync completed - {Count} channels found", grouped.Count);

                return grouped;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetYearlyPivotAsync");
                throw;
            }
        }

        // ============================================
        // IMPORT & EXPORT
        // ============================================

        // ============================================
        // IMPORT FROM EXCEL (GROUPED BY SITE FORMAT)
        // ============================================

        public async Task<SwrImportResultDto> ImportFromExcelAsync(IFormFile file, int userId)
        {
            var result = new SwrImportResultDto();

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                using var package = new ExcelPackage(stream);
                var ws = package.Workbook.Worksheets[0];

                if (ws == null || ws.Dimension == null)
                {
                    result.Errors.Add("Excel file is empty or invalid");
                    return result;
                }

                int currentRow = 1;
                int maxRow = ws.Dimension.End.Row;
                string? currentSiteName = null;
                var monthColumns = new Dictionary<string, (int fpwrCol, int vswrCol)>();

                while (currentRow <= maxRow)
                {
                    // ✅ DETECT SITE HEADER (merged cell with site name)
                    var siteHeaderCell = ws.Cells[currentRow, 1];
                    if (siteHeaderCell.Merge && !string.IsNullOrWhiteSpace(siteHeaderCell.Text))
                    {
                        currentSiteName = siteHeaderCell.Text.Trim();
                        _logger.LogInformation($"📍 Found site: {currentSiteName}");
                        currentRow++;

                        // ✅ PARSE MONTH HEADERS (Row after site header)
                        monthColumns.Clear();
                        var monthHeaderRow = currentRow;

                        for (int col = 3; col <= ws.Dimension.End.Column; col += 2)
                        {
                            var monthCell = ws.Cells[monthHeaderRow, col].Text?.Trim();
                            if (!string.IsNullOrEmpty(monthCell))
                            {
                                // Expected format: "Jan-25", "Feb-25", etc.
                                monthColumns[monthCell] = (fpwrCol: col, vswrCol: col + 1);
                                _logger.LogInformation($"  📅 Month: {monthCell} -> FPWR: Col {col}, VSWR: Col {col + 1}");
                            }
                        }

                        currentRow += 2; // Skip month header + sub-header (FPWR/VSWR)
                        continue;
                    }

                    // ✅ CHECK IF THIS IS AN EMPTY ROW (skip)
                    var noCell = ws.Cells[currentRow, 1].Text?.Trim();
                    if (string.IsNullOrEmpty(noCell))
                    {
                        currentRow++;
                        continue;
                    }

                    // ✅ PARSE DATA ROW
                    if (currentSiteName != null && int.TryParse(noCell, out _))
                    {
                        var channelName = ws.Cells[currentRow, 2].Text?.Trim();

                        if (string.IsNullOrEmpty(channelName))
                        {
                            currentRow++;
                            continue;
                        }

                        _logger.LogInformation($"  📝 Processing: {currentSiteName} - {channelName}");

                        // Find or create site
                        var site = await _context.SwrSites
                            .FirstOrDefaultAsync(s => s.Name == currentSiteName);

                        if (site == null)
                        {
                            result.Errors.Add($"Site not found: {currentSiteName}. Please create the site first.");
                            currentRow++;
                            continue;
                        }

                        // Find or create channel
                        var channel = await _context.SwrChannels
                            .FirstOrDefaultAsync(c => c.ChannelName == channelName && c.SwrSiteId == site.Id);

                        if (channel == null)
                        {
                            channel = new SwrChannel
                            {
                                ChannelName = channelName,
                                SwrSiteId = site.Id,
                                ExpectedSwrMax = 1.5m,
                                ExpectedPwrMax = 100m
                            };
                            _context.SwrChannels.Add(channel);
                            await _context.SaveChangesAsync();
                            result.ChannelsCreated++;
                        }

                        // ✅ PROCESS EACH MONTH
                        foreach (var (monthKey, cols) in monthColumns)
                        {
                            try
                            {
                                // Parse month-year from key (e.g., "Jan-25" -> January 2025)
                                var parts = monthKey.Split('-');
                                if (parts.Length != 2) continue;

                                var monthName = parts[0];
                                var yearShort = parts[1];
                                var year = 2000 + int.Parse(yearShort);
                                var monthNumber = DateTime.ParseExact(monthName, "MMM", CultureInfo.InvariantCulture).Month;

                                // Get FPWR and VSWR values
                                var fpwrText = ws.Cells[currentRow, cols.fpwrCol].Text?.Trim();
                                var vswrText = ws.Cells[currentRow, cols.vswrCol].Text?.Trim();

                                if (string.IsNullOrEmpty(fpwrText) && string.IsNullOrEmpty(vswrText))
                                    continue;

                                decimal? fpwr = null;
                                decimal? vswr = null;

                                if (!string.IsNullOrEmpty(fpwrText) && decimal.TryParse(fpwrText, out var fpwrValue))
                                    fpwr = fpwrValue;

                                if (!string.IsNullOrEmpty(vswrText) && decimal.TryParse(vswrText, out var vswrValue))
                                    vswr = vswrValue;

                                if (fpwr == null && vswr == null)
                                    continue;

                                // ✅ VALIDATION
                                if (vswr.HasValue && (vswr.Value < 1.0m || vswr.Value > 3.0m))
                                {
                                    result.Errors.Add($"Row {currentRow}, {monthKey}: VSWR {vswr.Value} out of range (1.0-3.0)");
                                    continue;
                                }

                                if (fpwr.HasValue && (fpwr.Value < 0 || fpwr.Value > 200))
                                {
                                    result.Errors.Add($"Row {currentRow}, {monthKey}: FPWR {fpwr.Value} out of range (0-200)");
                                    continue;
                                }

                                // Find or create history record
                                var recordDate = new DateTime(year, monthNumber, 1);
                                var history = await _context.SwrHistories
                                    .FirstOrDefaultAsync(h => h.SwrChannelId == channel.Id && h.Date == recordDate);

                                if (history == null)
                                {
                                    history = new SwrHistory
                                    {
                                        SwrChannelId = channel.Id,
                                        Date = recordDate,
                                        Fpwr = fpwr,
                                        Vswr = vswr ?? 1.0m,
                                        Status = SwrOperationalStatus.Active,
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    _context.SwrHistories.Add(history);
                                    result.RecordsCreated++;
                                }
                                else
                                {
                                    if (fpwr.HasValue)
                                        history.Fpwr = fpwr;

                                    if (vswr.HasValue)
                                        history.Vswr = vswr.Value;

                                    // No UpdatedAt field in SwrHistory model
                                    result.RecordsUpdated++;
                                }

                                _logger.LogInformation($"    ✅ {monthKey}: FPWR={fpwr}, VSWR={vswr}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"    ⚠️ Failed to parse {monthKey}: {ex.Message}");
                                result.Errors.Add($"Row {currentRow}, {monthKey}: {ex.Message}");
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    currentRow++;
                }

                // Activity log
                try
                {
                    await _activityLog.LogAsync(
                        module: "SWR Signal",
                        entityId: null,
                        action: "ImportExcel",
                        userId: userId,
                        description: $"Import Excel: {result.RecordsCreated} created, {result.RecordsUpdated} updated, {result.ChannelsCreated} channels created"
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "⚠️ ActivityLog failed for import");
                }

                result.Success = result.Errors.Count == 0;
                _logger.LogInformation($"✅ Import completed: {result.RecordsCreated} created, {result.RecordsUpdated} updated, {result.ChannelsCreated} channels created, {result.Errors.Count} errors");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in ImportFromExcelAsync");
                result.Errors.Add($"Import failed: {ex.Message}");
                return result;
            }
        }


        public async Task<byte[]> ExportYearlyToExcelAsync(int year, string? siteName = null, int? userId = null)
        {
            try
            {
                _logger.LogInformation("📥 ExportYearlyToExcelAsync - Year: {Year}, Site: {SiteName}", year, siteName);

                var pivot = await GetYearlyPivotAsync(year, siteName);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add($"SWR History {year}");

                int currentRow = 1;

                // ✅ Group data by Site
                var groupedBySite = pivot.GroupBy(x => x.SiteName).OrderBy(g => g.Key);

                foreach (var siteGroup in groupedBySite)
                {
                    string currentSiteName = siteGroup.Key;

                    // ✅ SITE HEADER - Row 1: Site Name merged across all columns
                    int totalCols = 2 + (12 * 2); // No, Name Channel + 12 months * 2 cols
                    ws.Cells[currentRow, 1, currentRow, totalCols].Merge = true;
                    ws.Cells[currentRow, 1].Value = currentSiteName;
                    ws.Cells[currentRow, 1].Style.Font.Bold = true;
                    ws.Cells[currentRow, 1].Style.Font.Size = 14;
                    ws.Cells[currentRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[currentRow, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(70, 130, 180)); // Steel Blue
                    ws.Cells[currentRow, 1].Style.Font.Color.SetColor(Color.White);
                    ws.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[currentRow, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    currentRow++;

                    // ✅ MONTH HEADER - Row 2: Jan-25, Feb-25, etc.
                    ws.Cells[currentRow, 1].Value = "No";
                    ws.Cells[currentRow, 2].Value = "Name Channel";
                    ws.Cells[currentRow, 1, currentRow + 1, 1].Merge = true; // Merge No vertically
                    ws.Cells[currentRow, 2, currentRow + 1, 2].Merge = true; // Merge Name Channel vertically

                    int col = 3;
                    for (int m = 1; m <= 12; m++)
                    {
                        var monthName = new DateTime(year, m, 1).ToString("MMM", CultureInfo.InvariantCulture);
                        var monthYear = $"{monthName}-{year.ToString().Substring(2)}";

                        ws.Cells[currentRow, col, currentRow, col + 1].Merge = true;
                        ws.Cells[currentRow, col].Value = monthYear;
                        ws.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        col += 2;
                    }

                    // Style month header row
                    using (var range = ws.Cells[currentRow, 1, currentRow, col - 1])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(100, 149, 237)); // Cornflower Blue
                        range.Style.Font.Color.SetColor(Color.White);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    }

                    currentRow++;

                    // ✅ SUB-HEADER - Row 3: FPWR, VSWR
                    col = 3;
                    for (int m = 1; m <= 12; m++)
                    {
                        ws.Cells[currentRow, col].Value = "FPWR";
                        ws.Cells[currentRow, col + 1].Value = "VSWR";
                        col += 2;
                    }

                    // Style sub-header row
                    using (var range = ws.Cells[currentRow, 1, currentRow, col - 1])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(135, 206, 250)); // Light Sky Blue
                        range.Style.Font.Color.SetColor(Color.Black);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    currentRow++;

                    // ✅ DATA ROWS for this site
                    int no = 1;
                    foreach (var item in siteGroup)
                    {
                        ws.Cells[currentRow, 1].Value = no++;
                        ws.Cells[currentRow, 2].Value = item.ChannelName;

                        col = 3;
                        for (int m = 1; m <= 12; m++)
                        {
                            var monthKey = new DateTime(year, m, 1).ToString("MMM-yy", CultureInfo.InvariantCulture);

                            // FPWR Column
                            if (item.MonthlyFpwr.TryGetValue(monthKey, out decimal? fpwr) && fpwr.HasValue)
                            {
                                ws.Cells[currentRow, col].Value = fpwr.Value;
                                ws.Cells[currentRow, col].Style.Numberformat.Format = "0";
                            }

                            // VSWR Column with color coding
                            if (item.MonthlyVswr.TryGetValue(monthKey, out decimal? vswr) && vswr.HasValue)
                            {
                                var cell = ws.Cells[currentRow, col + 1];
                                cell.Value = vswr.Value;
                                cell.Style.Numberformat.Format = "0.0";

                                // Color coding
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                if (vswr.Value >= 2.0m)
                                {
                                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 99, 71)); // Red
                                    cell.Style.Font.Color.SetColor(Color.White);
                                }
                                else if (vswr.Value >= item.ExpectedSwrMax)
                                {
                                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 165, 0)); // Orange
                                }
                                else
                                {
                                    cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(144, 238, 144)); // Light Green
                                }
                            }

                            col += 2;
                        }

                        // Borders
                        using (var range = ws.Cells[currentRow, 1, currentRow, col - 1])
                        {
                            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        }

                        currentRow++;
                    }

                    // ✅ Add spacing between sites (1 empty row)
                    currentRow++;
                }

                // Auto-fit columns
                ws.Cells.AutoFitColumns();

                // Set minimum width
                for (int c = 3; c <= 26; c++)
                {
                    if (ws.Column(c).Width < 7)
                        ws.Column(c).Width = 7;
                }

                // Freeze first 2 columns
                ws.View.FreezePanes(1, 3);

                if (userId.HasValue)
                {
                    try
                    {
                        await _activityLog.LogAsync(
                            module: "SWR Signal",
                            entityId: null,
                            action: "ExportExcel",
                            userId: userId.Value,
                            description: $"Export SWR History tahun {year} {(siteName == null ? "" : $"Site: {siteName}")}"
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
        // CRUD SITE
        // ============================================

        public async Task<List<SwrSiteListDto>> GetSitesAsync()
        {
            try
            {
                _logger.LogInformation("📊 GetSitesAsync");

                var sites = await _context.SwrSites
                    .AsNoTracking()
                    .Select(s => new SwrSiteListDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Location = s.Location,
                        Type = s.Type.ToString(),
                        ChannelCount = s.Channels.Count
                    })
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                _logger.LogInformation("✅ GetSitesAsync completed - {Count} sites found", sites.Count);

                return sites;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetSitesAsync");
                throw;
            }
        }

        public async Task<SwrSiteListDto> CreateSiteAsync(SwrSiteCreateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 CREATE Site - Name: {Name}, User: {UserId}", dto.Name, userId);

                var site = new SwrSite
                {
                    Name = dto.Name.Trim(),
                    Location = dto.Location?.Trim(),
                    Type = ParseSiteType(dto.Type)
                };

                _context.SwrSites.Add(site);

                var executionStrategy = _context.Database.CreateExecutionStrategy();

                await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("💾 Site created successfully - ID: {Id}", site.Id);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Failed to save site");
                        throw;
                    }
                });

                try
                {
                    await _activityLog.LogAsync(
                        module: "SWR Signal - Site",
                        entityId: site.Id,
                        action: "Create",
                        userId: userId,
                        description: $"Membuat site baru: {site.Name} ({site.Type})"
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                }

                return new SwrSiteListDto
                {
                    Id = site.Id,
                    Name = site.Name,
                    Location = site.Location,
                    Type = site.Type.ToString(),
                    ChannelCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CREATE FAILED for Site: {Name}", dto.Name);
                throw;
            }
        }

        public async Task<SwrSiteListDto> UpdateSiteAsync(SwrSiteUpdateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 UPDATE Site - ID: {Id}, User: {UserId}", dto.Id, userId);

                var site = await _context.SwrSites
                    .AsTracking()
                    .Include(s => s.Channels)
                    .FirstOrDefaultAsync(s => s.Id == dto.Id);

                if (site == null)
                {
                    _logger.LogWarning("❌ Site not found: {Id}", dto.Id);
                    throw new KeyNotFoundException("Site tidak ditemukan.");
                }

                var changes = new List<string>();

                if (site.Name != dto.Name.Trim())
                {
                    var exists = await _context.SwrSites
                        .AnyAsync(s => s.Name == dto.Name.Trim() && s.Id != dto.Id);

                    if (exists)
                        throw new ArgumentException("Nama site sudah digunakan.");

                    changes.Add($"Nama: '{site.Name}' → '{dto.Name}'");
                    site.Name = dto.Name.Trim();
                }

                if (dto.Location != null && site.Location != dto.Location.Trim())
                {
                    site.Location = dto.Location.Trim();
                    changes.Add("Location updated");
                }

                var newType = ParseSiteType(dto.Type);
                if (site.Type != newType)
                {
                    site.Type = newType;
                    changes.Add($"Type: {site.Type} → {newType}");
                }

                var executionStrategy = _context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var rowsAffected = await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        if (rowsAffected > 0 && changes.Any())
                        {
                            try
                            {
                                await _activityLog.LogAsync(
                                    "SWR Signal - Site",
                                    site.Id,
                                    "Updated",
                                    userId,
                                    $"Updated Site: {string.Join(", ", changes)}"
                                );
                            }
                            catch (Exception logEx)
                            {
                                _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                            }
                        }

                        return new SwrSiteListDto
                        {
                            Id = site.Id,
                            Name = site.Name,
                            Location = site.Location,
                            Type = site.Type.ToString(),
                            ChannelCount = site.Channels.Count
                        };
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Transaction failed for Site ID: {Id}", site.Id);
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ UPDATE FAILED for Site ID: {Id}", dto.Id);
                throw;
            }
        }

        public async Task DeleteSiteAsync(int id, int userId)
        {
            _logger.LogInformation("🗑️ DELETE Site - ID: {Id}, User: {UserId}", id, userId);

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var site = await _context.SwrSites
                        .Include(s => s.Channels)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (site == null)
                    {
                        throw new KeyNotFoundException($"Site dengan ID {id} tidak ditemukan");
                    }

                    if (site.Channels.Any())
                    {
                        throw new InvalidOperationException(
                            $"Tidak dapat menghapus site '{site.Name}' karena masih memiliki {site.Channels.Count} channel terkait. " +
                            "Silakan hapus channel terlebih dahulu."
                        );
                    }

                    _context.SwrSites.Remove(site);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("✅ Site '{Name}' berhasil dihapus", site.Name);

                    await _activityLog.LogAsync(
                        module: "SWR Signal",
                        action: "DELETE",
                        description: $"Menghapus site: {site.Name}",
                        entityId: id,
                        userId: userId
                    );
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "❌ Error saat menghapus site ID {Id}", id);
                    throw;
                }
            });
        }

        public async Task<List<SwrChannelListDto>> GetChannelsAsync()
        {
            try
            {
                _logger.LogInformation("📊 GetChannelsAsync");

                var channels = await _context.SwrChannels
                    .AsNoTracking()
                    .Include(c => c.SwrSite)
                    .Select(c => new SwrChannelListDto
                    {
                        Id = c.Id,
                        ChannelName = c.ChannelName,
                        SwrSiteId = c.SwrSiteId,
                        SwrSiteName = c.SwrSite.Name,
                        SwrSiteType = c.SwrSite.Type.ToString(),
                        ExpectedSwrMax = c.ExpectedSwrMax,
                        ExpectedPwrMax = c.ExpectedPwrMax  // ✅ ADDED
                    })
                    .OrderBy(c => c.SwrSiteName)
                    .ThenBy(c => c.ChannelName)
                    .ToListAsync();

                _logger.LogInformation("✅ GetChannelsAsync completed - {Count} channels found", channels.Count);

                return channels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetChannelsAsync");
                throw;
            }
        }

        public async Task<SwrChannelListDto> CreateChannelAsync(SwrChannelCreateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 CREATE Channel - Name: {Name}, User: {UserId}", dto.ChannelName, userId);

                var siteExists = await _context.SwrSites.AnyAsync(s => s.Id == dto.SwrSiteId);
                if (!siteExists)
                    throw new KeyNotFoundException("Site tidak ditemukan.");

                var exists = await _context.SwrChannels
                    .AnyAsync(c => c.ChannelName == dto.ChannelName.Trim() && c.SwrSiteId == dto.SwrSiteId);

                if (exists)
                    throw new ArgumentException($"Channel '{dto.ChannelName}' sudah ada di site ini.");

                // ✅ UPDATED: Tambahkan ExpectedFpwrMax
                var channel = new SwrChannel
                {
                    ChannelName = dto.ChannelName.Trim(),
                    SwrSiteId = dto.SwrSiteId,
                    ExpectedSwrMax = dto.ExpectedSwrMax,
                    ExpectedPwrMax = dto.ExpectedPwrMax  // ✅ ADDED
                };

                var executionStrategy = _context.Database.CreateExecutionStrategy();

                return await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.SwrChannels.Add(channel);
                        await _context.SaveChangesAsync();

                        await _context.Entry(channel).Reference(c => c.SwrSite).LoadAsync();

                        await transaction.CommitAsync();

                        _logger.LogInformation("💾 Channel created successfully - ID: {Id}", channel.Id);

                        try
                        {
                            await _activityLog.LogAsync(
                                module: "SWR Signal - Channel",
                                entityId: channel.Id,
                                action: "Create",
                                userId: userId,
                                description: $"Membuat channel: {channel.ChannelName} di site {channel.SwrSite.Name}"
                            );
                        }
                        catch (Exception logEx)
                        {
                            _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                        }

                        return new SwrChannelListDto
                        {
                            Id = channel.Id,
                            ChannelName = channel.ChannelName,
                            SwrSiteId = channel.SwrSiteId,
                            SwrSiteName = channel.SwrSite.Name,
                            SwrSiteType = channel.SwrSite.Type.ToString(),
                            ExpectedSwrMax = channel.ExpectedSwrMax,
                            ExpectedPwrMax = channel.ExpectedPwrMax  // ✅ ADDED
                        };
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Failed to save channel");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CREATE FAILED for Channel: {Name}", dto.ChannelName);
                throw;
            }
        }

        public async Task<SwrChannelListDto> UpdateChannelAsync(SwrChannelUpdateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 UPDATE Channel - ID: {Id}, User: {UserId}", dto.Id, userId);

                var channel = await _context.SwrChannels
                    .AsTracking()
                    .Include(c => c.SwrSite)
                    .FirstOrDefaultAsync(c => c.Id == dto.Id);

                if (channel == null)
                    throw new KeyNotFoundException("Channel tidak ditemukan.");

                var changes = new List<string>();

                if (channel.ChannelName != dto.ChannelName.Trim())
                {
                    var exists = await _context.SwrChannels
                        .AnyAsync(c => c.ChannelName == dto.ChannelName.Trim()
                            && c.SwrSiteId == dto.SwrSiteId
                            && c.Id != dto.Id);

                    if (exists)
                        throw new ArgumentException($"Channel '{dto.ChannelName}' sudah ada di site ini.");

                    changes.Add($"Nama: '{channel.ChannelName}' → '{dto.ChannelName}'");
                    channel.ChannelName = dto.ChannelName.Trim();
                }

                if (channel.SwrSiteId != dto.SwrSiteId)
                {
                    var siteExists = await _context.SwrSites.AnyAsync(s => s.Id == dto.SwrSiteId);
                    if (!siteExists)
                        throw new KeyNotFoundException("Site tidak ditemukan.");

                    channel.SwrSiteId = dto.SwrSiteId;
                    changes.Add("Site updated");
                }

                if (channel.ExpectedSwrMax != dto.ExpectedSwrMax)
                {
                    changes.Add($"ExpectedSwrMax: {channel.ExpectedSwrMax} → {dto.ExpectedSwrMax}");
                    channel.ExpectedSwrMax = dto.ExpectedSwrMax;
                }

                // ✅ ADDED: Handle ExpectedPwrMax update
                if (channel.ExpectedPwrMax != dto.ExpectedPwrMax)
                {
                    changes.Add($"ExpectedPwrMax: {channel.ExpectedPwrMax} → {dto.ExpectedPwrMax}");
                    channel.ExpectedPwrMax = dto.ExpectedPwrMax;
                }

                if (changes.Count == 0)
                {
                    _logger.LogInformation("⚠️ No changes detected for Channel ID: {Id}", dto.Id);

                    return new SwrChannelListDto
                    {
                        Id = channel.Id,
                        ChannelName = channel.ChannelName,
                        SwrSiteId = channel.SwrSiteId,
                        SwrSiteName = channel.SwrSite.Name,
                        SwrSiteType = channel.SwrSite.Type.ToString(),
                        ExpectedSwrMax = channel.ExpectedSwrMax,
                        ExpectedPwrMax = channel.ExpectedPwrMax  // ✅ ADDED
                    };
                }

                try
                {
                    var rowsAffected = await _context.SaveChangesAsync();
                    _logger.LogInformation("💾 SaveChanges: {RowsAffected} rows affected", rowsAffected);

                    try
                    {
                        await _activityLog.LogAsync(
                            "SWR Signal - Channel",
                            channel.Id,
                            "Updated",
                            userId,
                            $"Updated Channel: {string.Join(", ", changes)}"
                        );
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                    }

                    await _context.Entry(channel).Reference(c => c.SwrSite).LoadAsync();

                    return new SwrChannelListDto
                    {
                        Id = channel.Id,
                        ChannelName = channel.ChannelName,
                        SwrSiteId = channel.SwrSiteId,
                        SwrSiteName = channel.SwrSite.Name,
                        SwrSiteType = channel.SwrSite.Type.ToString(),
                        ExpectedSwrMax = channel.ExpectedSwrMax,
                        ExpectedPwrMax = channel.ExpectedPwrMax  // ✅ ADDED
                    };
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "❌ Database update error for Channel ID: {Id}", dto.Id);
                    throw new Exception($"Gagal menyimpan perubahan: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ UPDATE FAILED for Channel ID: {Id}", dto.Id);
                throw;
            }
        }

        public async Task DeleteChannelAsync(int id, int userId)
        {
            _logger.LogInformation("🗑️ DELETE Channel - ID: {Id}, User: {UserId}", id, userId);

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var channel = await _context.SwrChannels
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (channel == null)
                    {
                        throw new KeyNotFoundException($"Channel dengan ID {id} tidak ditemukan");
                    }

                    // Hapus semua histories terkait terlebih dahulu
                    var histories = await _context.SwrHistories
                        .Where(h => h.SwrChannelId == id)
                        .ToListAsync();

                    if (histories.Any())
                    {
                        _context.SwrHistories.RemoveRange(histories);
                        _logger.LogInformation("📊 Menghapus {Count} history terkait", histories.Count);
                    }

                    _context.SwrChannels.Remove(channel);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("✅ Channel '{Name}' berhasil dihapus dengan {Count} histories", channel.ChannelName, histories.Count);

                    await _activityLog.LogAsync(
                        module: "SWR Signal",
                        action: "DELETE",
                        description: $"Menghapus channel: {channel.ChannelName} dengan {histories.Count} histories",
                        entityId: id,
                        userId: userId
                    );
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "❌ Error saat menghapus channel ID {Id}", id);
                    throw;
                }
            });
        }

        private void ValidateFpwr(decimal? fpwr, decimal? expectedMax, string context)
        {
            if (!fpwr.HasValue) return;

            if (fpwr.Value < 0 || fpwr.Value > 200)
            {
                throw new ArgumentException($"{context}: FPWR {fpwr.Value}W di luar range (0-200W)");
            }

            // Warning log jika melebihi expected
            if (expectedMax.HasValue && fpwr.Value > expectedMax.Value)
            {
                _logger.LogWarning("⚠️ {Context}: FPWR {Fpwr}W melebihi threshold {Expected}W",
                    context, fpwr.Value, expectedMax.Value);
            }
        }

        // ============================================
        // CRUD HISTORY
        // ============================================

        public async Task<PagedResultDto<SwrHistoryItemDto>> GetHistoriesAsync(SwrHistoryQueryDto query)
        {
            try
            {
                _logger.LogInformation("📊 GetHistoriesAsync - Page: {Page}, PageSize: {PageSize}", query.Page, query.PageSize);

                var queryable = _context.SwrHistories
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(h => h.SwrChannel)
                        .ThenInclude(c => c.SwrSite)
                    .AsQueryable();

                // Filters
                if (query.SwrChannelId.HasValue)
                {
                    queryable = queryable.Where(h => h.SwrChannelId == query.SwrChannelId.Value);
                }

                if (query.SwrSiteId.HasValue)
                {
                    queryable = queryable.Where(h => h.SwrChannel.SwrSiteId == query.SwrSiteId.Value);
                }

                if (!string.IsNullOrWhiteSpace(query.SiteType))
                {
                    var siteType = ParseSiteType(query.SiteType);
                    queryable = queryable.Where(h => h.SwrChannel.SwrSite.Type == siteType);
                }

                if (!string.IsNullOrWhiteSpace(query.FiltersJson))
                {
                    queryable = queryable.ApplyDynamicFiltersNew<SwrHistory>(query.FiltersJson);
                }

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var term = query.Search.ToLower();
                    queryable = queryable.Where(h =>
                        (h.SwrChannel != null && h.SwrChannel.ChannelName != null && h.SwrChannel.ChannelName.ToLower().Contains(term)) ||
                        (h.SwrChannel != null && h.SwrChannel.SwrSite != null && h.SwrChannel.SwrSite.Name != null && h.SwrChannel.SwrSite.Name.ToLower().Contains(term))
                    );
                }

                var totalCount = await queryable.CountAsync();

                // Sorting
                IQueryable<SwrHistory> sorted = queryable.OrderByDescending(h => h.Date);

                if (!string.IsNullOrWhiteSpace(query.SortBy))
                {
                    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Date", "Date" },
                        { "Fpwr", "Fpwr" },
                        { "Vswr", "Vswr" },
                        { "ChannelName", "SwrChannel.ChannelName" },
                        { "SiteName", "SwrChannel.SwrSite.Name" }
                    };

                    if (map.TryGetValue(query.SortBy, out var field))
                    {
                        var dir = string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
                        sorted = sorted.ApplySorting(field, dir);
                    }
                }

                _logger.LogInformation("🔍 Executing query to load {Count} entities...", query.PageSize);

                List<SwrHistory> entities;
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
                    _logger.LogError(castEx, "❌ InvalidCastException detected");

                    _logger.LogWarning("⚠️ Attempting fallback query without navigation properties...");

                    var simpleQuery = _context.SwrHistories
                        .AsNoTracking()
                        .OrderByDescending(h => h.Date)
                        .Skip((query.Page - 1) * query.PageSize)
                        .Take(query.PageSize);

                    entities = await simpleQuery.ToListAsync();

                    foreach (var entity in entities)
                    {
                        await _context.Entry(entity).Reference(h => h.SwrChannel).LoadAsync();
                        if (entity.SwrChannel != null)
                        {
                            await _context.Entry(entity.SwrChannel).Reference(c => c.SwrSite).LoadAsync();
                        }
                    }

                    _logger.LogInformation("✅ Fallback successful - loaded {Count} entities", entities.Count);
                }

                var items = entities.Select(h => new SwrHistoryItemDto
                {
                    Id = h.Id,
                    SwrChannelId = h.SwrChannelId,
                    ChannelName = h.SwrChannel?.ChannelName ?? "[Missing Channel]",
                    SiteName = h.SwrChannel?.SwrSite?.Name ?? "[Missing Site]",
                    SiteType = h.SwrChannel?.SwrSite?.Type.ToString() ?? "Unknown",
                    Date = h.Date,
                    Fpwr = h.Fpwr,
                    Vswr = h.Vswr,
                    Notes = h.Notes,
                    Status = h.Status
                }).ToList();

                SwrHistoryItemDto.ApplyListNumbers(items, (query.Page - 1) * query.PageSize);

                _logger.LogInformation("✅ GetHistoriesAsync completed - {Count} items returned", items.Count);

                return new PagedResultDto<SwrHistoryItemDto>(
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

        public async Task<SwrHistoryItemDto?> GetHistoryByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("📊 GetHistoryByIdAsync - ID: {Id}", id);

                var h = await _context.SwrHistories
                    .AsNoTracking()
                    .Include(h => h.SwrChannel)
                        .ThenInclude(c => c.SwrSite)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (h == null)
                {
                    _logger.LogWarning("⚠️ History not found - ID: {Id}", id);
                    return null;
                }

                _logger.LogInformation("✅ GetHistoryByIdAsync completed - Found history for Channel: {ChannelName}", h.SwrChannel.ChannelName);

                return new SwrHistoryItemDto
                {
                    Id = h.Id,
                    SwrChannelId = h.SwrChannelId,
                    ChannelName = h.SwrChannel.ChannelName,
                    SiteName = h.SwrChannel.SwrSite.Name,
                    SiteType = h.SwrChannel.SwrSite.Type.ToString(),
                    Date = h.Date,
                    Fpwr = h.Fpwr,
                    Vswr = h.Vswr,
                    Notes = h.Notes,
                    Status = h.Status
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetHistoryByIdAsync - ID: {Id}", id);
                throw;
            }
        }

        public async Task<SwrHistoryItemDto> CreateHistoryAsync(SwrHistoryCreateDto dto, int userId)
        {
            var status = ParseStatus(dto.Status);

            // Validasi: Notes wajib jika status bukan Active
            if (status != SwrOperationalStatus.Active && string.IsNullOrWhiteSpace(dto.Notes))
            {
                throw new ArgumentException("Catatan wajib diisi untuk status non-Active");
            }

            var channel = await _context.SwrChannels
                .Include(c => c.SwrSite)
                .FirstOrDefaultAsync(c => c.Id == dto.SwrChannelId)
                ?? throw new KeyNotFoundException($"Channel dengan ID {dto.SwrChannelId} tidak ditemukan");

            // Validasi VSWR
            if (dto.Vswr < 1.0m || dto.Vswr > 4.0m)
                throw new ArgumentException("VSWR harus antara 1.0 hingga 4.0");

            // Validasi FPWR untuk Trunking
            if (channel.SwrSite.Type == SwrSiteType.Trunking && dto.Fpwr.HasValue)
            {
                if (dto.Fpwr.Value < 0 || dto.Fpwr.Value > 200)
                    throw new ArgumentException("FPWR harus antara 0 hingga 200");
            }

            var history = new SwrHistory
            {
                SwrChannelId = dto.SwrChannelId,
                Date = dto.Date.Date,
                Fpwr = dto.Fpwr,
                Vswr = dto.Vswr,
                Notes = dto.Notes,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _context.SwrHistories.Add(history);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync(
                module: "SWR Signal - History",
                entityId: history.Id,
                action: "Create",
                userId: userId,
                description: $"Menambahkan history SWR untuk channel {channel.ChannelName} pada {dto.Date:dd/MM/yyyy}"
            );

            return new SwrHistoryItemDto
            {
                Id = history.Id,
                SwrChannelId = history.SwrChannelId,
                ChannelName = channel.ChannelName,
                SiteName = channel.SwrSite.Name,
                SiteType = channel.SwrSite.Type.ToString(),
                Date = history.Date,
                Fpwr = history.Fpwr,
                Vswr = history.Vswr,
                Notes = history.Notes,
                Status = history.Status
            };
        }

        public async Task<SwrHistoryItemDto> UpdateHistoryAsync(int id, SwrHistoryUpdateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 UPDATE History - ID: {Id}, User: {UserId}", id, userId);

                var history = await _context.SwrHistories
                    .AsTracking()
                    .Include(h => h.SwrChannel)
                        .ThenInclude(c => c.SwrSite)
                    .FirstOrDefaultAsync(h => h.Id == id)
                    ?? throw new KeyNotFoundException("History tidak ditemukan.");

                var status = ParseStatus(dto.Status);

                // Validasi: Notes wajib jika status bukan Active
                if (status != SwrOperationalStatus.Active && string.IsNullOrWhiteSpace(dto.Notes))
                {
                    throw new ArgumentException("Catatan wajib diisi untuk status non-Active");
                }

                // Validasi VSWR
                if (dto.Vswr < 1.0m || dto.Vswr > 4.0m)
                    throw new ArgumentException("VSWR harus antara 1.0 hingga 4.0");

                // Validasi FPWR untuk Trunking
                if (history.SwrChannel.SwrSite.Type == SwrSiteType.Trunking && dto.Fpwr.HasValue)
                {
                    if (dto.Fpwr.Value < 0 || dto.Fpwr.Value > 200)
                        throw new ArgumentException("FPWR harus antara 0 hingga 200");
                }

                var changes = new List<string>();

                if (history.Fpwr != dto.Fpwr)
                {
                    changes.Add($"Fpwr: {history.Fpwr?.ToString() ?? "null"} → {dto.Fpwr?.ToString() ?? "null"}");
                    history.Fpwr = dto.Fpwr;
                }

                if (history.Vswr != dto.Vswr)
                {
                    changes.Add($"Vswr: {history.Vswr} → {dto.Vswr}");
                    history.Vswr = dto.Vswr;
                }

                if (history.Notes != dto.Notes)
                {
                    changes.Add("Notes updated");
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
                                await _activityLog.LogAsync("SWR Signal - History", history.Id, "Update", userId,
                                    $"Update SWR: {history.SwrChannel.ChannelName} - {string.Join(", ", changes)}");
                            }
                            catch (Exception logEx)
                            {
                                _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                            }
                        }

                        return new SwrHistoryItemDto
                        {
                            Id = history.Id,
                            SwrChannelId = history.SwrChannelId,
                            ChannelName = history.SwrChannel.ChannelName,
                            SiteName = history.SwrChannel.SwrSite.Name,
                            SiteType = history.SwrChannel.SwrSite.Type.ToString(),
                            Date = history.Date,
                            Fpwr = history.Fpwr,
                            Vswr = history.Vswr,
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
            _logger.LogInformation("🗑️ DELETE History - ID: {Id}, User: {UserId}", id, userId);

            var history = await _context.SwrHistories
                .Include(h => h.SwrChannel)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (history == null)
            {
                throw new KeyNotFoundException($"History dengan ID {id} tidak ditemukan");
            }

            try
            {
                _context.SwrHistories.Remove(history);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ History ID {Id} berhasil dihapus", id);

                await _activityLog.LogAsync(
                    module: "SWR Signal",
                    action: "DELETE",
                    description: $"Menghapus history SWR: {history.SwrChannel?.ChannelName} - {history.Date:yyyy-MM-dd}",
                    entityId: id,
                    userId: userId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error saat menghapus history ID {Id}", id);
                throw;
            }
        }
    }
}