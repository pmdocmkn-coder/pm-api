using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Models;

namespace Pm.Services
{
    public class LetterNumberService(
        AppDbContext context,
        IActivityLogService activityLog,
        ILogger<LetterNumberService> logger) : ILetterNumberService
    {

        public async Task<LetterNumberResponseDto> GenerateLetterNumberAsync(LetterNumberCreateDto dto, int userId)
        {
            try
            {
                logger.LogInformation("Generating letter number for CompanyId={CompanyId}, DocumentTypeId={DocumentTypeId}",
                    dto.CompanyId, dto.DocumentTypeId);

                // Validate DocumentType exists and is active
                var documentType = await context.DocumentTypes
                    .Where(d => d.Id == dto.DocumentTypeId && d.IsActive)
                    .FirstOrDefaultAsync();

                if (documentType is null)
                {
                    throw new ArgumentException($"Document type with ID {dto.DocumentTypeId} not found or inactive.");
                }

                // Validate Company exists and is active
                var company = await context.Companies
                    .Where(c => c.Id == dto.CompanyId && c.IsActive)
                    .FirstOrDefaultAsync();

                if (company is null)
                {
                    throw new ArgumentException($"Company with ID {dto.CompanyId} not found or inactive.");
                }

                var year = dto.LetterDate.Year;
                var month = dto.LetterDate.Month;

                // Use transaction to handle race conditions
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    // Get max sequence number for this combination (Company + DocumentType + Year)
                    var maxSequence = await context.LetterNumbers
                        .Where(l => l.CompanyId == dto.CompanyId
                                 && l.DocumentTypeId == dto.DocumentTypeId
                                 && l.Year == year)
                        .MaxAsync(l => (int?)l.SequenceNumber) ?? 0;

                    var newSequence = maxSequence + 1;

                    // Generate formatted number: {seq:D2}/MKN-{companyCode}/{docTypeCode}/{romanMonth}/{year}
                    var romanMonth = RomanNumeralHelper.ToRoman(month);
                    var formattedNumber = $"{newSequence:D2}/MKN-{company.Code}/{documentType.Code}/{romanMonth}/{year}";

                    var letterNumber = new LetterNumber
                    {
                        FormattedNumber = formattedNumber,
                        SequenceNumber = newSequence,
                        DocumentTypeId = dto.DocumentTypeId,
                        CompanyId = dto.CompanyId,
                        Year = year,
                        Month = month,
                        LetterDate = dto.LetterDate.Date,
                        Subject = dto.Subject.Trim(),
                        Recipient = dto.Recipient.Trim(),
                        AttachmentUrl = dto.AttachmentUrl?.Trim(),
                        Status = dto.Status,
                        CreatedBy = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.LetterNumbers.Add(letterNumber);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

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

                    // ActivityLog
                    try
                    {
                        await activityLog.LogAsync(
                            module: "Letter Numbering",
                            entityId: letterNumber.Id,
                            action: "Create",
                            userId: userId,
                            description: $"Generate nomor surat: {formattedNumber} - {dto.Subject}"
                        );
                    }
                    catch (Exception logEx)
                    {
                        logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                    }

                    logger.LogInformation("✅ Letter number generated: {FormattedNumber}", formattedNumber);

                    return new LetterNumberResponseDto
                    {
                        Id = letterNumber.Id,
                        FormattedNumber = letterNumber.FormattedNumber,
                        SequenceNumber = letterNumber.SequenceNumber,
                        Year = letterNumber.Year,
                        Month = letterNumber.Month,
                        LetterDate = letterNumber.LetterDate,
                        Subject = letterNumber.Subject,
                        Recipient = letterNumber.Recipient,
                        AttachmentUrl = letterNumber.AttachmentUrl,
                        Status = letterNumber.Status.ToString(),
                        CreatedAt = letterNumber.CreatedAt,
                        UpdatedAt = letterNumber.UpdatedAt,
                        DocumentType = new DocumentTypeListDto
                        {
                            Id = documentType.Id,
                            Code = documentType.Code,
                            Name = documentType.Name,
                            IsActive = documentType.IsActive
                        },
                        Company = new CompanyListDto
                        {
                            Id = company.Id,
                            Code = company.Code,
                            Name = company.Name,
                            IsActive = company.IsActive
                        },
                        CreatedByUser = createdByUser
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating letter number");
                throw;
            }
        }

        public async Task<PagedResultDto<LetterNumberListDto>> GetLetterNumbersAsync(LetterNumberQueryDto query)
        {
            try
            {
                var baseQuery = context.LetterNumbers
                    .AsNoTracking()
                    .Include(l => l.DocumentType)
                    .Include(l => l.Company)
                    .Include(l => l.CreatedByUser)
                    .AsQueryable();

                // Apply filters
                if (query.DocumentTypeId.HasValue)
                {
                    baseQuery = baseQuery.Where(l => l.DocumentTypeId == query.DocumentTypeId.Value);
                }

                if (query.CompanyId.HasValue)
                {
                    baseQuery = baseQuery.Where(l => l.CompanyId == query.CompanyId.Value);
                }

                if (query.Status.HasValue)
                {
                    baseQuery = baseQuery.Where(l => l.Status == query.Status.Value);
                }

                if (query.Year.HasValue)
                {
                    baseQuery = baseQuery.Where(l => l.Year == query.Year.Value);
                }

                if (query.Month.HasValue)
                {
                    baseQuery = baseQuery.Where(l => l.Month == query.Month.Value);
                }

                if (query.StartDate.HasValue)
                {
                    baseQuery = baseQuery.Where(l => l.LetterDate >= query.StartDate.Value.Date);
                }

                if (query.EndDate.HasValue)
                {
                    baseQuery = baseQuery.Where(l => l.LetterDate <= query.EndDate.Value.Date);
                }

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var searchTerm = query.Search.ToLower();
                    baseQuery = baseQuery.Where(l =>
                        l.FormattedNumber.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                        l.Subject.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                        l.Recipient.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
                }

                // Get total count
                var totalCount = await baseQuery.CountAsync();

                // Apply pagination
                var items = await baseQuery
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(l => new LetterNumberListDto
                    {
                        Id = l.Id,
                        FormattedNumber = l.FormattedNumber,
                        LetterDate = l.LetterDate,
                        Subject = l.Subject,
                        Recipient = l.Recipient,
                        Status = l.Status.ToString(),
                        DocumentTypeCode = l.DocumentType!.Code,
                        CompanyCode = l.Company!.Code,
                        CreatedByName = l.CreatedByUser != null ? l.CreatedByUser.FullName : null
                    })
                    .ToListAsync();

                return new PagedResultDto<LetterNumberListDto>(items, query.Page, query.PageSize, totalCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting letter numbers");
                throw;
            }
        }

        public async Task<LetterNumberResponseDto?> GetLetterNumberByIdAsync(int id)
        {
            var letterNumber = await context.LetterNumbers
                .AsNoTracking()
                .Include(l => l.DocumentType)
                .Include(l => l.Company)
                .Include(l => l.CreatedByUser)
                .Include(l => l.UpdatedByUser)
                .Where(l => l.Id == id)
                .Select(l => new LetterNumberResponseDto
                {
                    Id = l.Id,
                    FormattedNumber = l.FormattedNumber,
                    SequenceNumber = l.SequenceNumber,
                    Year = l.Year,
                    Month = l.Month,
                    LetterDate = l.LetterDate,
                    Subject = l.Subject,
                    Recipient = l.Recipient,
                    AttachmentUrl = l.AttachmentUrl,
                    Status = l.Status.ToString(),
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt,
                    DocumentType = new DocumentTypeListDto
                    {
                        Id = l.DocumentType!.Id,
                        Code = l.DocumentType.Code,
                        Name = l.DocumentType.Name,
                        IsActive = l.DocumentType.IsActive
                    },
                    Company = new CompanyListDto
                    {
                        Id = l.Company!.Id,
                        Code = l.Company.Code,
                        Name = l.Company.Name,
                        IsActive = l.Company.IsActive
                    },
                    CreatedByUser = l.CreatedByUser != null ? new UserInfoDto
                    {
                        UserId = l.CreatedByUser.UserId,
                        Username = l.CreatedByUser.Username,
                        FullName = l.CreatedByUser.FullName,
                        Email = l.CreatedByUser.Email,
                        PhotoUrl = l.CreatedByUser.PhotoUrl
                    } : null,
                    UpdatedByUser = l.UpdatedByUser != null ? new UserInfoDto
                    {
                        UserId = l.UpdatedByUser.UserId,
                        Username = l.UpdatedByUser.Username,
                        FullName = l.UpdatedByUser.FullName,
                        Email = l.UpdatedByUser.Email,
                        PhotoUrl = l.UpdatedByUser.PhotoUrl
                    } : null
                })
                .FirstOrDefaultAsync();

            return letterNumber;
        }

        public async Task<LetterNumberResponseDto> UpdateLetterNumberAsync(int id, LetterNumberUpdateDto dto, int userId)
        {
            try
            {
                logger.LogInformation("Updating letter number: ID={Id}", id);

                var letterNumber = await context.LetterNumbers
                    .Include(l => l.DocumentType)
                    .Include(l => l.Company)
                    .Include(l => l.CreatedByUser)
                    .Include(l => l.UpdatedByUser)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (letterNumber is null)
                {
                    throw new KeyNotFoundException($"Letter number with ID {id} not found.");
                }

                letterNumber.Subject = dto.Subject.Trim();
                letterNumber.Recipient = dto.Recipient.Trim();
                letterNumber.AttachmentUrl = dto.AttachmentUrl?.Trim();
                letterNumber.Status = dto.Status;
                letterNumber.UpdatedAt = DateTime.UtcNow;
                letterNumber.UpdatedBy = userId;

                await context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await activityLog.LogAsync(
                        module: "Letter Numbering",
                        entityId: letterNumber.Id,
                        action: "Update",
                        userId: userId,
                        description: $"Update surat: {letterNumber.FormattedNumber} - {letterNumber.Subject}"
                    );
                }
                catch (Exception logEx)
                {
                    logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                }

                logger.LogInformation("✅ Letter number updated: {FormattedNumber}", letterNumber.FormattedNumber);

                return new LetterNumberResponseDto
                {
                    Id = letterNumber.Id,
                    FormattedNumber = letterNumber.FormattedNumber,
                    SequenceNumber = letterNumber.SequenceNumber,
                    Year = letterNumber.Year,
                    Month = letterNumber.Month,
                    LetterDate = letterNumber.LetterDate,
                    Subject = letterNumber.Subject,
                    Recipient = letterNumber.Recipient,
                    AttachmentUrl = letterNumber.AttachmentUrl,
                    Status = letterNumber.Status.ToString(),
                    CreatedAt = letterNumber.CreatedAt,
                    UpdatedAt = letterNumber.UpdatedAt,
                    DocumentType = new DocumentTypeListDto
                    {
                        Id = letterNumber.DocumentType!.Id,
                        Code = letterNumber.DocumentType!.Code,
                        Name = letterNumber.DocumentType.Name,
                        IsActive = letterNumber.DocumentType.IsActive
                    },
                    Company = new CompanyListDto
                    {
                        Id = letterNumber.Company!.Id,
                        Code = letterNumber.Company!.Code,
                        Name = letterNumber.Company.Name,
                        IsActive = letterNumber.Company.IsActive
                    },
                    CreatedByUser = letterNumber.CreatedByUser != null ? new UserInfoDto
                    {
                        UserId = letterNumber.CreatedByUser.UserId,
                        Username = letterNumber.CreatedByUser.Username,
                        FullName = letterNumber.CreatedByUser.FullName,
                        Email = letterNumber.CreatedByUser.Email,
                        PhotoUrl = letterNumber.CreatedByUser.PhotoUrl
                    } : null,
                    UpdatedByUser = letterNumber.UpdatedByUser != null ? new UserInfoDto
                    {
                        UserId = letterNumber.UpdatedByUser.UserId,
                        Username = letterNumber.UpdatedByUser.Username,
                        FullName = letterNumber.UpdatedByUser.FullName,
                        Email = letterNumber.UpdatedByUser.Email,
                        PhotoUrl = letterNumber.UpdatedByUser.PhotoUrl
                    } : null
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating letter number");
                throw;
            }
        }

        public async Task DeleteLetterNumberAsync(int id, int userId)
        {
            try
            {
                logger.LogInformation("Deleting letter number: ID={Id}", id);

                var letterNumber = await context.LetterNumbers.FindAsync(id);
                if (letterNumber is null)
                {
                    throw new KeyNotFoundException($"Letter number with ID {id} not found.");
                }

                var formattedNumber = letterNumber.FormattedNumber;
                var subject = letterNumber.Subject;

                context.LetterNumbers.Remove(letterNumber);
                await context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await activityLog.LogAsync(
                        module: "Letter Numbering",
                        entityId: id,
                        action: "Delete",
                        userId: userId,
                        description: $"Hapus surat: {formattedNumber} - {subject}"
                    );
                }
                catch (Exception logEx)
                {
                    logger.LogWarning(logEx, "⚠️ ActivityLog failed (non-critical)");
                }

                logger.LogInformation("✅ Letter number deleted: {FormattedNumber}", formattedNumber);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting letter number");
                throw;
            }
        }
    }
}
