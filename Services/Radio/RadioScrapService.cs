using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.Radio;
using Pm.Helper;
using Pm.Models;
using System.Text;

namespace Pm.Services
{
    public class RadioScrapService : IRadioScrapService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RadioScrapService> _logger;
        private readonly IActivityLogService _activityLog;

        public RadioScrapService(AppDbContext context, ILogger<RadioScrapService> logger, IActivityLogService activityLog)
        {
            _context = context;
            _logger = logger;
            _activityLog = activityLog;
        }

        public async Task<PagedResultDto<RadioScrapDto>> GetAllAsync(RadioScrapQueryDto query)
        {
            var queryable = _context.RadioScraps
                .Include(s => s.SourceTrunking)
                .Include(s => s.SourceConventional)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                queryable = queryable.Where(s =>
                    (s.SerialNumber != null && s.SerialNumber.ToLower().Contains(search)) ||
                    (s.TypeRadio != null && s.TypeRadio.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(query.ScrapCategory))
                queryable = queryable.Where(s => s.ScrapCategory == query.ScrapCategory);
            if (query.Year.HasValue)
                queryable = queryable.Where(s => s.DateScrap.Year == query.Year.Value);
            if (query.StartDate.HasValue)
                queryable = queryable.Where(s => s.DateScrap >= query.StartDate.Value);
            if (query.EndDate.HasValue)
                queryable = queryable.Where(s => s.DateScrap <= query.EndDate.Value);

            queryable = queryable.ApplySorting(query.SortBy ?? "dateScrap", query.SortDir ?? "desc");

            var totalCount = await queryable.CountAsync();
            var items = await queryable
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(s => new RadioScrapDto
                {
                    Id = s.Id,
                    ScrapCategory = s.ScrapCategory,
                    TypeRadio = s.TypeRadio,
                    SerialNumber = s.SerialNumber,
                    JobNumber = s.JobNumber,
                    DateScrap = s.DateScrap,
                    Remarks = s.Remarks,
                    SourceTrunkingId = s.SourceTrunkingId,
                    SourceConventionalId = s.SourceConventionalId,
                    SourceGrafirId = s.SourceGrafirId,
                    CreatedAt = s.CreatedAt,
                    SourceRadioId = s.SourceTrunking != null ? s.SourceTrunking.RadioId :
                                    (s.SourceConventional != null ? s.SourceConventional.RadioId : null),
                    SourceUnitNumber = s.SourceTrunking != null ? s.SourceTrunking.UnitNumber :
                                       (s.SourceConventional != null ? s.SourceConventional.UnitNumber : null)
                })
                .ToListAsync();

            return new PagedResultDto<RadioScrapDto>(items, query, totalCount);
        }

        public async Task<RadioScrapDto?> GetByIdAsync(int id)
        {
            var s = await _context.RadioScraps
                .Include(s => s.SourceTrunking)
                .Include(s => s.SourceConventional)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (s == null) return null;

            return new RadioScrapDto
            {
                Id = s.Id,
                ScrapCategory = s.ScrapCategory,
                TypeRadio = s.TypeRadio,
                SerialNumber = s.SerialNumber,
                JobNumber = s.JobNumber,
                DateScrap = s.DateScrap,
                Remarks = s.Remarks,
                SourceTrunkingId = s.SourceTrunkingId,
                SourceConventionalId = s.SourceConventionalId,
                SourceGrafirId = s.SourceGrafirId,
                CreatedAt = s.CreatedAt,
                SourceRadioId = s.SourceTrunking?.RadioId ?? s.SourceConventional?.RadioId,
                SourceUnitNumber = s.SourceTrunking?.UnitNumber ?? s.SourceConventional?.UnitNumber
            };
        }

        public async Task<RadioScrapDto> CreateAsync(CreateRadioScrapDto dto, int userId)
        {
            var entity = new RadioScrap
            {
                ScrapCategory = dto.ScrapCategory,
                TypeRadio = dto.TypeRadio,
                SerialNumber = dto.SerialNumber,
                JobNumber = dto.JobNumber,
                DateScrap = dto.DateScrap,
                Remarks = dto.Remarks,
                CreatedBy = userId
            };

            _context.RadioScraps.Add(entity);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio Scrap", entity.Id, "Create", userId, $"Created Radio Scrap {entity.SerialNumber}");

            return new RadioScrapDto
            {
                Id = entity.Id,
                ScrapCategory = entity.ScrapCategory,
                TypeRadio = entity.TypeRadio,
                SerialNumber = entity.SerialNumber,
                JobNumber = entity.JobNumber,
                DateScrap = entity.DateScrap,
                Remarks = entity.Remarks,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<RadioScrapDto?> ScrapFromTrunkingAsync(int trunkingId, ScrapFromRadioDto dto, int userId)
        {
            var trunking = await _context.RadioTrunkings.FindAsync(trunkingId);
            if (trunking == null) return null;

            // Update trunking status
            trunking.Status = "Scrapped";
            trunking.UpdatedAt = DateTime.UtcNow;

            // Add history
            _context.RadioTrunkingHistories.Add(new RadioTrunkingHistory
            {
                RadioTrunkingId = trunkingId,
                PreviousUnitNumber = trunking.UnitNumber,
                NewUnitNumber = trunking.UnitNumber,
                ChangeType = "Scrap",
                Notes = dto.Remarks,
                ChangedBy = userId
            });

            // Create scrap record
            var scrap = new RadioScrap
            {
                ScrapCategory = "Trunking",
                TypeRadio = trunking.RadioType,
                SerialNumber = trunking.SerialNumber,
                JobNumber = dto.JobNumber,
                DateScrap = dto.DateScrap,
                Remarks = dto.Remarks,
                SourceTrunkingId = trunkingId,
                CreatedBy = userId
            };

            _context.RadioScraps.Add(scrap);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio Scrap", scrap.Id, "ScrapTrunking", userId, $"Scrapped and Created Radio Scrap from Trunking {trunking.UnitNumber} (New Job: {dto.JobNumber})");

            return await GetByIdAsync(scrap.Id);
        }

        public async Task<RadioScrapDto?> ScrapFromConventionalAsync(int conventionalId, ScrapFromRadioDto dto, int userId)
        {
            var conventional = await _context.RadioConventionals.FindAsync(conventionalId);
            if (conventional == null) return null;

            conventional.Status = "Scrapped";
            conventional.UpdatedAt = DateTime.UtcNow;

            _context.RadioConventionalHistories.Add(new RadioConventionalHistory
            {
                RadioConventionalId = conventionalId,
                PreviousUnitNumber = conventional.UnitNumber,
                NewUnitNumber = conventional.UnitNumber,
                ChangeType = "Scrap",
                Notes = dto.Remarks,
                ChangedBy = userId
            });

            var scrap = new RadioScrap
            {
                ScrapCategory = "Conventional",
                TypeRadio = conventional.RadioType,
                SerialNumber = conventional.SerialNumber,
                JobNumber = dto.JobNumber,
                DateScrap = dto.DateScrap,
                Remarks = dto.Remarks,
                SourceConventionalId = conventionalId,
                CreatedBy = userId
            };

            _context.RadioScraps.Add(scrap);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync("Radio Scrap", scrap.Id, "ScrapConventional", userId, $"Scrapped and Created Radio Scrap from Conventional {conventional.UnitNumber} (New Job: {dto.JobNumber})");

            return await GetByIdAsync(scrap.Id);
        }

        public async Task<RadioScrapDto?> UpdateAsync(int id, CreateRadioScrapDto dto)
        {
            var entity = await _context.RadioScraps.FindAsync(id);
            if (entity == null) return null;

            entity.ScrapCategory = dto.ScrapCategory;
            entity.TypeRadio = dto.TypeRadio;
            entity.SerialNumber = dto.SerialNumber;
            entity.JobNumber = dto.JobNumber;
            entity.DateScrap = dto.DateScrap;
            entity.Remarks = dto.Remarks;

            await _context.SaveChangesAsync();

            // UserId is 0 because signature doesn't support it yet
            await _activityLog.LogAsync("Radio Scrap", id, "Update", 0, $"Updated Radio Scrap {entity.SerialNumber}");
            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.RadioScraps.FindAsync(id);
            if (entity == null) return false;
            _context.RadioScraps.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<YearlyScrapSummaryDto> GetYearlySummaryAsync(int year)
        {
            var scraps = await _context.RadioScraps
                .Where(s => s.DateScrap.Year == year)
                .Select(s => new { s.ScrapCategory, Month = s.DateScrap.Month })
                .ToListAsync();

            var result = new YearlyScrapSummaryDto { Year = year };

            var trunkingMonthly = new int[12];
            var conventionalMonthly = new int[12];

            foreach (var scrap in scraps)
            {
                var monthIndex = scrap.Month - 1;
                if (scrap.ScrapCategory == "Trunking")
                    trunkingMonthly[monthIndex]++;
                else
                    conventionalMonthly[monthIndex]++;
            }

            result.Trunking = new ScrapCategorySummaryDto
            {
                Total = trunkingMonthly.Sum(),
                Monthly = trunkingMonthly
            };

            result.Conventional = new ScrapCategorySummaryDto
            {
                Total = conventionalMonthly.Sum(),
                Monthly = conventionalMonthly
            };

            result.GrandTotal = result.Trunking.Total + result.Conventional.Total;

            return result;
        }

        public async Task<byte[]> ExportCsvAsync(RadioScrapQueryDto? query)
        {
            var queryable = _context.RadioScraps.AsNoTracking();

            if (query?.ScrapCategory != null)
                queryable = queryable.Where(s => s.ScrapCategory == query.ScrapCategory);
            if (query?.Year.HasValue == true)
                queryable = queryable.Where(s => s.DateScrap.Year == query.Year.Value);

            var data = await queryable.ToListAsync();
            var sb = new StringBuilder();
            sb.AppendLine("ScrapCategory,TypeRadio,SerialNumber,JobNumber,DateScrap,Remarks");
            foreach (var item in data)
                sb.AppendLine($"{item.ScrapCategory},{item.TypeRadio},{item.SerialNumber},{item.JobNumber},{item.DateScrap:yyyy-MM-dd},{item.Remarks?.Replace(",", ";")}");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
