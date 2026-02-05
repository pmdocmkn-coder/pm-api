using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.Radio;
using Pm.Helper;
using Pm.Models;
using System.Text;

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

            using var reader = new StreamReader(stream);
            await reader.ReadLineAsync();

            var lineNumber = 1;
            while (!reader.EndOfStream)
            {
                lineNumber++;
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var values = line.Split(',');
                    await CreateAsync(new CreateRadioGrafirDto
                    {
                        NoAsset = values[0].Trim(),
                        SerialNumber = values[1].Trim(),
                        TypeRadio = values.Length > 2 ? values[2].Trim() : null,
                        Div = values.Length > 3 ? values[3].Trim() : null,
                        Dept = values.Length > 4 ? values[4].Trim() : null,
                        FleetId = values.Length > 5 ? values[5].Trim() : null
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
            await _activityLog.LogAsync("Radio Grafir", null, "Import", userId, $"Imported {success} success, {failed} failed");

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
    }
}
