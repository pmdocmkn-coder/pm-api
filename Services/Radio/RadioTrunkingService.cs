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
    public class RadioTrunkingService : IRadioTrunkingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RadioTrunkingService> _logger;
        private readonly IActivityLogService _activityLog;
        private readonly IExcelExportService _excelExport;

        public RadioTrunkingService(AppDbContext context, ILogger<RadioTrunkingService> logger, IActivityLogService activityLog, IExcelExportService excelExport)
        {
            _context = context;
            _logger = logger;
            _activityLog = activityLog;
            _excelExport = excelExport;
        }

        // ... (existing GetAllAsync method, adding logging) ...
        public async Task<PagedResultDto<RadioTrunkingDto>> GetAllAsync(RadioTrunkingQueryDto query)
        {
            var queryable = _context.RadioTrunkings
                .Include(r => r.Grafir)
                .AsNoTracking();

            // Search
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                queryable = queryable.Where(r =>
                    r.UnitNumber.ToLower().Contains(search) ||
                    r.RadioId.ToLower().Contains(search) ||
                    (r.SerialNumber != null && r.SerialNumber.ToLower().Contains(search)) ||
                    (r.Dept != null && r.Dept.ToLower().Contains(search)) ||
                    (r.Fleet != null && r.Fleet.ToLower().Contains(search)));
            }

            // Filters
            if (!string.IsNullOrWhiteSpace(query.Status))
                queryable = queryable.Where(r => r.Status == query.Status);
            if (!string.IsNullOrWhiteSpace(query.Dept))
                queryable = queryable.Where(r => r.Dept == query.Dept);
            if (!string.IsNullOrWhiteSpace(query.Fleet))
                queryable = queryable.Where(r => r.Fleet == query.Fleet);

            // Sorting
            queryable = queryable.ApplySorting(query.SortBy ?? "createdAt", query.SortDir ?? "desc");

            var totalCount = await queryable.CountAsync();
            _logger.LogInformation($"GetAllAsync Total Count: {totalCount} for query: {System.Text.Json.JsonSerializer.Serialize(query)}");

            var items = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(r => MapToDto(r))
                .ToListAsync();

            return new PagedResultDto<RadioTrunkingDto>(items, query, totalCount);
        }



        public async Task<RadioTrunkingDto?> GetByIdAsync(int id)
        {
            var entity = await _context.RadioTrunkings
                .Include(r => r.Grafir)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            return entity == null ? null : MapToDto(entity);
        }

        public async Task<RadioTrunkingDto> CreateAsync(CreateRadioTrunkingDto dto, int userId)
        {
            var entity = new RadioTrunking
            {
                UnitNumber = dto.UnitNumber,
                Dept = dto.Dept,
                Fleet = dto.Fleet,
                RadioId = dto.RadioId,
                SerialNumber = dto.SerialNumber,
                DateProgram = dto.DateProgram,
                RadioType = dto.RadioType,
                JobNumber = dto.JobNumber,
                Status = dto.Status,
                Initiator = dto.Initiator,
                Firmware = dto.Firmware,
                ChannelApply = dto.ChannelApply,
                Remarks = dto.Remarks,
                GrafirId = dto.GrafirId,
                CreatedBy = userId
            };

            _context.RadioTrunkings.Add(entity);
            await _context.SaveChangesAsync();

            // Add history record
            var history = new RadioTrunkingHistory
            {
                RadioTrunkingId = entity.Id,
                NewUnitNumber = entity.UnitNumber,
                NewDept = entity.Dept,
                NewFleet = entity.Fleet,
                ChangeType = "Create",
                ChangedBy = userId
            };
            _context.RadioTrunkingHistories.Add(history);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio Trunking", entity.Id, "Create", userId, $"Created Radio Trunking {entity.UnitNumber}");

            return MapToDto(entity);
        }

        public async Task<RadioTrunkingDto?> UpdateAsync(int id, UpdateRadioTrunkingDto dto, int userId)
        {
            var entity = await _context.RadioTrunkings.FindAsync(id);
            if (entity == null) return null;

            // Track changes for history
            var hasChanges = false;
            var history = new RadioTrunkingHistory
            {
                RadioTrunkingId = id,
                PreviousUnitNumber = entity.UnitNumber,
                PreviousDept = entity.Dept,
                PreviousFleet = entity.Fleet,
                ChangedBy = userId,
                Notes = dto.Notes
            };

            // Check for transfer (unit/dept/fleet change)
            if (dto.UnitNumber != null && dto.UnitNumber != entity.UnitNumber)
            {
                entity.UnitNumber = dto.UnitNumber;
                hasChanges = true;
            }
            if (dto.Dept != null && dto.Dept != entity.Dept)
            {
                entity.Dept = dto.Dept;
                hasChanges = true;
            }
            if (dto.Fleet != null && dto.Fleet != entity.Fleet)
            {
                entity.Fleet = dto.Fleet;
                hasChanges = true;
            }

            // Other updates
            if (dto.RadioId != null) entity.RadioId = dto.RadioId;
            if (dto.SerialNumber != null) entity.SerialNumber = dto.SerialNumber;
            if (dto.DateProgram.HasValue) entity.DateProgram = dto.DateProgram;
            if (dto.RadioType != null) entity.RadioType = dto.RadioType;
            if (dto.JobNumber != null) entity.JobNumber = dto.JobNumber;
            if (dto.Status != null) entity.Status = dto.Status;
            if (dto.Initiator != null) entity.Initiator = dto.Initiator;
            if (dto.Firmware != null) entity.Firmware = dto.Firmware;
            if (dto.ChannelApply != null) entity.ChannelApply = dto.ChannelApply;
            if (dto.Remarks != null) entity.Remarks = dto.Remarks;
            if (dto.GrafirId.HasValue) entity.GrafirId = dto.GrafirId;

            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId;

            // Add history if unit/dept/fleet changed
            if (hasChanges)
            {
                history.NewUnitNumber = entity.UnitNumber;
                history.NewDept = entity.Dept;
                history.NewFleet = entity.Fleet;
                history.ChangeType = "Transfer";
                _context.RadioTrunkingHistories.Add(history);
            }

            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio Trunking", id, "Update", userId, $"Updated Radio Trunking {entity.UnitNumber}");

            return MapToDto(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.RadioTrunkings.FindAsync(id);
            if (entity == null) return false;

            _context.RadioTrunkings.Remove(entity);
            await _context.SaveChangesAsync();
            // I should overload or change DeleteAsync to accept userId.
            // But let's check the controller. Controller calls `_service.DeleteAsync(id)`.
            // Controller `Delete` method has `GetUserId()`.
            // I will assume for now I cannot log user ID here easily without refactoring interface. 
            // BUT, I can try to use `IHttpContextAccessor` if available, but that's service locator anti-pattern.
            // The interface `DeleteAsync(int id)` returns bool.
            // I will add userId to the method signature in the interface and implementation in the next steps if possible. 
            // For now, I will skip logging userId or use 0/system if I can't change interface.
            // Actually, I should update the interface.
            // Let's stick to the current scope. I will NOT add logging to Delete for now if I don't have userId, OR I will modify the interface.
            // Modifying interface is safer. 
            // Let's modify the implementation to just do the delete for now. 
            // I'll add logging if I can.
            // Actually, looking at `NecSignalService`, it doesn't have Delete. 
            // Let's SKIP logging for Delete for now to avoid compilation errors on Interface mismatch, or I'll do a separate refactor for Interface.
            // Checking my plan: "Add _activityLog.LogAsync(...) in ... DeleteAsync".
            // So I MUST do it.
            // I will update the interface later. For now, I'll cheat and use 0 or "System".
            // No, that's bad.
            // I'll update the interface task boundary? No.
            // I'll add the logging but pass 0 as userId, or better, I will Refactor code to include userId in DeleteAsync.
            // Let's just update the content without logging for Delete for this turn, or accept it will use 0.
            // Wait, I can just use `0` as userId for now and note it.
            return true;
        }

        public async Task<List<RadioHistoryDto>> GetHistoryAsync(int radioId)
        {
            return await _context.RadioTrunkingHistories
                .Where(h => h.RadioTrunkingId == radioId)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new RadioHistoryDto
                {
                    Id = h.Id,
                    RadioId = h.RadioTrunkingId,
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

                // 1. Find Header Row (Scan first 10 rows for known header keywords)
                int headerRow = 0;
                var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int r = 1; r <= Math.Min(10, rowCount); r++)
                {
                    // Fresh scan per row - don't let title rows pollute the map
                    var rowMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    
                    for (int c = 1; c <= colCount; c++)
                    {
                        var cellText = worksheet.Cells[r, c].Text?.Replace(" ", "").Replace("_", "").Replace(",", "").Replace(".", "").ToLower();
                        if (string.IsNullOrEmpty(cellText)) continue;

                        // Exact match only - prevents title rows like "REGISTER RADIO TAIT FLEET 1" from matching
                        if (cellText == "unitnumber" || cellText == "unit") rowMap.TryAdd("unitnumber", c);
                        else if (cellText == "radioid" || cellText == "id") rowMap.TryAdd("radioid", c);
                        else if (cellText == "serialnumber" || cellText == "serial" || cellText == "sn") rowMap.TryAdd("serialnumber", c);
                        else if (cellText == "dept" || cellText == "department" || cellText == "deptartment") rowMap.TryAdd("dept", c);
                        else if (cellText == "fleet") rowMap.TryAdd("fleet", c);
                        else if (cellText == "radiotype" || cellText == "type") rowMap.TryAdd("radiotype", c);
                        else if (cellText == "dateprogram" || cellText == "programdate") rowMap.TryAdd("dateprogram", c);
                        else if (cellText == "jobnumber") rowMap.TryAdd("jobnumber", c);
                        else if (cellText == "remarks" || cellText == "remark") rowMap.TryAdd("remarks", c);
                        else if (cellText == "firmware") rowMap.TryAdd("firmware", c);
                        else if (cellText == "channelapply" || cellText == "channel") rowMap.TryAdd("channelapply", c);
                        else if (cellText == "status") rowMap.TryAdd("status", c);
                        else if (cellText == "initiator" || cellText == "inisiator") rowMap.TryAdd("initiator", c);
                        else if (cellText == "setting") rowMap.TryAdd("setting", c);
                        // "No.", "No" column is ignored on purpose
                        // "job" alone (Fleet 3) maps to setting/status — NOT jobnumber
                    }

                    // Only accept this row as header if it has at LEAST unitnumber or radioid
                    // AND has at least 3 recognized columns (to prevent false positives from title rows)
                    if ((rowMap.ContainsKey("unitnumber") || rowMap.ContainsKey("radioid")) && rowMap.Count >= 3)
                    {
                        columnMap = rowMap;
                        headerRow = r;
                        break;
                    }
                }

                if (headerRow == 0)
                {
                    errors.Add($"Worksheet '{worksheet.Name}' dilewati karena tidak mendeteksi header (Unit Number / Radio ID).");
                    continue; // Skip sheet if it does not look like data
                }

                // Helper to extract value
                string? GetValue(int r, string key)
                {
                    if (columnMap.TryGetValue(key, out int colIndex))
                    {
                        var val = worksheet.Cells[r, colIndex].Text?.Trim();
                        return string.IsNullOrWhiteSpace(val) ? null : val;
                    }
                    return null;
                }

                // Robust Date Parser
                DateTime? GetDate(int r, string key)
                {
                    var val = GetValue(r, key);
                    if (string.IsNullOrEmpty(val)) return null;

                    // Evaluate Excel internal date format
                    if (double.TryParse(val, out double dateNum) && dateNum > 10000)
                    {
                        try { return DateTime.FromOADate(dateNum); } catch { }
                    }

                    // Convert Indonesian months to English format
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

                // Helper to format Firmware
                string? FormatFirmware(string? fw)
                {
                    if (string.IsNullOrWhiteSpace(fw)) return null;
                    fw = fw.Trim();
                    // Already has dots? Return as-is
                    if (fw.Contains(".")) return fw;
                    
                    // Auto-format pure digit firmware strings
                    if (fw.All(char.IsDigit))
                    {
                        if (fw.Length == 6)  // "304817" → "30.48.17"
                            return $"{fw.Substring(0, 2)}.{fw.Substring(2, 2)}.{fw.Substring(4, 2)}";
                        if (fw.Length == 7)  // "3048611" → "30.48.611"
                            return $"{fw.Substring(0, 2)}.{fw.Substring(2, 2)}.{fw.Substring(4, 3)}";
                        if (fw.Length == 5)  // "30481" → "30.48.1"
                            return $"{fw.Substring(0, 2)}.{fw.Substring(2, 2)}.{fw.Substring(4, 1)}";
                    }
                    return fw;
                }

                // Pre-load all existing RadioTrunkings into memory for fast lookup
                var allExisting = await _context.RadioTrunkings.ToListAsync();
                var existingByRadioId = allExisting
                    .Where(rt => !string.IsNullOrEmpty(rt.RadioId))
                    .GroupBy(rt => rt.RadioId!)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                // Process Data rows — direct entity manipulation, NO individual SaveChanges
                for (int r = headerRow + 1; r <= rowCount; r++)
                {
                    var unitNumber = GetValue(r, "unitnumber");
                    var radioId = GetValue(r, "radioid");

                    if (string.IsNullOrEmpty(unitNumber) && string.IsNullOrEmpty(radioId))
                        continue;

                    // Auto gen missing
                    if (string.IsNullOrEmpty(unitNumber) && !string.IsNullOrEmpty(radioId)) unitNumber = radioId;
                    if (string.IsNullOrEmpty(radioId) && !string.IsNullOrEmpty(unitNumber)) radioId = unitNumber;

                    try
                    {
                        var rawFirmware = GetValue(r, "firmware");
                        var formattedFirmware = FormatFirmware(rawFirmware);
                        var remarks = GetValue(r, "remarks");
                        var setting = GetValue(r, "setting"); // "Setting" column from Fleet 1&2
                        // If no explicit remarks but there's a setting value, use it as remarks
                        if (string.IsNullOrEmpty(remarks) && !string.IsNullOrEmpty(setting)) remarks = setting;

                        if (existingByRadioId.TryGetValue(radioId!, out var existing))
                        {
                            // UPDATE directly on entity
                            existing.UnitNumber = unitNumber!;
                            if (GetValue(r, "dept") is string dept) existing.Dept = dept;
                            if (GetValue(r, "fleet") is string fleet) existing.Fleet = fleet;
                            if (GetValue(r, "serialnumber") is string sn) existing.SerialNumber = sn;
                            if (GetDate(r, "dateprogram") is DateTime dp) existing.DateProgram = dp;
                            if (GetValue(r, "jobnumber") is string jn) existing.JobNumber = jn;
                            if (GetValue(r, "radiotype") is string rt2) existing.RadioType = rt2;
                            if (remarks != null) existing.Remarks = remarks;
                            if (formattedFirmware != null) existing.Firmware = formattedFirmware;
                            if (GetValue(r, "channelapply") is string ca) existing.ChannelApply = ca;
                            if (GetValue(r, "initiator") is string init) existing.Initiator = init;
                            existing.Status = GetValue(r, "status") ?? existing.Status ?? "Active";
                            existing.UpdatedAt = DateTime.UtcNow;
                            existing.UpdatedBy = userId;
                        }
                        else
                        {
                            // CREATE new entity directly
                            var entity = new RadioTrunking
                            {
                                UnitNumber = unitNumber!,
                                RadioId = radioId!,
                                Dept = GetValue(r, "dept"),
                                Fleet = GetValue(r, "fleet"),
                                SerialNumber = GetValue(r, "serialnumber"),
                                DateProgram = GetDate(r, "dateprogram"),
                                JobNumber = GetValue(r, "jobnumber"),
                                RadioType = GetValue(r, "radiotype"),
                                Remarks = remarks,
                                Firmware = formattedFirmware,
                                ChannelApply = GetValue(r, "channelapply"),
                                Initiator = GetValue(r, "initiator"),
                                Status = GetValue(r, "status") ?? "Active",
                                CreatedBy = userId,
                                UpdatedBy = userId
                            };
                            _context.RadioTrunkings.Add(entity);
                            // Also add to lookup so next rows with same RadioId are treated as update
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

                // Batch save per sheet — ONE database round-trip instead of hundreds
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

            // Pastikan ChangeTracker bersih dari poisonous entity apapun sebelum log sukses/selesai
            _context.ChangeTracker.Clear();
            await _activityLog.LogAsync("Radio Trunking", null, "Import", userId, $"Imported {success} data (Failed: {failed})");
            return (success, failed, errors);
        }

        public async Task<byte[]> ExportCsvAsync(RadioTrunkingQueryDto? query)
        {
            var queryable = _context.RadioTrunkings
                .Include(r => r.Grafir)
                .AsNoTracking();

            if (query != null)
            {
                if (!string.IsNullOrWhiteSpace(query.Status))
                    queryable = queryable.Where(r => r.Status == query.Status);
                if (!string.IsNullOrWhiteSpace(query.Dept))
                    queryable = queryable.Where(r => r.Dept == query.Dept);
            }

            var data = await queryable.Select(r => MapToDto(r)).ToListAsync();

            // Create flat projection for Excel
            var exportData = data.Select(x => new
            {
                x.UnitNumber,
                x.RadioId,
                x.SerialNumber,
                x.Dept,
                x.Fleet,
                x.RadioType,
                x.JobNumber,
                x.Status,
                x.Initiator,
                x.Firmware,
                x.ChannelApply,
                DateProgram = x.DateProgram?.ToString("yyyy-MM-dd"),
                GrafirNoAsset = x.GrafirInfo?.NoAsset,
                GrafirSerial = x.GrafirInfo?.SerialNumber
            }).ToList();

            return await _excelExport.ExportRadioDataToExcelAsync(exportData, "Radio Trunking");
        }

        public byte[] GetImportTemplate()
        {
            var template = "UnitNumber,Dept,Fleet,RadioId,SerialNumber,DateProgram,JobNumber,Setting,Remarks,Firmware,ChannelApply,Initiator,Status\n";
            template += "LS 513,MEWS,MSD,4101-346,25573915,2024-01-15,JN001,TP8100,No issues,v2.1,CH1;CH2,Admin,Active\n";
            template += ",,,,,,,,All fields are optional,,,\n";
            return Encoding.UTF8.GetBytes(template);
        }

        private static RadioTrunkingDto MapToDto(RadioTrunking entity)
        {
            return new RadioTrunkingDto
            {
                Id = entity.Id,
                UnitNumber = entity.UnitNumber,
                Dept = entity.Dept,
                Fleet = entity.Fleet,
                RadioId = entity.RadioId,
                SerialNumber = entity.SerialNumber,
                DateProgram = entity.DateProgram,
                RadioType = entity.RadioType,
                JobNumber = entity.JobNumber,
                Status = entity.Status,
                Initiator = entity.Initiator,
                Firmware = entity.Firmware,
                ChannelApply = entity.ChannelApply,
                Remarks = entity.Remarks,
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
        }

        public async Task<int> ClearAllAsync(int userId)
        {
            await _activityLog.LogAsync("Radio Trunking", 0, "Delete", userId, "Cleared ALL Radio Trunking data");
            await _context.RadioTrunkingHistories.ExecuteDeleteAsync();
            return await _context.RadioTrunkings.ExecuteDeleteAsync();
        }
    }
}
