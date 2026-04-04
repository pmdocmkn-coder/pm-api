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
    public class RadioGrafirService : IRadioGrafirService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RadioGrafirService> _logger;
        private readonly IActivityLogService _activityLog;

        public RadioGrafirService(AppDbContext context, ILogger<RadioGrafirService> logger, IActivityLogService activityLog)
        {
            _context = context;
            _logger = logger;
            _activityLog = activityLog;
        }

        public async Task<PagedResultDto<RadioGrafirDto>> GetAllAsync(RadioGrafirQueryDto query)
        {
            var queryable = _context.RadioGrafirs
                .Include(g => g.TrunkingRadios)
                .Include(g => g.ConventionalRadios)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                queryable = queryable.Where(g =>
                    g.NoAsset.ToLower().Contains(search) ||
                    g.SerialNumber.ToLower().Contains(search) ||
                    (g.TypeRadio != null && g.TypeRadio.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
                queryable = queryable.Where(g => g.Status == query.Status);
            if (!string.IsNullOrWhiteSpace(query.Div))
                queryable = queryable.Where(g => g.Div == query.Div);
            if (!string.IsNullOrWhiteSpace(query.Dept))
                queryable = queryable.Where(g => g.Dept == query.Dept);

            queryable = queryable.ApplySorting(query.SortBy ?? "createdAt", query.SortDir ?? "desc");

            var totalCount = await queryable.CountAsync();
            var items = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(g => new RadioGrafirDto
                {
                    Id = g.Id,
                    NoAsset = g.NoAsset,
                    SerialNumber = g.SerialNumber,
                    TypeRadio = g.TypeRadio,
                    Div = g.Div,
                    Dept = g.Dept,
                    FleetId = g.FleetId,
                    Tanggal = g.Tanggal,
                    Status = g.Status,
                    CreatedAt = g.CreatedAt,
                    UpdatedAt = g.UpdatedAt,
                    TrunkingCount = g.TrunkingRadios.Count,
                    ConventionalCount = g.ConventionalRadios.Count
                })
                .ToListAsync();

            return new PagedResultDto<RadioGrafirDto>(items, query, totalCount);
        }

        public async Task<RadioGrafirDto?> GetByIdAsync(int id)
        {
            var entity = await _context.RadioGrafirs
                .Include(g => g.TrunkingRadios)
                .Include(g => g.ConventionalRadios)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (entity == null) return null;

            return new RadioGrafirDto
            {
                Id = entity.Id,
                NoAsset = entity.NoAsset,
                SerialNumber = entity.SerialNumber,
                TypeRadio = entity.TypeRadio,
                Div = entity.Div,
                Dept = entity.Dept,
                FleetId = entity.FleetId,
                Tanggal = entity.Tanggal,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                TrunkingCount = entity.TrunkingRadios.Count,
                ConventionalCount = entity.ConventionalRadios.Count
            };
        }

        public async Task<RadioGrafirDto> CreateAsync(CreateRadioGrafirDto dto, int userId)
        {
            var entity = new RadioGrafir
            {
                NoAsset = dto.NoAsset,
                SerialNumber = dto.SerialNumber,
                TypeRadio = dto.TypeRadio,
                Div = dto.Div,
                Dept = dto.Dept,
                FleetId = dto.FleetId,
                Tanggal = dto.Tanggal,
                Status = dto.Status,
                CreatedBy = userId
            };

            _context.RadioGrafirs.Add(entity);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio Grafir", entity.Id, "Create", userId, $"Created Radio Grafir {entity.NoAsset}");

            return new RadioGrafirDto
            {
                Id = entity.Id,
                NoAsset = entity.NoAsset,
                SerialNumber = entity.SerialNumber,
                TypeRadio = entity.TypeRadio,
                Div = entity.Div,
                Dept = entity.Dept,
                FleetId = entity.FleetId,
                Tanggal = entity.Tanggal,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<RadioGrafirDto?> UpdateAsync(int id, UpdateRadioGrafirDto dto, int userId)
        {
            var entity = await _context.RadioGrafirs.FindAsync(id);
            if (entity == null) return null;

            if (dto.NoAsset != null) entity.NoAsset = dto.NoAsset;
            if (dto.SerialNumber != null) entity.SerialNumber = dto.SerialNumber;
            if (dto.TypeRadio != null) entity.TypeRadio = dto.TypeRadio;
            if (dto.Div != null) entity.Div = dto.Div;
            if (dto.Dept != null) entity.Dept = dto.Dept;
            if (dto.FleetId != null) entity.FleetId = dto.FleetId;
            if (dto.Tanggal.HasValue) entity.Tanggal = dto.Tanggal;
            if (dto.Status != null) entity.Status = dto.Status;

            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio Grafir", id, "Update", userId, $"Updated Radio Grafir {entity.NoAsset}");

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.RadioGrafirs.FindAsync(id);
            if (entity == null) return false;
            _context.RadioGrafirs.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RadioTrunkingDto>> GetLinkedTrunkingAsync(int grafirId)
        {
            return await _context.RadioTrunkings
                .Where(r => r.GrafirId == grafirId)
                .Select(r => new RadioTrunkingDto
                {
                    Id = r.Id,
                    UnitNumber = r.UnitNumber,
                    RadioId = r.RadioId,
                    SerialNumber = r.SerialNumber,
                    Status = r.Status
                })
                .ToListAsync();
        }

        public async Task<List<RadioConventionalDto>> GetLinkedConventionalAsync(int grafirId)
        {
            return await _context.RadioConventionals
                .Where(r => r.GrafirId == grafirId)
                .Select(r => new RadioConventionalDto
                {
                    Id = r.Id,
                    UnitNumber = r.UnitNumber,
                    RadioId = r.RadioId,
                    SerialNumber = r.SerialNumber,
                    Status = r.Status
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

                int headerRow = 0;
                var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int r = 1; r <= Math.Min(10, rowCount); r++)
                {
                    var rowMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    for (int c = 1; c <= colCount; c++)
                    {
                        var cellText = worksheet.Cells[r, c].Text?.Replace(" ", "").Replace("_", "").Replace(",", "").Replace(".", "").ToLower();
                        if (string.IsNullOrEmpty(cellText)) continue;

                        if (cellText == "noasset" || cellText == "asset") rowMap.TryAdd("noasset", c);
                        else if (cellText == "serialnumber" || cellText == "serial" || cellText == "sn") rowMap.TryAdd("serialnumber", c);
                        else if (cellText == "typeradio" || cellText == "type") rowMap.TryAdd("typeradio", c);
                        else if (cellText == "div" || cellText == "division") rowMap.TryAdd("div", c);
                        else if (cellText == "dept" || cellText == "department") rowMap.TryAdd("dept", c);
                        else if (cellText == "fleetid" || cellText == "fleet") rowMap.TryAdd("fleetid", c);
                        else if (cellText == "tanggal" || cellText == "date") rowMap.TryAdd("tanggal", c);
                        else if (cellText == "status") rowMap.TryAdd("status", c);
                    }

                    if ((rowMap.ContainsKey("noasset") || rowMap.ContainsKey("serialnumber")) && rowMap.Count >= 3)
                    {
                        columnMap = rowMap;
                        headerRow = r;
                        break;
                    }
                }

                if (headerRow == 0) continue;

                string? GetValue(int r, string key)
                {
                    if (columnMap.TryGetValue(key, out int colIndex))
                    {
                        var val = worksheet.Cells[r, colIndex].Text?.Trim();
                        return string.IsNullOrWhiteSpace(val) ? null : val;
                    }
                    return null;
                }

                DateTime? GetDate(int r, string key)
                {
                    var val = GetValue(r, key);
                    if (string.IsNullOrEmpty(val)) return null;

                    if (double.TryParse(val, out double dateNum) && dateNum > 10000)
                    {
                        try { return DateTime.FromOADate(dateNum); } catch { }
                    }

                    val = val.Replace("Januari", "January", StringComparison.OrdinalIgnoreCase)
                             .Replace("Februari", "February", StringComparison.OrdinalIgnoreCase)
                             .Replace("Maret", "March", StringComparison.OrdinalIgnoreCase)
                             .Replace("Mei", "May", StringComparison.OrdinalIgnoreCase)
                             .Replace("Juni", "June", StringComparison.OrdinalIgnoreCase)
                             .Replace("Juli", "July", StringComparison.OrdinalIgnoreCase)
                             .Replace("Agustus", "August", StringComparison.OrdinalIgnoreCase)
                             .Replace("Oktober", "October", StringComparison.OrdinalIgnoreCase)
                             .Replace("Nopember", "November", StringComparison.OrdinalIgnoreCase)
                             .Replace("Desember", "December", StringComparison.OrdinalIgnoreCase);

                    if (DateTime.TryParse(val, out var parsed)) return parsed;
                    return null;
                }

                // Pre-load all existing RadioGrafirs into memory for fast lookup
                var allExisting = await _context.RadioGrafirs.ToListAsync();
                var existingByNoAsset = allExisting
                    .Where(rt => !string.IsNullOrEmpty(rt.NoAsset))
                    .GroupBy(rt => rt.NoAsset!)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                for (int r = headerRow + 1; r <= rowCount; r++)
                {
                    var noAsset = GetValue(r, "noasset");
                    var serialNumber = GetValue(r, "serialnumber");

                    if (string.IsNullOrEmpty(noAsset) && string.IsNullOrEmpty(serialNumber)) continue;
                    if (string.IsNullOrEmpty(noAsset) && !string.IsNullOrEmpty(serialNumber)) noAsset = serialNumber;

                    try
                    {
                        if (existingByNoAsset.TryGetValue(noAsset!, out var existing))
                        {
                            if (serialNumber != null) existing.SerialNumber = serialNumber;
                            if (GetValue(r, "typeradio") is string tr) existing.TypeRadio = tr;
                            if (GetValue(r, "div") is string div) existing.Div = div;
                            if (GetValue(r, "dept") is string dept) existing.Dept = dept;
                            if (GetValue(r, "fleetid") is string fid) existing.FleetId = fid;
                            if (GetDate(r, "tanggal") is DateTime tgl) existing.Tanggal = tgl;
                            existing.Status = GetValue(r, "status") ?? existing.Status ?? "Active";
                            existing.UpdatedAt = DateTime.UtcNow;
                            existing.UpdatedBy = userId;
                        }
                        else
                        {
                            var entity = new RadioGrafir
                            {
                                NoAsset = noAsset!,
                                SerialNumber = serialNumber ?? noAsset!,
                                TypeRadio = GetValue(r, "typeradio"),
                                Div = GetValue(r, "div"),
                                Dept = GetValue(r, "dept"),
                                FleetId = GetValue(r, "fleetid"),
                                Tanggal = GetDate(r, "tanggal"),
                                Status = GetValue(r, "status") ?? "Active",
                                CreatedBy = userId,
                                UpdatedBy = userId
                            };
                            _context.RadioGrafirs.Add(entity);
                            existingByNoAsset[noAsset!] = entity;
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
            await _activityLog.LogAsync("Radio Grafir", null, "Import", userId, $"Imported {success} data (Failed: {failed})");
            return (success, failed, errors);
        }

        public async Task<byte[]> ExportCsvAsync(RadioGrafirQueryDto? query)
        {
            var data = await _context.RadioGrafirs.AsNoTracking().ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("NoAsset,SerialNumber,TypeRadio,Div,Dept,FleetId,Tanggal,Status");
            foreach (var item in data)
                sb.AppendLine($"{item.NoAsset},{item.SerialNumber},{item.TypeRadio},{item.Div},{item.Dept},{item.FleetId},{item.Tanggal:yyyy-MM-dd},{item.Status}");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public byte[] GetImportTemplate()
        {
            return Encoding.UTF8.GetBytes("NoAsset,SerialNumber,TypeRadio,Div,Dept,FleetId\nRTH0441S,25573915,TP8100,MSD,MEWS,4101-346\n");
        }

        public async Task<int> ClearAllAsync(int userId)
        {
            await _activityLog.LogAsync("Radio Grafir", 0, "Delete", userId, "Cleared ALL Radio Grafir data");
            return await _context.RadioGrafirs.ExecuteDeleteAsync();
        }
    }
}
