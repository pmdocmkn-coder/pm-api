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

        public async Task<SwrImportResultDto> ImportFromPivotExcelAsync(IFormFile file, int userId)
        {
            _logger.LogInformation("📤 ImportFromPivotExcelAsync - User: {UserId}, File: {FileName}", userId, file?.FileName);

            var result = new SwrImportResultDto();
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
                errors.Add("Format Excel tidak sesuai. Minimal harus ada kolom No, Channel, dan data bulan.");
                result.Errors = errors;
                return result;
            }

            // Parse header untuk ambil bulan-bulan
            var monthColumns = new Dictionary<int, DateTime>();

            for (int col = 3; col <= colCount; col++)
            {
                var headerCell = ws.Cells[1, col].Value;

                if (headerCell == null) continue;

                DateTime monthDate;

                if (headerCell is DateTime dateTimeValue)
                {
                    monthDate = new DateTime(dateTimeValue.Year, dateTimeValue.Month, 15);
                    monthColumns[col] = monthDate;
                    _logger.LogInformation("📅 Found DateTime column at col {Col} -> {Date}", col, monthDate);
                }
                else if (headerCell is double doubleValue)
                {
                    var excelDate = DateTime.FromOADate(doubleValue);
                    monthDate = new DateTime(excelDate.Year, excelDate.Month, 15);
                    monthColumns[col] = monthDate;
                    _logger.LogInformation("📅 Found numeric date at col {Col} -> {Date}", col, monthDate);
                }
                else if (headerCell is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
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
                }
            }

            if (monthColumns.Count == 0)
            {
                errors.Add("Tidak ditemukan kolom bulan yang valid. Pastikan baris 1 berisi tanggal bulan.");
                result.Errors = errors;
                return result;
            }

            _logger.LogInformation("✅ Found {Count} month columns", monthColumns.Count);

            // Load all channels untuk matching
            var channelsDict = await _context.SwrChannels
                .AsNoTracking()
                .Include(c => c.SwrSite)
                .ToDictionaryAsync(c => c.ChannelName.Trim(), c => c, StringComparer.OrdinalIgnoreCase);

            var histories = new List<SwrHistory>();
            int totalDataPoints = 0;

            // Process each row (channel)
            for (int row = 2; row <= rowCount; row++)
            {
                var channelNameCell = ws.Cells[row, 2].GetValue<string>()?.Trim();

                if (string.IsNullOrWhiteSpace(channelNameCell))
                {
                    _logger.LogWarning("⚠️ Row {Row}: Empty channel name, skipping", row);
                    continue;
                }

                if (!channelsDict.TryGetValue(channelNameCell, out var channel))
                {
                    errors.Add($"Baris {row}: Channel '{channelNameCell}' tidak ditemukan di database.");
                    result.FailedRows++;
                    continue;
                }

                // Process each month column for this channel
                // ✅ UPDATED: Both Trunking and Conventional now use VSWR + FPWR pairs
                foreach (var (colIndex, monthDate) in monthColumns)
                {
                    totalDataPoints++;

                    // FPWR should be in current column, VSWR in next column
                    var fpwrValue = ws.Cells[row, colIndex].GetValue<double?>();
                    var vswrValue = ws.Cells[row, colIndex + 1].GetValue<double?>();

                    decimal? fpwr = null;
                    decimal? vswr = null;

                    if (fpwrValue.HasValue) fpwr = (decimal)fpwrValue.Value;
                    if (vswrValue.HasValue) vswr = (decimal)vswrValue.Value;

                    // Skip if no VSWR data
                    if (!vswr.HasValue || vswr.Value == 0)
                    {
                        _logger.LogDebug("⚠️ Row {Row}, Col {Col}: Empty VSWR, skipping", row, colIndex);
                        continue;
                    }

                    // Validate VSWR range
                    if (vswr.Value < 1.0m || vswr.Value > 3.0m)
                    {
                        errors.Add($"Baris {row}, Kolom {colIndex}: VSWR {vswr.Value} di luar range (1.0-3.0)");
                        result.FailedRows++;
                        continue;
                    }

                    // Validate FPWR range if provided
                    if (fpwr.HasValue && (fpwr.Value < 0 || fpwr.Value > 200))
                    {
                        errors.Add($"Baris {row}, Kolom {colIndex}: FPWR {fpwr.Value} di luar range (0-200)");
                        result.FailedRows++;
                        continue;
                    }

                    // Check duplicate
                    var duplicate = await _context.SwrHistories
                        .AnyAsync(h => h.SwrChannelId == channel.Id && h.Date.Date == monthDate.Date);

                    if (duplicate)
                    {
                        _logger.LogDebug("⚠️ Duplicate found: {Channel} on {Date}, skipping",
                            channelNameCell, monthDate.ToString("yyyy-MM-dd"));
                        result.FailedRows++;
                        continue;
                    }

                    // Add to list
                    histories.Add(new SwrHistory
                    {
                        SwrChannelId = channel.Id,
                        Date = monthDate.Date,
                        Fpwr = fpwr, // Can be null
                        Vswr = vswr.Value,
                        Status = SwrOperationalStatus.Active
                    });

                    result.SuccessfulInserts++;
                }
            }

            result.TotalRowsProcessed = totalDataPoints;

            // Save to database
            if (histories.Any())
            {
                _logger.LogInformation("💾 Saving {Count} history records from pivot...", histories.Count);

                var executionStrategy = _context.Database.CreateExecutionStrategy();

                await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        _context.SwrHistories.AddRange(histories);
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
                    module: "SWR Signal",
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

        public async Task<byte[]> ExportYearlyToExcelAsync(int year, string? siteName = null, int? userId = null)
        {
            try
            {
                _logger.LogInformation("📥 ExportYearlyToExcelAsync - Year: {Year}, Site: {SiteName}", year, siteName);

                var pivot = await GetYearlyPivotAsync(year, siteName);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add($"SWR History {year}");

                // Header
                ws.Cells[1, 1].Value = "No";
                ws.Cells[1, 2].Value = "Site";
                ws.Cells[1, 3].Value = "Channel";
                int col = 4;
                for (int m = 1; m <= 12; m++)
                {
                    var monthName = new DateTime(year, m, 1).ToString("MMM", CultureInfo.InvariantCulture);
                    ws.Cells[1, col].Value = $"{monthName} FPWR";
                    ws.Cells[1, col + 1].Value = $"{monthName} VSWR";
                    col += 2;
                }

                using (var range = ws.Cells[1, 1, 1, col - 1])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(0, 102, 204));
                    range.Style.Font.Color.SetColor(Color.White);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int currentRow = 2;
                int no = 1;

                foreach (var item in pivot)
                {
                    ws.Cells[currentRow, 1].Value = no++;
                    ws.Cells[currentRow, 2].Value = item.SiteName;
                    ws.Cells[currentRow, 3].Value = item.ChannelName;

                    col = 4;
                    for (int m = 1; m <= 12; m++)
                    {
                        var monthKey = new DateTime(year, m, 1).ToString("MMM-yy", CultureInfo.InvariantCulture);

                        if (item.MonthlyFpwr.TryGetValue(monthKey, out decimal? fpwr) && fpwr.HasValue)
                        {
                            ws.Cells[currentRow, col].Value = fpwr.Value;
                        }

                        if (item.MonthlyVswr.TryGetValue(monthKey, out decimal? vswr) && vswr.HasValue)
                        {
                            ws.Cells[currentRow, col + 1].Value = vswr.Value;

                            // Color coding for VSWR
                            var cell = ws.Cells[currentRow, col + 1];
                            if (vswr.Value >= item.ExpectedSwrMax)
                            {
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(Color.Orange);
                            }
                        }

                        col += 2;
                    }

                    currentRow++;
                }

                ws.Cells.AutoFitColumns();
                ws.View.FreezePanes(2, 1);

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

                var exists = await _context.SwrSites.AnyAsync(s => s.Name == dto.Name.Trim());
                if (exists) throw new ArgumentException("Nama site sudah ada.");

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
            if (dto.Vswr < 1.0m || dto.Vswr > 3.0m)
                throw new ArgumentException("VSWR harus antara 1.0 hingga 3.0");

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
                if (dto.Vswr < 1.0m || dto.Vswr > 3.0m)
                    throw new ArgumentException("VSWR harus antara 1.0 hingga 3.0");

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