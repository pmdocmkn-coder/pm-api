using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;

namespace Pm.Services
{
    public class DivisionService : IDivisionService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<DivisionService> _logger;

        public DivisionService(AppDbContext context, IActivityLogService activityLog, ILogger<DivisionService> logger)
        {
            _context = context;
            _activityLog = activityLog;
            _logger = logger;
        }

        public async Task<DivisionResponseDto> CreateAsync(DivisionCreateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("Creating division: {Code}", dto.Code);

                var codeUpper = dto.Code.ToUpper().Trim();
                var exists = await _context.Divisions
                    .AnyAsync(d => d.Code.ToUpper() == codeUpper);

                if (exists)
                {
                    throw new ArgumentException($"Division with code '{dto.Code}' already exists.");
                }

                var division = new Division
                {
                    Code = codeUpper,
                    Name = dto.Name.Trim(),
                    IsActive = true,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Divisions.Add(division);
                await _context.SaveChangesAsync();

                try
                {
                    await _activityLog.LogAsync(
                        module: "Division",
                        entityId: division.Id,
                        action: "Create",
                        userId: userId,
                        description: $"Membuat divisi: {division.Code} - {division.Name}"
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                }

                _logger.LogInformation("✅ Division created: ID={Id}", division.Id);

                var createdByUser = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new UserInfoDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        Email = u.Email,
                        PhotoUrl = u.PhotoUrl,
                        EmployeeId = u.EmployeeId,
                        Division = u.Division
                    })
                    .FirstOrDefaultAsync();

                return new DivisionResponseDto
                {
                    Id = division.Id,
                    Code = division.Code,
                    Name = division.Name,
                    IsActive = division.IsActive,
                    CreatedAt = division.CreatedAt,
                    UpdatedAt = division.UpdatedAt,
                    CreatedByUser = createdByUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating division");
                throw;
            }
        }

        public async Task<DivisionResponseDto> UpdateAsync(int id, DivisionUpdateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("Updating division: ID={Id}", id);

                var division = await _context.Divisions
                    .Include(d => d.CreatedByUser)
                    .Include(d => d.UpdatedByUser)
                    .FirstOrDefaultAsync(d => d.Id == id)
                    ?? throw new KeyNotFoundException($"Division with ID {id} not found.");

                division.Name = dto.Name.Trim();
                division.IsActive = dto.IsActive;
                division.UpdatedAt = DateTime.UtcNow;
                division.UpdatedBy = userId;

                await _context.SaveChangesAsync();

                try
                {
                    await _activityLog.LogAsync(
                        module: "Division",
                        entityId: division.Id,
                        action: "Update",
                        userId: userId,
                        description: $"Mengubah divisi: {division.Code} - {division.Name}"
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                }

                _logger.LogInformation("✅ Division updated: ID={Id}", division.Id);

                return new DivisionResponseDto
                {
                    Id = division.Id,
                    Code = division.Code,
                    Name = division.Name,
                    IsActive = division.IsActive,
                    CreatedAt = division.CreatedAt,
                    UpdatedAt = division.UpdatedAt,
                    CreatedByUser = division.CreatedByUser != null ? new UserInfoDto
                    {
                        UserId = division.CreatedByUser.UserId,
                        Username = division.CreatedByUser.Username,
                        FullName = division.CreatedByUser.FullName,
                        Email = division.CreatedByUser.Email,
                        PhotoUrl = division.CreatedByUser.PhotoUrl,
                        EmployeeId = division.CreatedByUser.EmployeeId,
                        Division = division.CreatedByUser.Division
                    } : null,
                    UpdatedByUser = division.UpdatedByUser != null ? new UserInfoDto
                    {
                        UserId = division.UpdatedByUser.UserId,
                        Username = division.UpdatedByUser.Username,
                        FullName = division.UpdatedByUser.FullName,
                        Email = division.UpdatedByUser.Email,
                        PhotoUrl = division.UpdatedByUser.PhotoUrl,
                        EmployeeId = division.UpdatedByUser.EmployeeId,
                        Division = division.UpdatedByUser.Division
                    } : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating division");
                throw;
            }
        }

        public async Task DeleteAsync(int id, int userId)
        {
            try
            {
                _logger.LogInformation("Deleting division: ID={Id}", id);

                var division = await _context.Divisions.FindAsync(id)
                    ?? throw new KeyNotFoundException($"Division with ID {id} not found.");

                var code = division.Code;
                var name = division.Name;

                _context.Divisions.Remove(division);
                await _context.SaveChangesAsync();

                try
                {
                    await _activityLog.LogAsync(
                        module: "Division",
                        entityId: id,
                        action: "Delete",
                        userId: userId,
                        description: $"Menghapus divisi: {code} - {name}"
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                }

                _logger.LogInformation("✅ Division deleted: ID={Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting division");
                throw;
            }
        }

        public async Task<DivisionResponseDto?> GetByIdAsync(int id)
        {
            var division = await _context.Divisions
                .AsNoTracking()
                .Include(d => d.CreatedByUser)
                .Include(d => d.UpdatedByUser)
                .Where(d => d.Id == id)
                .Select(d => new DivisionResponseDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt,
                    CreatedByUser = d.CreatedByUser != null ? new UserInfoDto
                    {
                        UserId = d.CreatedByUser.UserId,
                        Username = d.CreatedByUser.Username,
                        FullName = d.CreatedByUser.FullName,
                        Email = d.CreatedByUser.Email,
                        PhotoUrl = d.CreatedByUser.PhotoUrl,
                        EmployeeId = d.CreatedByUser.EmployeeId,
                        Division = d.CreatedByUser.Division
                    } : null,
                    UpdatedByUser = d.UpdatedByUser != null ? new UserInfoDto
                    {
                        UserId = d.UpdatedByUser.UserId,
                        Username = d.UpdatedByUser.Username,
                        FullName = d.UpdatedByUser.FullName,
                        Email = d.UpdatedByUser.Email,
                        PhotoUrl = d.UpdatedByUser.PhotoUrl,
                        EmployeeId = d.UpdatedByUser.EmployeeId,
                        Division = d.UpdatedByUser.Division
                    } : null
                })
                .FirstOrDefaultAsync();

            return division;
        }

        public async Task<PagedResultDto<DivisionListDto>> GetAllAsync(DivisionQueryDto query)
        {
            var baseQuery = _context.Divisions.AsNoTracking();

            if (query.IsActive.HasValue)
            {
                baseQuery = baseQuery.Where(d => d.IsActive == query.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                baseQuery = baseQuery.Where(d =>
                    d.Code.Contains(query.Search) ||
                    d.Name.Contains(query.Search));
            }

            var totalCount = await baseQuery.CountAsync();

            var divisions = await baseQuery
                .OrderBy(d => d.Code)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(d => new DivisionListDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    IsActive = d.IsActive
                })
                .ToListAsync();

            return new PagedResultDto<DivisionListDto>(divisions, query.Page, query.PageSize, totalCount);
        }
    }
}
