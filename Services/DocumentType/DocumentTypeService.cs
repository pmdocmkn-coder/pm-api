using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.Models;

namespace Pm.Services
{
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

                // Validate unique code
                var exists = await context.DocumentTypes
                    .AnyAsync(d => d.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    throw new ArgumentException($"Document type with code '{dto.Code}' already exists.");
                }

                var documentType = new DocumentType
                {
                    Code = dto.Code.ToUpper().Trim(),
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

                return new DocumentTypeResponseDto
                {
                    Id = documentType.Id,
                    Code = documentType.Code,
                    Name = documentType.Name,
                    Description = documentType.Description,
                    IsActive = documentType.IsActive,
                    CreatedAt = documentType.CreatedAt,
                    UpdatedAt = documentType.UpdatedAt
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

                var documentType = await context.DocumentTypes.FindAsync(id);
                if (documentType is null)
                {
                    throw new KeyNotFoundException($"Document type with ID {id} not found.");
                }

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
                    UpdatedAt = documentType.UpdatedAt
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

                var documentType = await context.DocumentTypes.FindAsync(id);
                if (documentType is null)
                {
                    throw new KeyNotFoundException($"Document type with ID {id} not found.");
                }

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
                .Where(d => d.Id == id)
                .Select(d => new DocumentTypeResponseDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Description = d.Description,
                    IsActive = d.IsActive,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return documentType;
        }

        public async Task<List<DocumentTypeListDto>> GetAllAsync(bool activeOnly = true)
        {
            var query = context.DocumentTypes.AsNoTracking();

            if (activeOnly)
            {
                query = query.Where(d => d.IsActive);
            }

            var documentTypes = await query
                .OrderBy(d => d.Code)
                .Select(d => new DocumentTypeListDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    IsActive = d.IsActive
                })
                .ToListAsync();

            return documentTypes;
        }
    }
}
