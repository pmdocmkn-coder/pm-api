using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Models;
using Pm.Enums;

namespace Pm.Services
{
    public class LetterNumberService(AppDbContext _context, IActivityLogService _activityLog, ILogger<LetterNumberService> _logger) : ILetterNumberService
    {

        public async Task<LetterNumberResponseDto> GenerateLetterNumberAsync(LetterNumberCreateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("Generating letter number for CompanyId={CompanyId}, DocumentTypeId={DocumentTypeId}",
                    dto.CompanyId, dto.DocumentTypeId);

                // Validate DocumentType exists and is active
                var documentType = await _context.DocumentTypes
                    .Where(d => d.Id == dto.DocumentTypeId && d.IsActive)
                    .FirstOrDefaultAsync()
                    ?? throw new ArgumentException($"Document type with ID {dto.DocumentTypeId} not found or inactive.");

                // Validate Company exists and is active
                var company = await _context.Companies
                    .Where(c => c.Id == dto.CompanyId && c.IsActive)
                    .FirstOrDefaultAsync()
                    ?? throw new ArgumentException($"Company with ID {dto.CompanyId} not found or inactive.");

                var year = dto.LetterDate.Year;
                var month = dto.LetterDate.Month;

                // Use execution strategy for transaction
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Get all existing sequence numbers for this year (global across all doc types)
                        var existingSequences = await _context.LetterNumbers
                            .Where(l => l.Year == year)
                            .Select(l => l.SequenceNumber)
                            .OrderBy(s => s)
                            .ToListAsync();

                        // Find the lowest available sequence (reuse gaps from deleted drafts)
                        int newSequence;
                        if (existingSequences.Count == 0)
                        {
                            newSequence = 1;
                        }
                        else
                        {
                            newSequence = existingSequences.Max() + 1; // default: next after max
                            for (int i = 1; i <= existingSequences.Max(); i++)
                            {
                                if (!existingSequences.Contains(i))
                                {
                                    newSequence = i; // reuse the gap
                                    break;
                                }
                            }
                        }

                        // Generate formatted number: {seq:D2}/MKN-{companyCode}/{docTypeCode}/{romanMonth}/{year}
                        // If company is MKN, it should be {seq:D2}/MKN/{docTypeCode}/{romanMonth}/{year}
                        _logger.LogInformation("Generating formatted number for seq={Sequence}, company={CompanyCode}, type={TypeCode}",
                            newSequence, company.Code, documentType.Code);
                        var romanMonth = RomanNumeralHelper.ToRoman(month);

                        var companyPart = company.Code.Equals("MKN", StringComparison.OrdinalIgnoreCase) ? "MKN" : $"MKN-{company.Code}";
                        var formattedNumber = $"{newSequence:D2}/{companyPart}/{documentType.Code}/{romanMonth}/{year}";

                        var letterNumber = new LetterNumber
                        {
                            FormattedNumber = formattedNumber,
                            SequenceNumber = newSequence,
                            Year = year,
                            Month = month,
                            CompanyId = dto.CompanyId,
                            DocumentTypeId = dto.DocumentTypeId,
                            Subject = dto.Subject.Trim(),
                            Recipient = dto.Recipient?.Trim(),
                            LetterDate = dto.LetterDate.Date,
                            AttachmentUrl = dto.AttachmentUrl?.Trim(),
                            Status = dto.Status,
                            CreatedBy = userId,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.LetterNumbers.Add(letterNumber);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // ActivityLog
                        try
                        {
                            await _activityLog.LogAsync("LetterNumber", letterNumber.Id, "Create", userId,
                                $"Created letter number: {formattedNumber} for {company.Name} ({documentType.Name})");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to write activity log for letter number creation");
                        }

                        _logger.LogInformation("Letter number created successfully: {FormattedNumber}", formattedNumber);

                        // Return response with full data
                        return await GetLetterNumberByIdAsync(letterNumber.Id)
                            ?? throw new InvalidOperationException("Failed to retrieve created letter number");
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating letter number");
                throw;
            }
        }

        public async Task<PagedResultDto<LetterNumberListDto>> GetLetterNumbersAsync(LetterNumberQueryDto query)
        {
            try
            {
                var queryable = _context.LetterNumbers
                    .Include(l => l.Company)
                    .Include(l => l.DocumentType)
                    .Include(l => l.CreatedByUser)
                    .AsQueryable();

                // Filters
                if (query.CompanyId.HasValue)
                    queryable = queryable.Where(l => l.CompanyId == query.CompanyId.Value);

                if (query.DocumentTypeId.HasValue)
                    queryable = queryable.Where(l => l.DocumentTypeId == query.DocumentTypeId.Value);

                if (query.Year.HasValue)
                    queryable = queryable.Where(l => l.Year == query.Year.Value);

                if (query.Month.HasValue)
                    queryable = queryable.Where(l => l.Month == query.Month.Value);

                if (query.StartDate.HasValue)
                    queryable = queryable.Where(l => l.LetterDate >= query.StartDate.Value.Date);

                if (query.EndDate.HasValue)
                    queryable = queryable.Where(l => l.LetterDate <= query.EndDate.Value.Date);

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var search = query.Search.ToLower();
                    queryable = queryable.Where(l =>
                        l.FormattedNumber.ToLower().Contains(search) ||
                        (l.Company != null && l.Company.Name.ToLower().Contains(search)) ||
                        (l.Subject != null && l.Subject.ToLower().Contains(search)));
                }

                // Total count
                var totalCount = await queryable.CountAsync();

                // Pagination
                var items = await queryable
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(l => new LetterNumberListDto
                    {
                        Id = l.Id,
                        FormattedNumber = l.FormattedNumber,
                        LetterDate = l.LetterDate,
                        CompanyCode = l.Company != null ? l.Company.Code : "",
                        DocumentTypeCode = l.DocumentType != null ? l.DocumentType.Code : "",
                        Subject = l.Subject != null && l.Subject.Length > 100 ? l.Subject.Substring(0, 100) + "..." : l.Subject ?? "",
                        Recipient = l.Recipient,
                        Status = l.Status.ToString(),
                        CreatedByName = l.CreatedByUser != null ? l.CreatedByUser.FullName : null
                    })
                    .ToListAsync();

                return new PagedResultDto<LetterNumberListDto>(items, query.Page, query.PageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving letter numbers");
                throw;
            }
        }

        public async Task<LetterNumberResponseDto?> GetLetterNumberByIdAsync(int id)
        {
            try
            {
                return await _context.LetterNumbers
                    .Include(l => l.Company)
                    .Include(l => l.DocumentType)
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
                        Subject = l.Subject ?? "",
                        LetterDate = l.LetterDate,
                        Recipient = l.Recipient,
                        AttachmentUrl = l.AttachmentUrl,
                        Status = l.Status.ToString(),
                        CreatedAt = l.CreatedAt,
                        UpdatedAt = l.UpdatedAt,
                        Company = l.Company != null ? new CompanyListDto
                        {
                            Id = l.Company.Id,
                            Code = l.Company.Code,
                            Name = l.Company.Name,
                            IsActive = l.Company.IsActive
                        } : null,
                        DocumentType = l.DocumentType != null ? new DocumentTypeListDto
                        {
                            Id = l.DocumentType.Id,
                            Code = l.DocumentType.Code,
                            Name = l.DocumentType.Name,
                            IsActive = l.DocumentType.IsActive
                        } : null,
                        CreatedByUser = l.CreatedByUser != null ? new UserInfoDto
                        {
                            UserId = l.CreatedByUser.UserId,
                            Username = l.CreatedByUser.Username,
                            FullName = l.CreatedByUser.FullName,
                            Email = l.CreatedByUser.Email,
                            PhotoUrl = l.CreatedByUser.PhotoUrl,
                            EmployeeId = l.CreatedByUser.EmployeeId,
                            Division = l.CreatedByUser.Division
                        } : null,
                        UpdatedByUser = l.UpdatedByUser != null ? new UserInfoDto
                        {
                            UserId = l.UpdatedByUser.UserId,
                            Username = l.UpdatedByUser.Username,
                            FullName = l.UpdatedByUser.FullName,
                            Email = l.UpdatedByUser.Email,
                            PhotoUrl = l.UpdatedByUser.PhotoUrl,
                            EmployeeId = l.UpdatedByUser.EmployeeId,
                            Division = l.UpdatedByUser.Division
                        } : null
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving letter number by ID: {Id}", id);
                throw;
            }
        }

        public async Task<LetterNumberResponseDto> UpdateLetterNumberAsync(int id, LetterNumberUpdateDto dto, int userId)
        {
            try
            {
                var letterNumber = await _context.LetterNumbers
                    .FirstOrDefaultAsync(l => l.Id == id)
                    ?? throw new ArgumentException($"Letter number with ID {id} not found");

                _logger.LogInformation("Updating letter number: {FormattedNumber}", letterNumber.FormattedNumber);

                // Update fields
                letterNumber.Subject = dto.Subject.Trim();
                letterNumber.Recipient = dto.Recipient?.Trim();
                letterNumber.AttachmentUrl = dto.AttachmentUrl?.Trim();
                letterNumber.Status = dto.Status;
                letterNumber.UpdatedBy = userId;
                letterNumber.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await _activityLog.LogAsync("LetterNumber", id, "Update", userId,
                        $"Updated letter number: {letterNumber.FormattedNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write activity log for letter number update");
                }

                _logger.LogInformation("Letter number updated successfully: {FormattedNumber}", letterNumber.FormattedNumber);

                return await GetLetterNumberByIdAsync(id)
                    ?? throw new InvalidOperationException("Failed to retrieve updated letter number");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating letter number");
                throw;
            }
        }

        public async Task DeleteLetterNumberAsync(int id, int userId, string? userRole = null)
        {
            try
            {
                var letterNumber = await _context.LetterNumbers
                    .FirstOrDefaultAsync(l => l.Id == id)
                    ?? throw new ArgumentException($"Letter number with ID {id} not found");

                // Super Admin can delete any status, others only Draft
                bool isSuperAdmin = string.Equals(userRole, "Super Admin", StringComparison.OrdinalIgnoreCase);
                if (!isSuperAdmin && letterNumber.Status != LetterStatus.Draft)
                {
                    throw new InvalidOperationException(
                        $"Hanya surat dengan status Draft yang bisa dihapus. Status saat ini: {letterNumber.Status}");
                }

                _logger.LogInformation("Deleting draft letter number: {FormattedNumber}", letterNumber.FormattedNumber);

                var formattedNumber = letterNumber.FormattedNumber;
                _context.LetterNumbers.Remove(letterNumber);
                await _context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await _activityLog.LogAsync("LetterNumber", id, "Delete", userId,
                        $"Deleted letter number: {formattedNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write activity log for letter number deletion");
                }

                _logger.LogInformation("Letter number deleted successfully: {FormattedNumber}", formattedNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting letter number");
                throw;
            }
        }
    }
}
