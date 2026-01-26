using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;

namespace Pm.Services.DocumentType;

public class DocumentTypeService(
    AppDbContext context,
    IActivityLogService activityLog,
    ILogger<DocumentTypeService> logger) : IDocumentTypeService
{

    public async Task<DocumentTypeResponseDto> CreateAsync(DocumentTypeCreateDto dto, int userId)
    {
        try
        {
            logger.LogInformation("Creating document type: {Code}", dto.Code);

            // Validate unique code (case-insensitive)
            // Note: Using ToUpper() instead of StringComparison.OrdinalIgnoreCase
            // because EF Core MySQL doesn't support StringComparison translation
#pragma warning disable CA1862 // Use StringComparison for performance
            var codeUpper = dto.Code.ToUpper().Trim();
            var exists = await context.DocumentTypes
                .AnyAsync(d => d.Code.ToUpper() == codeUpper);
#pragma warning restore CA1862

            if (exists)
            {
                throw new ArgumentException($"Document type with code '{dto.Code}' already exists.");
            }

            var documentType = new Pm.Models.DocumentType
            {
                Code = codeUpper,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                IsActive = true,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            context.DocumentTypes.Add(documentType);
            await context.SaveChangesAsync();

            // ActivityLog
            try
            {
                await activityLog.LogAsync(
                    module: "Letter Numbering - DocumentType",
                    entityId: documentType.Id,
                    action: "Create",
                    userId: userId,
                    description: $"Membuat jenis surat: {documentType.Code} - {documentType.Name}"
                );
            }
            catch (Exception logEx)
            {
                logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
            }

            logger.LogInformation("✅ Document type created: ID={Id}", documentType.Id);

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

            return new DocumentTypeResponseDto
            {
                Id = documentType.Id,
                Code = documentType.Code,
                Name = documentType.Name,
                Description = documentType.Description,
                IsActive = documentType.IsActive,
                CreatedAt = documentType.CreatedAt,
                UpdatedAt = documentType.UpdatedAt,
                CreatedByUser = createdByUser
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating document type");
            throw;
        }
    }

    public async Task<DocumentTypeResponseDto> UpdateAsync(int id, DocumentTypeUpdateDto dto, int userId)
    {
        try
        {
            logger.LogInformation("Updating document type: ID={Id}", id);

            var documentType = await context.DocumentTypes
                .Include(d => d.CreatedByUser)
                .Include(d => d.UpdatedByUser)
                .FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new KeyNotFoundException($"Document type with ID {id} not found.");

            documentType.Name = dto.Name.Trim();
            documentType.Description = dto.Description?.Trim();
            documentType.IsActive = dto.IsActive;
            documentType.UpdatedAt = DateTime.UtcNow;
            documentType.UpdatedBy = userId;

            await context.SaveChangesAsync();

            // ActivityLog
            try
            {
                await activityLog.LogAsync(
                    module: "Letter Numbering - DocumentType",
                    entityId: documentType.Id,
                    action: "Update",
                    userId: userId,
                    description: $"Mengubah jenis surat: {documentType.Code} - {documentType.Name}"
                );
            }
            catch (Exception logEx)
            {
                logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
            }

            logger.LogInformation("✅ Document type updated: ID={Id}", documentType.Id);

            return new DocumentTypeResponseDto
            {
                Id = documentType.Id,
                Code = documentType.Code,
                Name = documentType.Name,
                Description = documentType.Description,
                IsActive = documentType.IsActive,
                CreatedAt = documentType.CreatedAt,
                UpdatedAt = documentType.UpdatedAt,
                CreatedByUser = documentType.CreatedByUser != null ? new UserInfoDto
                {
                    UserId = documentType.CreatedByUser.UserId,
                    Username = documentType.CreatedByUser.Username,
                    FullName = documentType.CreatedByUser.FullName,
                    Email = documentType.CreatedByUser.Email,
                    PhotoUrl = documentType.CreatedByUser.PhotoUrl
                } : null,
                UpdatedByUser = documentType.UpdatedByUser != null ? new UserInfoDto
                {
                    UserId = documentType.UpdatedByUser.UserId,
                    Username = documentType.UpdatedByUser.Username,
                    FullName = documentType.UpdatedByUser.FullName,
                    Email = documentType.UpdatedByUser.Email,
                    PhotoUrl = documentType.UpdatedByUser.PhotoUrl
                } : null
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating document type");
            throw;
        }
    }

    public async Task DeleteAsync(int id, int userId)
    {
        try
        {
            logger.LogInformation("Deleting document type: ID={Id}", id);

            var documentType = await context.DocumentTypes.FindAsync(id)
                ?? throw new KeyNotFoundException($"Document type with ID {id} not found.");

            // Check if used in letter numbers
            var isUsed = await context.LetterNumbers
                .AnyAsync(l => l.DocumentTypeId == id);

            if (isUsed)
            {
                throw new InvalidOperationException(
                    "Cannot delete document type that is being used in letter numbers. " +
                    "Consider deactivating it instead.");
            }

            var code = documentType.Code;
            var name = documentType.Name;

            context.DocumentTypes.Remove(documentType);
            await context.SaveChangesAsync();

            // ActivityLog
            try
            {
                await activityLog.LogAsync(
                    module: "Letter Numbering - DocumentType",
                    entityId: id,
                    action: "Delete",
                    userId: userId,
                    description: $"Menghapus jenis surat: {code} - {name}"
                );
            }
            catch (Exception logEx)
            {
                logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
            }

            logger.LogInformation("✅ Document type deleted: ID={Id}", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting document type");
            throw;
        }
    }

    public async Task<DocumentTypeResponseDto?> GetByIdAsync(int id)
    {
        var documentType = await context.DocumentTypes
            .AsNoTracking()
            .Include(d => d.CreatedByUser)
            .Include(d => d.UpdatedByUser)
            .Where(d => d.Id == id)
            .Select(d => new DocumentTypeResponseDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                CreatedByUser = d.CreatedByUser != null ? new UserInfoDto
                {
                    UserId = d.CreatedByUser.UserId,
                    Username = d.CreatedByUser.Username,
                    FullName = d.CreatedByUser.FullName,
                    Email = d.CreatedByUser.Email,
                    PhotoUrl = d.CreatedByUser.PhotoUrl
                } : null,
                UpdatedByUser = d.UpdatedByUser != null ? new UserInfoDto
                {
                    UserId = d.UpdatedByUser.UserId,
                    Username = d.UpdatedByUser.Username,
                    FullName = d.UpdatedByUser.FullName,
                    Email = d.UpdatedByUser.Email,
                    PhotoUrl = d.UpdatedByUser.PhotoUrl
                } : null
            })
            .FirstOrDefaultAsync();

        return documentType;
    }

    public async Task<PagedResultDto<DocumentTypeListDto>> GetAllAsync(DocumentTypeQueryDto query)
    {
        var baseQuery = context.DocumentTypes.AsNoTracking();

        // Apply filters
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

        // Get total count
        var totalCount = await baseQuery.CountAsync();

        // Apply pagination
        var documentTypes = await baseQuery
            .OrderBy(d => d.Code)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DocumentTypeListDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive
            })
            .ToListAsync();

        return new PagedResultDto<DocumentTypeListDto>(documentTypes, query.Page, query.PageSize, totalCount);
    }
}

