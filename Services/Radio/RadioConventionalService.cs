using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.Radio;
using Pm.Helper;
using Pm.Models;
using System.Text;

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

            using var reader = new StreamReader(stream);
            await reader.ReadLineAsync(); // Skip header

            var lineNumber = 1;
            while (!reader.EndOfStream)
            {
                lineNumber++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var values = line.Split(',');
                    await CreateAsync(new CreateRadioConventionalDto
                    {
                        UnitNumber = values[0].Trim(),
                        RadioId = values[1].Trim(),
                        SerialNumber = values.Length > 2 ? values[2].Trim() : null,
                        Dept = values.Length > 3 ? values[3].Trim() : null,
                        Fleet = values.Length > 4 ? values[4].Trim() : null,
                        RadioType = values.Length > 5 ? values[5].Trim() : null,
                        Frequency = values.Length > 6 ? values[6].Trim() : null,
                        Status = values.Length > 7 ? values[7].Trim() : "Active"
                    }, userId);
                    success++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"Line {lineNumber}: {ex.Message}");
                }
            }

            // Audit Log for Import
            await _activityLog.LogAsync("Radio Conventional", null, "Import", userId, $"Imported {success} success, {failed} failed");

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
    }
}
