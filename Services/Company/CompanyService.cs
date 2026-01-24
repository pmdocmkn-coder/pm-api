using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
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

                return new CompanyResponseDto
                {
                    Id = company.Id,
                    Code = company.Code,
                    Name = company.Name,
                    Address = company.Address,
                    IsActive = company.IsActive,
                    CreatedAt = company.CreatedAt,
                    UpdatedAt = company.UpdatedAt
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

                var company = await context.Companies.FindAsync(id);
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
                    UpdatedAt = company.UpdatedAt
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
                .Where(c => c.Id == id)
                .Select(c => new CompanyResponseDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Address = c.Address,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return company;
        }

        public async Task<List<CompanyListDto>> GetAllAsync(bool activeOnly = true)
        {
            var query = context.Companies.AsNoTracking();

            if (activeOnly)
            {
                query = query.Where(c => c.IsActive);
            }

            var companies = await query
                .OrderBy(c => c.Code)
                .Select(c => new CompanyListDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return companies;
        }
    }
}
