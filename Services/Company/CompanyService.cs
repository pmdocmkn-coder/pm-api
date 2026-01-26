using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;

namespace Pm.Services
{
    public class CompanyService(
        AppDbContext context,
        IActivityLogService activityLog,
        ILogger<CompanyService> logger) : ICompanyService
    {

        public async Task<CompanyResponseDto> CreateAsync(CompanyCreateDto dto, int userId)
        {
            try
            {
                logger.LogInformation("Creating company: {Code}", dto.Code);

                // Validate unique code
                var exists = await context.Companies
                    .AnyAsync(c => c.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    throw new ArgumentException($"Company with code '{dto.Code}' already exists.");
                }

                var company = new Company
                {
                    Code = dto.Code.ToUpper().Trim(),
                    Name = dto.Name.Trim(),
                    Address = dto.Address?.Trim(),
                    IsActive = true,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                context.Companies.Add(company);
                await context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await activityLog.LogAsync(
                        module: "Letter Numbering - Company",
                        entityId: company.Id,
                        action: "Create",
                        userId: userId,
                        description: $"Membuat perusahaan: {company.Code} - {company.Name}"
                    );
                }
                catch (Exception logEx)
                {
                    logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                }

                logger.LogInformation("✅ Company created: ID={Id}", company.Id);

                // Load creator user for response
                var createdByUser = await context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new UserInfoDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        Email = u.Email,
                        PhotoUrl = u.PhotoUrl
                    })
                    .FirstOrDefaultAsync();

                return new CompanyResponseDto
                {
                    Id = company.Id,
                    Code = company.Code,
                    Name = company.Name,
                    Address = company.Address,
                    IsActive = company.IsActive,
                    CreatedAt = company.CreatedAt,
                    UpdatedAt = company.UpdatedAt,
                    CreatedByUser = createdByUser
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating company");
                throw;
            }
        }

        public async Task<CompanyResponseDto> UpdateAsync(int id, CompanyUpdateDto dto, int userId)
        {
            try
            {
                logger.LogInformation("Updating company: ID={Id}", id);

                var company = await context.Companies
                    .Include(c => c.CreatedByUser)
                    .Include(c => c.UpdatedByUser)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (company is null)
                {
                    throw new KeyNotFoundException($"Company with ID {id} not found.");
                }

                company.Name = dto.Name.Trim();
                company.Address = dto.Address?.Trim();
                company.IsActive = dto.IsActive;
                company.UpdatedAt = DateTime.UtcNow;
                company.UpdatedBy = userId;

                await context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await activityLog.LogAsync(
                        module: "Letter Numbering - Company",
                        entityId: company.Id,
                        action: "Update",
                        userId: userId,
                        description: $"Mengubah perusahaan: {company.Code} - {company.Name}"
                    );
                }
                catch (Exception logEx)
                {
                    logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                }

                logger.LogInformation("✅ Company updated: ID={Id}", company.Id);

                return new CompanyResponseDto
                {
                    Id = company.Id,
                    Code = company.Code,
                    Name = company.Name,
                    Address = company.Address,
                    IsActive = company.IsActive,
                    CreatedAt = company.CreatedAt,
                    UpdatedAt = company.UpdatedAt,
                    CreatedByUser = company.CreatedByUser != null ? new UserInfoDto
                    {
                        UserId = company.CreatedByUser.UserId,
                        Username = company.CreatedByUser.Username,
                        FullName = company.CreatedByUser.FullName,
                        Email = company.CreatedByUser.Email,
                        PhotoUrl = company.CreatedByUser.PhotoUrl
                    } : null,
                    UpdatedByUser = company.UpdatedByUser != null ? new UserInfoDto
                    {
                        UserId = company.UpdatedByUser.UserId,
                        Username = company.UpdatedByUser.Username,
                        FullName = company.UpdatedByUser.FullName,
                        Email = company.UpdatedByUser.Email,
                        PhotoUrl = company.UpdatedByUser.PhotoUrl
                    } : null
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating company");
                throw;
            }
        }

        public async Task DeleteAsync(int id, int userId)
        {
            try
            {
                logger.LogInformation("Deleting company: ID={Id}", id);

                var company = await context.Companies.FindAsync(id);
                if (company is null)
                {
                    throw new KeyNotFoundException($"Company with ID {id} not found.");
                }

                // Check if used in letter numbers
                var isUsed = await context.LetterNumbers
                    .AnyAsync(l => l.CompanyId == id);

                if (isUsed)
                {
                    throw new InvalidOperationException(
                        "Cannot delete company that is being used in letter numbers. " +
                        "Consider deactivating it instead.");
                }

                var code = company.Code;
                var name = company.Name;

                context.Companies.Remove(company);
                await context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await activityLog.LogAsync(
                        module: "Letter Numbering - Company",
                        entityId: id,
                        action: "Delete",
                        userId: userId,
                        description: $"Menghapus perusahaan: {code} - {name}"
                    );
                }
                catch (Exception logEx)
                {
                    logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                }

                logger.LogInformation("✅ Company deleted: ID={Id}", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting company");
                throw;
            }
        }

        public async Task<CompanyResponseDto?> GetByIdAsync(int id)
        {
            var company = await context.Companies
                .AsNoTracking()
                .Include(c => c.CreatedByUser)
                .Include(c => c.UpdatedByUser)
                .Where(c => c.Id == id)
                .Select(c => new CompanyResponseDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Address = c.Address,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CreatedByUser = c.CreatedByUser != null ? new UserInfoDto
                    {
                        UserId = c.CreatedByUser.UserId,
                        Username = c.CreatedByUser.Username,
                        FullName = c.CreatedByUser.FullName,
                        Email = c.CreatedByUser.Email,
                        PhotoUrl = c.CreatedByUser.PhotoUrl
                    } : null,
                    UpdatedByUser = c.UpdatedByUser != null ? new UserInfoDto
                    {
                        UserId = c.UpdatedByUser.UserId,
                        Username = c.UpdatedByUser.Username,
                        FullName = c.UpdatedByUser.FullName,
                        Email = c.UpdatedByUser.Email,
                        PhotoUrl = c.UpdatedByUser.PhotoUrl
                    } : null
                })
                .FirstOrDefaultAsync();

            return company;
        }

        public async Task<PagedResultDto<CompanyListDto>> GetAllAsync(CompanyQueryDto query)
        {
            var baseQuery = context.Companies.AsNoTracking();

            // Apply filters
            if (query.IsActive.HasValue)
            {
                baseQuery = baseQuery.Where(c => c.IsActive == query.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                baseQuery = baseQuery.Where(c =>
                    c.Code.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                    c.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
            }

            // Get total count
            var totalCount = await baseQuery.CountAsync();

            // Apply pagination
            var companies = await baseQuery
                .OrderBy(c => c.Code)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new CompanyListDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return new PagedResultDto<CompanyListDto>(companies, query.Page, query.PageSize, totalCount);
        }
    }
}
