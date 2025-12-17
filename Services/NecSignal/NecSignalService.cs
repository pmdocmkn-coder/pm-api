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
using Berkat.Helper;
using Microsoft.Extensions.Logging;

namespace Pm.Services
{
    public class NecSignalService : INecSignalService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<NecSignalService> _logger;
        
        private const decimal NormalMax = -30m;
        private const decimal NormalMin = -60m;

        public NecSignalService(
            AppDbContext context, 
            IActivityLogService activityLog,
            ILogger<NecSignalService> logger)
        {
            _context = context;
            _activityLog = activityLog;
            _logger = logger;
        }

        // === MONTHLY & YEARLY ===
        public async Task<NecMonthlyHistoryResponseDto> GetMonthlyAsync(int year, int month)
        {
            try
            {
                _logger.LogInformation("📊 GetMonthlyAsync - Year: {Year}, Month: {Month}", year, month);
                
                if (month < 1 || month > 12) throw new ArgumentException("Bulan tidak valid.");

                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1);

                var rawData = await _context.NecRslHistories
                    .AsNoTracking()
                    .Where(h => h.Date >= startDate && h.Date < endDate)
                    .Select(h => new
                    {
                        TowerName = h.NecLink.NearEndTower.Name,
                        LinkName = h.NecLink.LinkName,
                        Rsl = h.RslNearEnd,
                        LinkMin = h.NecLink.ExpectedRslMin,
                        LinkMax = h.NecLink.ExpectedRslMax
                    })
                    .ToListAsync();

                var grouped = rawData
                    .GroupBy(x => x.TowerName)
                    .Select(tg => new NecTowerMonthlyDto
                    {
                        TowerName = tg.Key,
                        Links = tg.GroupBy(l => l.LinkName)
                            .Select(lg =>
                            {
                                var avg = Math.Round(lg.Average(x => x.Rsl), 1);
                                var maxThreshold = lg.First().LinkMax != 0m ? lg.First().LinkMax : NormalMax;
                                var minThreshold = lg.First().LinkMin != 0m ? lg.First().LinkMin : NormalMin;

                                var status = avg > maxThreshold ? "warning_high" :
                                             avg < minThreshold ? "warning_low" : "normal";

                                var warning = avg > maxThreshold ? $"Terlalu kuat ({avg} dBm)" :
                                              avg < minThreshold ? $"Terlalu lemah ({avg} dBm)" : null;

                                return new NecLinkMonthlyDto
                                {
                                    LinkName = lg.Key,
                                    AvgRsl = avg,
                                    Status = status,
                                    WarningMessage = warning
                                };
                            })
                            .OrderBy(l => l.LinkName)
                            .ToList()
                    })
                    .OrderBy(t => t.TowerName)
                    .ToList();

                _logger.LogInformation("✅ GetMonthlyAsync completed - {Count} towers found", grouped.Count);
                
                return new NecMonthlyHistoryResponseDto
                {
                    Period = startDate.ToString("MMMM yyyy", new CultureInfo("id-ID")),
                    Data = grouped
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetMonthlyAsync - Year: {Year}, Month: {Month}", year, month);
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
                        Rsl = h.RslNearEnd,
                        LinkMin = h.NecLink.ExpectedRslMin,
                        LinkMax = h.NecLink.ExpectedRslMax
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
                                            var maxThreshold = m.First().LinkMax != 0m ? m.First().LinkMax : NormalMax;
                                            var minThreshold = m.First().LinkMin != 0m ? m.First().LinkMin : NormalMin;

                                            if (avg > maxThreshold)
                                                return $"{new DateTime(year, m.Key, 1).ToString("MMM", new CultureInfo("id-ID"))}: Terlalu kuat ({avg} dBm)";
                                            if (avg < minThreshold)
                                                return $"{new DateTime(year, m.Key, 1).ToString("MMM", new CultureInfo("id-ID"))}: Terlalu lemah ({avg} dBm)";
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
                _logger.LogError(ex, "❌ Error in GetYearlyAsync - Year: {Year}", year);
                throw;
            }
        }

        // === IMPORT & EXPORT ===
        public async Task<NecSignalImportResultDto> ImportFromExcelAsync(IFormFile file, int userId)
        {
            _logger.LogInformation("📤 ImportFromExcelAsync - User: {UserId}, File: {FileName}", userId, file?.FileName);
            
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
            if (rowCount < 2)
            {
                errors.Add("Tidak ada data untuk diimport.");
                result.Errors = errors;
                return result;
            }

            result.TotalRowsProcessed = rowCount - 1;

            var linksDict = await _context.NecLinks
                .AsNoTracking()
                .ToDictionaryAsync(l => l.LinkName.Trim(), l => l, StringComparer.OrdinalIgnoreCase);

            var histories = new List<NecRslHistory>();

            for (int row = 2; row <= rowCount; row++)
            {
                var dateCell = ws.Cells[row, 1].GetValue<string>();
                var linkNameCell = ws.Cells[row, 2].GetValue<string>()?.Trim();
                var rslNearCell = ws.Cells[row, 3].GetValue<double?>();
                var rslFarCell = ws.Cells[row, 4].GetValue<double?>();

                if (string.IsNullOrWhiteSpace(linkNameCell))
                {
                    errors.Add($"Baris {row}: Nama link wajib diisi.");
                    result.FailedRows++;
                    continue;
                }

                if (!DateTime.TryParse(dateCell, out DateTime date))
                {
                    errors.Add($"Baris {row}: Format tanggal tidak valid.");
                    result.FailedRows++;
                    continue;
                }

                if (!rslNearCell.HasValue)
                {
                    errors.Add($"Baris {row}: RSL Near End wajib diisi.");
                    result.FailedRows++;
                    continue;
                }

                if (!linksDict.TryGetValue(linkNameCell, out var link))
                {
                    errors.Add($"Baris {row}: Link '{linkNameCell}' tidak ditemukan di database.");
                    result.FailedRows++;
                    continue;
                }

                var duplicate = await _context.NecRslHistories
                    .AnyAsync(h => h.NecLinkId == link.Id && h.Date.Date == date.Date);

                if (duplicate)
                {
                    errors.Add($"Baris {row}: Data untuk {linkNameCell} pada {date:yyyy-MM-dd} sudah ada.");
                    result.FailedRows++;
                    continue;
                }

                histories.Add(new NecRslHistory
                {
                    NecLinkId = link.Id,
                    Date = date.Date,
                    RslNearEnd = (decimal)rslNearCell.Value,
                    RslFarEnd = rslFarCell.HasValue ? (decimal?)rslFarCell.Value : null
                });

                result.SuccessfulInserts++;
            }

            if (histories.Any())
            {
                _logger.LogInformation("💾 Saving {Count} history records to database...", histories.Count);
                
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
            }

            result.Errors = errors.Take(50).ToList();
            if (errors.Count > 50) result.Errors.Add("... dan lebih banyak error.");

            result.Message = result.FailedRows == 0
                ? "Import berhasil semua!"
                : $"Import selesai dengan {result.FailedRows} baris gagal.";

            // ✅ ActivityLog dengan try-catch
            try
            {
                await _activityLog.LogAsync(
                    module: "NEC Signal",
                    entityId: null,
                    action: result.FailedRows == 0 ? "ImportSuccess" : "ImportPartial",
                    userId: userId,
                    description: result.Message + $" ({result.SuccessfulInserts} berhasil)"
                );
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "⚠️ ActivityLog failed for import");
                // Jangan throw - continue tanpa crash
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

                ws.Cells[1, 1].Value = "No";
                ws.Cells[1, 2].Value = "Link";
                for (int m = 1; m <= 12; m++)
                {
                    ws.Cells[1, m + 2].Value = new DateTime(year, m, 1).ToString("MMM-yy", new CultureInfo("id-ID"));
                }
                ws.Cells[1, 15].Value = "Yearly Avg";

                using (var range = ws.Cells[1, 1, 1, 15])
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
                        currentRow++;
                    }
                }

                if (currentRow > 2)
                {
                    var dataRange = ws.Cells[2, 3, currentRow - 1, 14];
                    var cf = dataRange.ConditionalFormatting.AddThreeColorScale();
                    cf.LowValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                    cf.LowValue.Value = -70;
                    cf.LowValue.Color = Color.Red;

                    cf.MiddleValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                    cf.MiddleValue.Value = -45;
                    cf.MiddleValue.Color = Color.Yellow;

                    cf.HighValue.Type = eExcelConditionalFormattingValueObjectType.Num;
                    cf.HighValue.Value = -20;
                    cf.HighValue.Color = Color.Green;
                }

                ws.Cells.AutoFitColumns();
                ws.View.FreezePanes(2, 1);

                // ✅ ActivityLog dengan try-catch
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
                        // Jangan throw - continue tanpa crash
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

        // === CRUD TOWER ===
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

                // Validasi duplikat
                var exists = await _context.Towers.AnyAsync(t => t.Name == dto.Name.Trim());
                if (exists) throw new ArgumentException("Nama tower sudah ada.");

                var tower = new Tower
                {
                    Name = dto.Name.Trim(),
                    Location = dto.Location?.Trim()
                };

                _context.Towers.Add(tower);
                
                // ✅ PERBAIKAN: Gunakan execution strategy untuk save
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

                // ✅ ActivityLog dengan try-catch
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
                    // Jangan throw - continue tanpa crash
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

                // ✅ GET ENTITY dengan tracking
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

                // ✅ LOG perubahan
                var changes = new List<string>();
                var oldName = tower.Name;

                // ✅ VALIDASI duplikat nama jika berbeda
                if (!string.IsNullOrWhiteSpace(dto.Name) && tower.Name != dto.Name.Trim())
                {
                    var exists = await _context.Towers
                        .AnyAsync(t => t.Name == dto.Name.Trim() && t.Id != dto.Id);
                    
                    if (exists) 
                        throw new ArgumentException("Nama tower sudah digunakan.");
                    
                    tower.Name = dto.Name.Trim();
                    changes.Add($"Nama: '{oldName}' → '{tower.Name}'");
                }

                // ✅ UPDATE location JIKA DISERTAKAN dalam request (tidak null)
                // Perhatikan: dto.Location bisa null jika tidak disertakan dalam request
                // Kita hanya update jika ada nilai (tidak null), tapi bisa empty string
                if (dto.Location != null)  // Perbaikan di sini!
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

                // ✅ SAVE CHANGES dengan execution strategy
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
                            // ✅ ACTIVITY LOG DENGAN TRY-CATCH
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
                                // Jangan throw - continue tanpa crash
                            }

                            // Return updated data
                            return new TowerListDto
                            {
                                Id = tower.Id,
                                Name = tower.Name,
                                Location = tower.Location,  // Ini akan tetap ada nilainya
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

        private async Task<TowerListDto> SaveChangesAndReturnAsync(Tower tower, int userId, List<string> changes)
        {
            if (changes.Count == 0)
            {
                _logger.LogInformation("📝 No changes detected for Tower ID: {Id}", tower.Id);
                return new TowerListDto
                {
                    Id = tower.Id,
                    Name = tower.Name,
                    Location = tower.Location,
                    LinkCount = tower.NearEndLinks.Count + tower.FarEndLinks.Count
                };
            }

            _logger.LogInformation("📝 Changes made for Tower ID {Id}: {Changes}", 
                tower.Id, string.Join(", ", changes));

            // ✅ PERBAIKAN: Gunakan execution strategy untuk MySQL
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
                        // ✅ ACTIVITY LOG DENGAN TRY-CATCH (NON-CRITICAL)
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
                            // Jangan throw - continue tanpa crash
                        }

                        // Return updated data
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

        public async Task DeleteTowerAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("🗑️ DELETE Tower - ID: {Id}, User: {UserId}", id, userId);

                var tower = await _context.Towers
                    .AsTracking()
                    .Include(t => t.NearEndLinks)
                    .Include(t => t.FarEndLinks)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tower == null) 
                    throw new KeyNotFoundException("Tower tidak ditemukan.");
                
                if (tower.NearEndLinks.Any() || tower.FarEndLinks.Any())
                    throw new InvalidOperationException("Tower masih memiliki link, tidak bisa dihapus.");

                _context.Towers.Remove(tower);
                
                // ✅ PERBAIKAN: Gunakan execution strategy
                var executionStrategy = _context.Database.CreateExecutionStrategy();
                
                await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var rowsAffected = await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("💾 Tower deleted successfully - ID: {Id}, Rows affected: {Rows}", id, rowsAffected);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Failed to delete tower");
                        throw;
                    }
                });

                // ✅ ACTIVITY LOG DENGAN TRY-CATCH
                try
                {
                    await _activityLog.LogAsync(
                        module: "NEC Signal - Tower",
                        entityId: id,
                        action: "Delete",
                        userId: userId,
                        description: $"Menghapus tower: {tower.Name}"
                    );
                    _logger.LogInformation("✅ ActivityLog recorded for deleted Tower ID: {Id}", id);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                    // Jangan throw - continue tanpa crash
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in DeleteTowerAsync - ID: {Id}", id);
                throw;
            }
        }

        // === CRUD LINK ===
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

                var exists = await _context.NecLinks.AnyAsync(l => l.LinkName == dto.LinkName.Trim());
                if (exists) throw new ArgumentException("Nama link sudah ada.");

                if (dto.NearEndTowerId == dto.FarEndTowerId)
                    throw new ArgumentException("Near dan Far End tower tidak boleh sama.");

                var nearExists = await _context.Towers.AnyAsync(t => t.Id == dto.NearEndTowerId);
                var farExists = await _context.Towers.AnyAsync(t => t.Id == dto.FarEndTowerId);
                if (!nearExists || !farExists) throw new KeyNotFoundException("Tower tidak ditemukan.");

                var link = new NecLink
                {
                    LinkName = dto.LinkName.Trim(),
                    NearEndTowerId = dto.NearEndTowerId,
                    FarEndTowerId = dto.FarEndTowerId,
                    ExpectedRslMin = dto.ExpectedRslMin,
                    ExpectedRslMax = dto.ExpectedRslMax
                };

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.NecLinks.Add(link);
                    await _context.SaveChangesAsync();
                    
                    await _context.Entry(link).Reference(l => l.NearEndTower).LoadAsync();
                    await _context.Entry(link).Reference(l => l.FarEndTower).LoadAsync();
                    
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("💾 Link created successfully - ID: {Id}", link.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "❌ Failed to save link");
                    throw;
                }

                // ✅ ActivityLog dengan try-catch
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
                    // Jangan throw - continue tanpa crash
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
                _logger.LogError(ex, "❌ CREATE FAILED for Link: {Name}", dto.LinkName);
                throw;
            }
        }

        public async Task<NecLinkListDto> UpdateLinkAsync(NecLinkUpdateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("🔄 UPDATE Link - ID: {Id}, User: {UserId}", dto.Id, userId);

                // GET ENTITY dengan tracking
                var link = await _context.NecLinks
                    .AsTracking()
                    .Include(l => l.NearEndTower)
                    .Include(l => l.FarEndTower)
                    .FirstOrDefaultAsync(l => l.Id == dto.Id);

                if (link == null) throw new KeyNotFoundException("Link tidak ditemukan.");

                // LOG perubahan
                var changes = new List<string>();
                var oldName = link.LinkName;

                // VALIDASI duplikat nama jika berbeda
                if (link.LinkName != dto.LinkName.Trim())
                {
                    var exists = await _context.NecLinks.AnyAsync(l => l.LinkName == dto.LinkName.Trim() && l.Id != dto.Id);
                    if (exists) throw new ArgumentException("Nama link sudah digunakan.");
                    
                    link.LinkName = dto.LinkName.Trim();
                    changes.Add($"Nama: '{oldName}' → '{link.LinkName}'");
                }

                // VALIDASI tower tidak sama
                if (dto.NearEndTowerId == dto.FarEndTowerId)
                    throw new ArgumentException("Near dan Far End tidak boleh sama.");

                var nearExists = await _context.Towers.AnyAsync(t => t.Id == dto.NearEndTowerId);
                var farExists = await _context.Towers.AnyAsync(t => t.Id == dto.FarEndTowerId);
                if (!nearExists || !farExists) throw new KeyNotFoundException("Tower tidak ditemukan.");

                // UPDATE lainnya jika berbeda
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

                // ✅ PERBAIKAN: Gunakan execution strategy untuk MySQL
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
                            // ✅ ACTIVITY LOG DENGAN TRY-CATCH
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
                                // Jangan throw - continue tanpa crash
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

                        throw new Exception("Gagal menyimpan perubahan ke database.");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "❌ Transaction failed for Link ID: {Id}", dto.Id);
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ UPDATE FAILED for Link ID: {Id}", dto.Id);
                throw;
            }
        }

        public async Task DeleteLinkAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("🗑️ DELETE Link - ID: {Id}, User: {UserId}", id, userId);

                var link = await _context.NecLinks
                    .AsTracking()
                    .Include(l => l.Histories)
                    .Include(l => l.NearEndTower)
                    .Include(l => l.FarEndTower)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (link == null) throw new KeyNotFoundException("Link tidak ditemukan.");
                
                if (link.Histories.Any())
                    throw new InvalidOperationException("Link memiliki data history RSL, tidak bisa dihapus.");

                var linkName = link.LinkName;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.NecLinks.Remove(link);
                    var rowsAffected = await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("💾 Link deleted successfully - ID: {Id}, Rows affected: {Rows}", id, rowsAffected);

                    // ✅ ACTIVITY LOG DENGAN TRY-CATCH
                    try
                    {
                        await _activityLog.LogAsync(
                            module: "NEC Signal - Link",
                            entityId: id,
                            action: "Delete",
                            userId: userId,
                            description: $"Menghapus link: {linkName}"
                        );
                        _logger.LogInformation("✅ ActivityLog recorded for deleted Link ID: {Id}", id);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                        // Jangan throw - continue tanpa crash
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "❌ Failed to delete link");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in DeleteLinkAsync - ID: {Id}", id);
                throw;
            }
        }

        // === CRUD HISTORY RSL ===
        public async Task<PagedResultDto<NecRslHistoryItemDto>> GetHistoriesAsync(NecRslHistoryQueryDto query)
        {
            try
            {
                _logger.LogInformation("📊 GetHistoriesAsync - Page: {Page}, PageSize: {PageSize}", query.Page, query.PageSize);

                var queryable = _context.NecRslHistories
                    .AsNoTracking()
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.NearEndTower)
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.FarEndTower)
                    .AsQueryable();

                // Filter NecLinkId
                if (query.NecLinkId.HasValue)
                {
                    queryable = queryable.Where(h => h.NecLinkId == query.NecLinkId.Value);
                    _logger.LogInformation("🔍 Filter by NecLinkId: {NecLinkId}", query.NecLinkId.Value);
                }

                // Advanced filters
                if (!string.IsNullOrWhiteSpace(query.FiltersJson))
                {
                    queryable = queryable.ApplyDynamicFiltersNew<NecRslHistory>(query.FiltersJson);
                    _logger.LogInformation("🔍 Applied dynamic filters");
                }

                // Global search
                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var term = query.Search.ToLower();
                    queryable = queryable.Where(h =>
                        h.NecLink.LinkName.ToLower().Contains(term) ||
                        h.NecLink.NearEndTower.Name.ToLower().Contains(term) ||
                        h.NecLink.FarEndTower.Name.ToLower().Contains(term));
                    _logger.LogInformation("🔍 Search term: {Search}", query.Search);
                }

                // Total count setelah semua filter
                var totalCount = await queryable.CountAsync();
                _logger.LogInformation("📊 Total count: {TotalCount}", totalCount);

                // Sorting: default Date desc
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
                        _logger.LogInformation("🔍 Sorting by: {Field} {Direction}", field, dir);
                    }
                }

                // Pagination + projection
                var items = await sorted
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(h => new NecRslHistoryItemDto
                    {
                        Id = h.Id,
                        NecLinkId = h.NecLinkId,
                        LinkName = h.NecLink.LinkName,
                        NearEndTower = h.NecLink.NearEndTower.Name,
                        FarEndTower = h.NecLink.FarEndTower.Name,
                        Date = h.Date,
                        RslNearEnd = h.RslNearEnd,
                        RslFarEnd = h.RslFarEnd
                    })
                    .ToListAsync();

                // Nomor urut
                NecRslHistoryItemDto.ApplyListNumbers(items, (query.Page - 1) * query.PageSize);

                _logger.LogInformation("✅ GetHistoriesAsync completed - {Count} items returned", items.Count);
                
                return new PagedResultDto<NecRslHistoryItemDto>(items, query, totalCount);
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
                    RslFarEnd = h.RslFarEnd
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

                if (dto.RslNearEnd > -10 || dto.RslNearEnd < -100)
                    throw new ArgumentException("RSL Near End harus antara -100 hingga -10 dBm.");

                var history = new NecRslHistory
                {
                    NecLinkId = dto.NecLinkId,
                    Date = dto.Date.Date,
                    RslNearEnd = dto.RslNearEnd,
                    RslFarEnd = dto.RslFarEnd
                };

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.NecRslHistories.Add(history);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("💾 History created successfully - ID: {Id}", history.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "❌ Failed to save history");
                    throw;
                }

                // ✅ ACTIVITY LOG DENGAN TRY-CATCH
                try
                {
                    await _activityLog.LogAsync("NEC Signal - History", history.Id, "Create", userId,
                        $"Tambah manual RSL: {link.LinkName} - {dto.Date:yyyy-MM-dd}");
                    _logger.LogInformation("✅ ActivityLog recorded for History ID: {Id}", history.Id);
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                    // Jangan throw - continue tanpa crash
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
                    RslFarEnd = history.RslFarEnd
                };
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

                // ✅ GET ENTITY dengan tracking
                var history = await _context.NecRslHistories
                    .AsTracking()
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.NearEndTower)
                    .Include(h => h.NecLink)
                        .ThenInclude(l => l.FarEndTower)
                    .FirstOrDefaultAsync(h => h.Id == id)
                    ?? throw new KeyNotFoundException("History tidak ditemukan.");

                if (dto.RslNearEnd > -10 || dto.RslNearEnd < -100)
                    throw new ArgumentException("RSL Near End harus antara -100 hingga -10 dBm.");

                // ✅ LOG perubahan
                var changes = new List<string>();
                
                if (history.RslNearEnd != dto.RslNearEnd)
                {
                    var oldValue = history.RslNearEnd;
                    history.RslNearEnd = dto.RslNearEnd;
                    changes.Add($"RslNearEnd: {oldValue} → {history.RslNearEnd}");
                }

                if (history.RslFarEnd != dto.RslFarEnd)
                {
                    var oldValue = history.RslFarEnd?.ToString() ?? "null";
                    var newValue = dto.RslFarEnd?.ToString() ?? "null";
                    history.RslFarEnd = dto.RslFarEnd;
                    changes.Add($"RslFarEnd: {oldValue} → {newValue}");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var rowsAffected = await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("💾 History updated successfully - ID: {Id}, Rows affected: {Rows}", id, rowsAffected);

                    if (rowsAffected > 0)
                    {
                        // ✅ ACTIVITY LOG DENGAN TRY-CATCH
                        try
                        {
                            await _activityLog.LogAsync("NEC Signal - History", history.Id, "Update", userId,
                                $"Update RSL: {history.NecLink.LinkName} - {history.Date:yyyy-MM-dd}" +
                                (changes.Any() ? $" ({string.Join(", ", changes)})" : ""));
                            _logger.LogInformation("✅ ActivityLog recorded for History ID: {Id}", history.Id);
                        }
                        catch (Exception logEx)
                        {
                            _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                            // Jangan throw - continue tanpa crash
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
                            RslFarEnd = history.RslFarEnd
                        };
                    }

                    throw new Exception("Gagal menyimpan perubahan ke database.");
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "❌ Database update error for History ID: {Id}", id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ UPDATE FAILED for History ID: {Id}", id);
                throw;
            }
        }

        public async Task DeleteHistoryAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("🗑️ DELETE History - ID: {Id}, User: {UserId}", id, userId);

                var history = await _context.NecRslHistories
                    .AsTracking()
                    .Include(h => h.NecLink)
                    .FirstOrDefaultAsync(h => h.Id == id)
                    ?? throw new KeyNotFoundException("History tidak ditemukan.");

                var desc = $"Hapus RSL: {history.NecLink.LinkName} - {history.Date:yyyy-MM-dd}";

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.NecRslHistories.Remove(history);
                    var rowsAffected = await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("💾 History deleted successfully - ID: {Id}, Rows affected: {Rows}", id, rowsAffected);

                    // ✅ ACTIVITY LOG DENGAN TRY-CATCH
                    try
                    {
                        await _activityLog.LogAsync("NEC Signal - History", id, "Delete", userId, desc);
                        _logger.LogInformation("✅ ActivityLog recorded for deleted History ID: {Id}", id);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                        // Jangan throw - continue tanpa crash
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "❌ Failed to delete history");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in DeleteHistoryAsync - ID: {Id}", id);
                throw;
            }
        }
    }
}