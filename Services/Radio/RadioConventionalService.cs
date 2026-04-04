using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.Radio;
using Pm.Helper;
using Pm.Models;
using System.Text;
using OfficeOpenXml;

namespace Pm.Services
{
    public class RadioConventionalService : IRadioConventionalService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RadioConventionalService> _logger;
        private readonly IActivityLogService _activityLog;

        public RadioConventionalService(AppDbContext context, ILogger<RadioConventionalService> logger, IActivityLogService activityLog)
        {
            _context = context;
            _logger = logger;
            _activityLog = activityLog;
        }

        public async Task<PagedResultDto<RadioConventionalDto>> GetAllAsync(RadioConventionalQueryDto query)
        {
            var queryable = _context.RadioConventionals
                .Include(r => r.Grafir)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                queryable = queryable.Where(r =>
                    r.UnitNumber.ToLower().Contains(search) ||
                    r.RadioId.ToLower().Contains(search) ||
                    (r.SerialNumber != null && r.SerialNumber.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
                queryable = queryable.Where(r => r.Status == query.Status);
            if (!string.IsNullOrWhiteSpace(query.Dept))
                queryable = queryable.Where(r => r.Dept == query.Dept);

            queryable = queryable.ApplySorting(query.SortBy ?? "createdAt", query.SortDir ?? "desc");

            var totalCount = await queryable.CountAsync();
            var items = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(r => MapToDto(r))
                .ToListAsync();

            return new PagedResultDto<RadioConventionalDto>(items, query, totalCount);
        }

        public async Task<RadioConventionalDto?> GetByIdAsync(int id)
        {
            var entity = await _context.RadioConventionals
                .Include(r => r.Grafir)
                .FirstOrDefaultAsync(r => r.Id == id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<RadioConventionalDto> CreateAsync(CreateRadioConventionalDto dto, int userId)
        {
            var entity = new RadioConventional
            {
                UnitNumber = dto.UnitNumber,
                RadioId = dto.RadioId,
                SerialNumber = dto.SerialNumber,
                Dept = dto.Dept,
                Fleet = dto.Fleet,
                RadioType = dto.RadioType,
                Frequency = dto.Frequency,
                Status = dto.Status,
                GrafirId = dto.GrafirId,
                CreatedBy = userId
            };

            _context.RadioConventionals.Add(entity);
            await _context.SaveChangesAsync();

            // Add history
            _context.RadioConventionalHistories.Add(new RadioConventionalHistory
            {
                RadioConventionalId = entity.Id,
                NewUnitNumber = entity.UnitNumber,
                NewDept = entity.Dept,
                NewFleet = entity.Fleet,
                ChangeType = "Create",
                ChangedBy = userId
            });
            await _context.SaveChangesAsync();

            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio Conventional", entity.Id, "Create", userId, $"Created Radio Conventional {entity.UnitNumber}");

            return MapToDto(entity);
        }

        public async Task<RadioConventionalDto?> UpdateAsync(int id, UpdateRadioConventionalDto dto, int userId)
        {
            var entity = await _context.RadioConventionals.FindAsync(id);
            if (entity == null) return null;

            var hasChanges = entity.UnitNumber != dto.UnitNumber || entity.Dept != dto.Dept || entity.Fleet != dto.Fleet;

            if (hasChanges)
            {
                _context.RadioConventionalHistories.Add(new RadioConventionalHistory
                {
                    RadioConventionalId = id,
                    PreviousUnitNumber = entity.UnitNumber,
                    PreviousDept = entity.Dept,
                    PreviousFleet = entity.Fleet,
                    NewUnitNumber = dto.UnitNumber ?? entity.UnitNumber,
                    NewDept = dto.Dept ?? entity.Dept,
                    NewFleet = dto.Fleet ?? entity.Fleet,
                    ChangeType = "Transfer",
                    Notes = dto.Notes,
                    ChangedBy = userId
                });
            }

            if (dto.UnitNumber != null) entity.UnitNumber = dto.UnitNumber;
            if (dto.RadioId != null) entity.RadioId = dto.RadioId;
            if (dto.SerialNumber != null) entity.SerialNumber = dto.SerialNumber;
            if (dto.Dept != null) entity.Dept = dto.Dept;
            if (dto.Fleet != null) entity.Fleet = dto.Fleet;
            if (dto.RadioType != null) entity.RadioType = dto.RadioType;
            if (dto.Frequency != null) entity.Frequency = dto.Frequency;
            if (dto.Status != null) entity.Status = dto.Status;
            if (dto.GrafirId.HasValue) entity.GrafirId = dto.GrafirId;

            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio Conventional", id, "Update", userId, $"Updated Radio Conventional {entity.UnitNumber}");

            return MapToDto(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.RadioConventionals.FindAsync(id);
            if (entity == null) return false;
            _context.RadioConventionals.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RadioHistoryDto>> GetHistoryAsync(int radioId)
        {
            return await _context.RadioConventionalHistories
                .Where(h => h.RadioConventionalId == radioId)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new RadioHistoryDto
                {
                    Id = h.Id,
                    RadioId = h.RadioConventionalId,
                    PreviousUnitNumber = h.PreviousUnitNumber,
                    NewUnitNumber = h.NewUnitNumber,
                    PreviousDept = h.PreviousDept,
                    NewDept = h.NewDept,
                    PreviousFleet = h.PreviousFleet,
                    NewFleet = h.NewFleet,
                    ChangeType = h.ChangeType,
                    Notes = h.Notes,
                    ChangedAt = h.ChangedAt,
                    ChangedByName = h.ChangedByUser != null ? h.ChangedByUser.FullName : null
                })
                .ToListAsync();
        }

        public async Task<(int success, int failed, List<string> errors)> ImportCsvAsync(Stream stream, int userId)
        {
            var success = 0;
            var failed = 0;
            var errors = new List<string>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(stream);

            foreach (var worksheet in package.Workbook.Worksheets)
            {
                int rowCount = worksheet.Dimension?.Rows ?? 0;
                int colCount = worksheet.Dimension?.Columns ?? 0;
                if (rowCount == 0) continue;

                // 1. Find Header Row
                int headerRow = 0;
                var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int r = 1; r <= Math.Min(10, rowCount); r++)
                {
                    var rowMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    for (int c = 1; c <= colCount; c++)
                    {
                        var cellText = worksheet.Cells[r, c].Text?.Replace(" ", "").Replace("_", "").Replace(",", "").Replace(".", "").ToLower();
                        if (string.IsNullOrEmpty(cellText)) continue;

                        if (cellText == "unitnumber" || cellText == "unit") rowMap.TryAdd("unitnumber", c);
                        else if (cellText == "radioid" || cellText == "id") rowMap.TryAdd("radioid", c);
                        else if (cellText == "serialnumber" || cellText == "serial" || cellText == "sn") rowMap.TryAdd("serialnumber", c);
                        else if (cellText == "dept" || cellText == "department") rowMap.TryAdd("dept", c);
                        else if (cellText == "fleet") rowMap.TryAdd("fleet", c);
                        else if (cellText == "radiotype" || cellText == "type") rowMap.TryAdd("radiotype", c);
                        else if (cellText == "frequency" || cellText == "freq") rowMap.TryAdd("frequency", c);
                        else if (cellText == "status") rowMap.TryAdd("status", c);
                    }

                    if ((rowMap.ContainsKey("unitnumber") || rowMap.ContainsKey("radioid")) && rowMap.Count >= 3)
                    {
                        columnMap = rowMap;
                        headerRow = r;
                        break;
                    }
                }

                if (headerRow == 0) continue; // Skip non-data sheets

                string? GetValue(int r, string key)
                {
                    if (columnMap.TryGetValue(key, out int colIndex))
                    {
                        var val = worksheet.Cells[r, colIndex].Text?.Trim();
                        return string.IsNullOrWhiteSpace(val) ? null : val;
                    }
                    return null;
                }

                // Pre-load all existing RadioConventionals into memory for fast lookup
                var allExisting = await _context.RadioConventionals.ToListAsync();
                var existingByRadioId = allExisting
                    .Where(rt => !string.IsNullOrEmpty(rt.RadioId))
                    .GroupBy(rt => rt.RadioId!)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                // Process rows — direct entity manipulation
                for (int r = headerRow + 1; r <= rowCount; r++)
                {
                    var unitNumber = GetValue(r, "unitnumber");
                    var radioId = GetValue(r, "radioid");

                    if (string.IsNullOrEmpty(unitNumber) && string.IsNullOrEmpty(radioId)) continue;
                    if (string.IsNullOrEmpty(unitNumber) && !string.IsNullOrEmpty(radioId)) unitNumber = radioId;
                    if (string.IsNullOrEmpty(radioId) && !string.IsNullOrEmpty(unitNumber)) radioId = unitNumber;

                    try
                    {
                        if (existingByRadioId.TryGetValue(radioId!, out var existing))
                        {
                            existing.UnitNumber = unitNumber!;
                            if (GetValue(r, "dept") is string dept) existing.Dept = dept;
                            if (GetValue(r, "fleet") is string fleet) existing.Fleet = fleet;
                            if (GetValue(r, "serialnumber") is string sn) existing.SerialNumber = sn;
                            if (GetValue(r, "radiotype") is string rt2) existing.RadioType = rt2;
                            if (GetValue(r, "frequency") is string freq) existing.Frequency = freq;
                            existing.Status = GetValue(r, "status") ?? existing.Status ?? "Active";
                            existing.UpdatedAt = DateTime.UtcNow;
                            existing.UpdatedBy = userId;
                        }
                        else
                        {
                            var entity = new RadioConventional
                            {
                                UnitNumber = unitNumber!,
                                RadioId = radioId!,
                                Dept = GetValue(r, "dept"),
                                Fleet = GetValue(r, "fleet"),
                                SerialNumber = GetValue(r, "serialnumber"),
                                RadioType = GetValue(r, "radiotype"),
                                Frequency = GetValue(r, "frequency"),
                                Status = GetValue(r, "status") ?? "Active",
                                CreatedBy = userId,
                                UpdatedBy = userId
                            };
                            _context.RadioConventionals.Add(entity);
                            existingByRadioId[radioId!] = entity;
                        }
                        success++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        errors.Add($"Sheet '{worksheet.Name}' Baris {r}: {ex.InnerException?.Message ?? ex.Message}");
                    }
                }

                // Batch save per sheet
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _context.ChangeTracker.Clear();
                    errors.Add($"Sheet '{worksheet.Name}' batch save error: {ex.InnerException?.Message ?? ex.Message}");
                }
            }

            _context.ChangeTracker.Clear();
            await _activityLog.LogAsync("Radio Conventional", null, "Import", userId, $"Imported {success} data (Failed: {failed})");
            return (success, failed, errors);
        }

        public async Task<byte[]> ExportCsvAsync(RadioConventionalQueryDto? query)
        {
            var data = await _context.RadioConventionals.AsNoTracking().ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("UnitNumber,RadioId,SerialNumber,Dept,Fleet,RadioType,Frequency,Status");
            foreach (var item in data)
                sb.AppendLine($"{item.UnitNumber},{item.RadioId},{item.SerialNumber},{item.Dept},{item.Fleet},{item.RadioType},{item.Frequency},{item.Status}");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public byte[] GetImportTemplate()
        {
            return Encoding.UTF8.GetBytes("UnitNumber,RadioId,SerialNumber,Dept,Fleet,RadioType,Frequency,Status\n");
        }

        private static RadioConventionalDto MapToDto(RadioConventional entity) => new()
        {
            Id = entity.Id,
            UnitNumber = entity.UnitNumber,
            RadioId = entity.RadioId,
            SerialNumber = entity.SerialNumber,
            Dept = entity.Dept,
            Fleet = entity.Fleet,
            RadioType = entity.RadioType,
            Frequency = entity.Frequency,
            Status = entity.Status,
            GrafirId = entity.GrafirId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            GrafirInfo = entity.Grafir != null ? new RadioGrafirBasicDto
            {
                Id = entity.Grafir.Id,
                NoAsset = entity.Grafir.NoAsset,
                SerialNumber = entity.Grafir.SerialNumber,
                TypeRadio = entity.Grafir.TypeRadio,
                Div = entity.Grafir.Div
            } : null
        };

        public async Task<int> ClearAllAsync(int userId)
        {
            await _activityLog.LogAsync("Radio Conventional", 0, "Delete", userId, "Cleared ALL Radio Conventional data");
            await _context.RadioConventionalHistories.ExecuteDeleteAsync();
            return await _context.RadioConventionals.ExecuteDeleteAsync();
        }
    }
}
