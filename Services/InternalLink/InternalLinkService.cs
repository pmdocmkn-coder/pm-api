using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.InternalLink;
using Pm.Enums;
using Pm.Helper;
using Pm.Models.InternalLink;
using Microsoft.Extensions.Logging;

namespace Pm.Services
{
    public class InternalLinkService : IInternalLinkService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<InternalLinkService> _logger;

        public InternalLinkService(
            AppDbContext context,
            IActivityLogService activityLog,
            ILogger<InternalLinkService> logger)
        {
            _context = context;
            _activityLog = activityLog;
            _logger = logger;
        }

        // ==========================================
        // HELPER: Parse status string to enum
        // ==========================================
        private static InternalLinkStatus ParseStatus(string? statusStr)
        {
            if (string.IsNullOrWhiteSpace(statusStr))
                return InternalLinkStatus.Active;

            return Enum.TryParse<InternalLinkStatus>(statusStr, ignoreCase: true, out var result)
                ? result
                : InternalLinkStatus.Active;
        }

        private static InternalLinkServiceType ParseServiceType(string? serviceTypeStr)
        {
            if (string.IsNullOrWhiteSpace(serviceTypeStr))
                return InternalLinkServiceType.LinkInternal;

            return Enum.TryParse<InternalLinkServiceType>(serviceTypeStr, ignoreCase: true, out var result)
                ? result
                : InternalLinkServiceType.LinkInternal;
        }

        private static InternalLinkDirection ParseDirection(string? directionStr)
        {
            if (string.IsNullOrWhiteSpace(directionStr))
                return InternalLinkDirection.None;

            return Enum.TryParse<InternalLinkDirection>(directionStr, ignoreCase: true, out var result)
                ? result
                : InternalLinkDirection.None;
        }

        // ==========================================
        // CRUD LINK
        // ==========================================

        public async Task<List<InternalLinkListDto>> GetLinksAsync()
        {
            _logger.LogInformation("📊 GetLinksAsync");

            var links = await _context.InternalLinks
                .AsNoTracking()
                .Include(l => l.Histories)
                .OrderBy(l => l.LinkName)
                .ToListAsync();

            return links.Select(l => new InternalLinkListDto
            {
                Id = l.Id,
                LinkName = l.LinkName,
                LinkGroup = l.LinkGroup,
                Direction = l.Direction,
                IpAddress = l.IpAddress,
                Device = l.Device,
                Type = l.Type,
                UsedFrequency = l.UsedFrequency,
                RslNearEnd = l.RslNearEnd,
                ServiceType = l.ServiceType,
                IsActive = l.IsActive,
                HistoryCount = l.Histories.Count
            }).ToList();
        }

        public async Task<InternalLinkDetailDto?> GetLinkByIdAsync(int id)
        {
            var link = await _context.InternalLinks
                .AsNoTracking()
                .Include(l => l.Histories)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (link == null) return null;

            return new InternalLinkDetailDto
            {
                Id = link.Id,
                LinkName = link.LinkName,
                LinkGroup = link.LinkGroup,
                Direction = link.Direction,
                IpAddress = link.IpAddress,
                Device = link.Device,
                Type = link.Type,
                UsedFrequency = link.UsedFrequency,
                RslNearEnd = link.RslNearEnd,
                ServiceType = link.ServiceType,
                IsActive = link.IsActive,
                HistoryCount = link.Histories.Count,
                CreatedAt = link.CreatedAt
            };
        }

        public async Task<InternalLinkListDto> CreateLinkAsync(InternalLinkCreateDto dto, int userId)
        {
            _logger.LogInformation("➕ CreateLinkAsync: {LinkName}", dto.LinkName);

            // Check duplicate
            var exists = await _context.InternalLinks
                .AnyAsync(l => l.LinkName == dto.LinkName);
            if (exists)
                throw new ArgumentException($"Link '{dto.LinkName}' sudah ada.");

            var link = new InternalLink
            {
                LinkName = dto.LinkName,
                LinkGroup = dto.LinkGroup,
                Direction = ParseDirection(dto.Direction),
                IpAddress = dto.IpAddress,
                Device = dto.Device,
                Type = dto.Type,
                UsedFrequency = dto.UsedFrequency,
                RslNearEnd = dto.RslNearEnd,
                ServiceType = ParseServiceType(dto.ServiceType),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.InternalLinks.Add(link);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync(
                module: "Internal Link",
                entityId: link.Id,
                action: "Create",
                userId: userId,
                description: $"Menambahkan link internal: {link.LinkName} (IP: {link.IpAddress}, Service: {link.ServiceType})"
            );

            return new InternalLinkListDto
            {
                Id = link.Id,
                LinkName = link.LinkName,
                LinkGroup = link.LinkGroup,
                Direction = link.Direction,
                IpAddress = link.IpAddress,
                Device = link.Device,
                Type = link.Type,
                UsedFrequency = link.UsedFrequency,
                RslNearEnd = link.RslNearEnd,
                ServiceType = link.ServiceType,
                IsActive = link.IsActive,
                HistoryCount = 0
            };
        }

        public async Task<InternalLinkListDto> UpdateLinkAsync(InternalLinkUpdateDto dto, int userId)
        {
            _logger.LogInformation("✏️ UpdateLinkAsync: ID {Id}", dto.Id);

            var link = await _context.InternalLinks
                .Include(l => l.Histories)
                .FirstOrDefaultAsync(l => l.Id == dto.Id)
                ?? throw new KeyNotFoundException($"Link ID {dto.Id} tidak ditemukan.");

            // Check duplicate name (exclude self)
            var duplicateName = await _context.InternalLinks
                .AnyAsync(l => l.LinkName == dto.LinkName && l.Id != dto.Id);
            if (duplicateName)
                throw new ArgumentException($"Link '{dto.LinkName}' sudah ada.");

            var oldName = link.LinkName;
            link.LinkName = dto.LinkName;
            link.LinkGroup = dto.LinkGroup;
            link.Direction = ParseDirection(dto.Direction);
            link.IpAddress = dto.IpAddress;
            link.Device = dto.Device;
            link.Type = dto.Type;
            link.UsedFrequency = dto.UsedFrequency;
            link.RslNearEnd = dto.RslNearEnd;
            link.ServiceType = ParseServiceType(dto.ServiceType);
            link.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            await _activityLog.LogAsync(
                module: "Internal Link",
                entityId: link.Id,
                action: "Update",
                userId: userId,
                description: $"Mengupdate link internal: {oldName} → {link.LinkName}"
            );

            return new InternalLinkListDto
            {
                Id = link.Id,
                LinkName = link.LinkName,
                LinkGroup = link.LinkGroup,
                Direction = link.Direction,
                IpAddress = link.IpAddress,
                Device = link.Device,
                Type = link.Type,
                UsedFrequency = link.UsedFrequency,
                RslNearEnd = link.RslNearEnd,
                ServiceType = link.ServiceType,
                IsActive = link.IsActive,
                HistoryCount = link.Histories.Count
            };
        }

        public async Task DeleteLinkAsync(int id, int userId)
        {
            _logger.LogInformation("🗑️ DeleteLinkAsync: ID {Id}", id);

            var link = await _context.InternalLinks
                .Include(l => l.Histories)
                .FirstOrDefaultAsync(l => l.Id == id)
                ?? throw new KeyNotFoundException($"Link ID {id} tidak ditemukan.");

            if (link.Histories.Any())
                throw new InvalidOperationException($"Tidak bisa menghapus link '{link.LinkName}' karena masih memiliki {link.Histories.Count} history. Hapus semua history terlebih dahulu.");

            _context.InternalLinks.Remove(link);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync(
                module: "Internal Link",
                entityId: id,
                action: "Delete",
                userId: userId,
                description: $"Menghapus link internal: {link.LinkName}"
            );
        }

        // ==========================================
        // CRUD HISTORY
        // ==========================================

        public async Task<PagedResultDto<InternalLinkHistoryItemDto>> GetHistoriesAsync(InternalLinkHistoryQueryDto query)
        {
            _logger.LogInformation("📊 GetHistoriesAsync - Page: {Page}, PageSize: {PageSize}", query.Page, query.PageSize);

            var queryable = _context.InternalLinkHistories
                .AsNoTracking()
                .Include(h => h.InternalLink)
                .AsQueryable();

            // Filter by link
            if (query.InternalLinkId.HasValue)
            {
                queryable = queryable.Where(h => h.InternalLinkId == query.InternalLinkId.Value);
            }

            // Dynamic filters
            if (!string.IsNullOrWhiteSpace(query.FiltersJson))
            {
                queryable = queryable.ApplyDynamicFiltersNew<InternalLinkHistory>(query.FiltersJson);
            }

            // Search
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.ToLower();
                queryable = queryable.Where(h =>
                    (h.InternalLink != null && h.InternalLink.LinkName.ToLower().Contains(term)) ||
                    (h.Notes != null && h.Notes.ToLower().Contains(term))
                );
            }

            var totalCount = await queryable.CountAsync();

            // Sorting
            IQueryable<InternalLinkHistory> sorted = queryable.OrderByDescending(h => h.Date);

            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Date", "Date" },
                    { "Uptime", "Uptime" },
                    { "LinkName", "InternalLink.LinkName" },
                    { "Status", "Status" }
                };

                if (map.TryGetValue(query.SortBy, out var field))
                {
                    var dir = string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
                    sorted = sorted.ApplySorting(field, dir);
                }
            }

            // Pagination — select WITHOUT ScreenshotBase64 for performance
            var entities = await sorted
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(h => new
                {
                    h.Id,
                    h.InternalLinkId,
                    LinkName = h.InternalLink != null ? h.InternalLink.LinkName : "[Missing Link]",
                    h.Date,
                    h.RslNearEnd,
                    h.Uptime,
                    h.Notes,
                    HasScreenshot = h.ScreenshotBase64 != null && h.ScreenshotBase64.Length > 0,
                    h.Status,
                    h.CreatedAt
                })
                .ToListAsync();

            var items = entities.Select(h => new InternalLinkHistoryItemDto
            {
                Id = h.Id,
                InternalLinkId = h.InternalLinkId,
                LinkName = h.LinkName,
                Date = h.Date,
                RslNearEnd = h.RslNearEnd,
                Uptime = h.Uptime,
                Notes = h.Notes,
                HasScreenshot = h.HasScreenshot,
                Status = h.Status
            }).ToList();

            InternalLinkHistoryItemDto.ApplyListNumbers(items, (query.Page - 1) * query.PageSize);

            return new PagedResultDto<InternalLinkHistoryItemDto>(items, query, totalCount);
        }

        public async Task<InternalLinkHistoryDetailDto?> GetHistoryByIdAsync(int id)
        {
            // Detail includes ScreenshotBase64
            var h = await _context.InternalLinkHistories
                .AsNoTracking()
                .Include(h => h.InternalLink)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (h == null) return null;

            return new InternalLinkHistoryDetailDto
            {
                Id = h.Id,
                InternalLinkId = h.InternalLinkId,
                LinkName = h.InternalLink?.LinkName ?? "[Missing Link]",
                Date = h.Date,
                Uptime = h.Uptime,
                Notes = h.Notes,
                HasScreenshot = !string.IsNullOrEmpty(h.ScreenshotBase64),
                ScreenshotBase64 = h.ScreenshotBase64,
                Status = h.Status,
                RslNearEnd = h.RslNearEnd // Added RslNearEnd
            };
        }

        public async Task<InternalLinkHistoryItemDto> CreateHistoryAsync(InternalLinkHistoryCreateDto dto, int userId)
        {
            _logger.LogInformation("➕ CreateHistoryAsync: LinkId {LinkId}", dto.InternalLinkId);

            var status = ParseStatus(dto.Status);

            var link = await _context.InternalLinks
                .FirstOrDefaultAsync(l => l.Id == dto.InternalLinkId)
                ?? throw new KeyNotFoundException($"Link dengan ID {dto.InternalLinkId} tidak ditemukan.");

            // Validate uptime (days, non-negative)
            if (dto.Uptime.HasValue && dto.Uptime.Value < 0)
                throw new ArgumentException("Uptime (days) tidak boleh negatif.");

            // Validate notes for non-active
            if (status != InternalLinkStatus.Active && string.IsNullOrWhiteSpace(dto.Notes))
                throw new ArgumentException("Catatan wajib diisi untuk status non-Active.");

            var history = new InternalLinkHistory
            {
                InternalLinkId = dto.InternalLinkId,
                Date = dto.Date.Date,
                RslNearEnd = dto.RslNearEnd,
                Uptime = dto.Uptime,
                Notes = dto.Notes,
                ScreenshotBase64 = dto.ScreenshotBase64,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _context.InternalLinkHistories.Add(history);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync(
                module: "Internal Link - History",
                entityId: history.Id,
                action: "Create",
                userId: userId,
                description: $"Menambahkan history untuk link {link.LinkName} pada {dto.Date:dd/MM/yyyy} dengan status {status}"
            );

            return new InternalLinkHistoryItemDto
            {
                Id = history.Id,
                InternalLinkId = history.InternalLinkId,
                LinkName = link.LinkName,
                Date = history.Date,
                RslNearEnd = history.RslNearEnd,
                Uptime = history.Uptime,
                Notes = history.Notes,
                HasScreenshot = !string.IsNullOrEmpty(history.ScreenshotBase64),
                Status = history.Status
            };
        }

        public async Task<InternalLinkHistoryItemDto> UpdateHistoryAsync(int id, InternalLinkHistoryUpdateDto dto, int userId)
        {
            _logger.LogInformation("✏️ UpdateHistoryAsync: ID {Id}", id);

            var history = await _context.InternalLinkHistories
                .Include(h => h.InternalLink)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new KeyNotFoundException($"History ID {id} tidak ditemukan.");

            var status = ParseStatus(dto.Status);

            // Validate
            if (dto.Uptime.HasValue && dto.Uptime.Value < 0)
                throw new ArgumentException("Uptime (days) tidak boleh negatif.");

            if (status != InternalLinkStatus.Active && string.IsNullOrWhiteSpace(dto.Notes))
                throw new ArgumentException("Catatan wajib diisi untuk status non-Active.");

            history.RslNearEnd = dto.RslNearEnd;
            history.Uptime = dto.Uptime;
            history.Notes = dto.Notes;
            history.Status = status;

            if (dto.Date.HasValue)
            {
                history.Date = dto.Date.Value.Date;
            }

            // Handle screenshot update
            if (dto.RemoveScreenshot == true)
            {
                history.ScreenshotBase64 = null;
            }
            else if (!string.IsNullOrEmpty(dto.ScreenshotBase64))
            {
                history.ScreenshotBase64 = dto.ScreenshotBase64;
            }

            await _context.SaveChangesAsync();

            await _activityLog.LogAsync(
                module: "Internal Link - History",
                entityId: history.Id,
                action: "Update",
                userId: userId,
                description: $"Mengupdate history link {history.InternalLink?.LinkName} pada {history.Date:dd/MM/yyyy}"
            );

            return new InternalLinkHistoryItemDto
            {
                Id = history.Id,
                InternalLinkId = history.InternalLinkId,
                LinkName = history.InternalLink?.LinkName ?? "[Missing Link]",
                Date = history.Date,
                Uptime = history.Uptime,
                Notes = history.Notes,
                HasScreenshot = !string.IsNullOrEmpty(history.ScreenshotBase64),
                Status = history.Status
            };
        }

        public async Task DeleteHistoryAsync(int id, int userId)
        {
            _logger.LogInformation("🗑️ DeleteHistoryAsync: ID {Id}", id);

            var history = await _context.InternalLinkHistories
                .Include(h => h.InternalLink)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new KeyNotFoundException($"History ID {id} tidak ditemukan.");

            _context.InternalLinkHistories.Remove(history);
            await _context.SaveChangesAsync();

            await _activityLog.LogAsync(
                module: "Internal Link - History",
                entityId: id,
                action: "Delete",
                userId: userId,
                description: $"Menghapus history link {history.InternalLink?.LinkName} - {history.Date:yyyy-MM-dd}"
            );
        }
    }
}
