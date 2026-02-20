using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Models;
using Pm.Enums;

namespace Pm.Services
{
    public class QuotationService : IQuotationService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<QuotationService> _logger;

        public QuotationService(AppDbContext context, IActivityLogService activityLog, ILogger<QuotationService> logger)
        {
            _context = context;
            _activityLog = activityLog;
            _logger = logger;
        }

        public async Task<QuotationResponseDto> CreateQuotationAsync(QuotationCreateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("Creating quotation for customerId={CustomerId}, date={Date}",
                    dto.CustomerId, dto.QuotationDate);

                // Validate Customer exists
                var customer = await _context.Companies
                    .Where(c => c.Id == dto.CustomerId && c.IsActive)
                    .FirstOrDefaultAsync()
                    ?? throw new ArgumentException($"Customer with ID {dto.CustomerId} not found or inactive");

                var year = dto.QuotationDate.Year;
                var month = dto.QuotationDate.Month;

                // Use execution strategy for transaction
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Get all existing sequence numbers for this year
                        var existingSequences = await _context.Quotations
                            .Where(q => q.Year == year)
                            .Select(q => q.SequenceNumber)
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
                            newSequence = existingSequences.Max() + 1;
                            for (int i = 1; i <= existingSequences.Max(); i++)
                            {
                                if (!existingSequences.Contains(i))
                                {
                                    newSequence = i;
                                    break;
                                }
                            }
                        }

                        // Generate formatted number: {seq:D2}/MKN/QUOT/{romanMonth}/{year}
                        _logger.LogInformation("Generating formatted number for seq={Sequence}, type=QUOT",
                            newSequence);
                        var romanMonth = RomanNumeralHelper.ToRoman(month);
                        var formattedNumber = $"{newSequence:D2}/MKN/QUOT/{romanMonth}/{year}";

                        var quotation = new Models.Quotation
                        {
                            FormattedNumber = formattedNumber,
                            SequenceNumber = newSequence,
                            Year = year,
                            Month = month,
                            CustomerId = dto.CustomerId,
                            CustomerName = customer.Name, // Denormalized for history
                            Description = dto.Description.Trim(),
                            QuotationDate = dto.QuotationDate.Date,
                            Notes = dto.Notes?.Trim(),
                            Status = dto.Status,
                            CreatedBy = userId,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Quotations.Add(quotation);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // ActivityLog
                        try
                        {
                            await _activityLog.LogAsync("Quotation", quotation.Id, "Create", userId,
                                $"Created quotation: {formattedNumber} for {customer.Name}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to write activity log for quotation creation");
                        }

                        _logger.LogInformation("Quotation created successfully: {FormattedNumber}", formattedNumber);

                        // Return response with full data
                        return await GetQuotationByIdAsync(quotation.Id)
                            ?? throw new InvalidOperationException("Failed to retrieve created quotation");
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
                _logger.LogError(ex, "Error creating quotation");
                throw;
            }
        }

        public async Task<PagedResultDto<QuotationListDto>> GetQuotationsAsync(QuotationQueryDto query)
        {
            try
            {
                var queryable = _context.Quotations
                    .Include(q => q.Customer)
                    .Include(q => q.CreatedByUser)
                    .AsQueryable();

                // Filters
                if (query.CustomerId.HasValue)
                    queryable = queryable.Where(q => q.CustomerId == query.CustomerId.Value);

                if (query.Status.HasValue)
                    queryable = queryable.Where(q => q.Status == query.Status.Value);

                if (query.Year.HasValue)
                    queryable = queryable.Where(q => q.Year == query.Year.Value);

                if (query.Month.HasValue)
                    queryable = queryable.Where(q => q.Month == query.Month.Value);

                if (query.StartDate.HasValue)
                    queryable = queryable.Where(q => q.QuotationDate >= query.StartDate.Value.Date);

                if (query.EndDate.HasValue)
                    queryable = queryable.Where(q => q.QuotationDate <= query.EndDate.Value.Date);

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var search = query.Search.ToLower();
                    queryable = queryable.Where(q =>
                        q.FormattedNumber.ToLower().Contains(search) ||
                        q.CustomerName.ToLower().Contains(search) ||
                        q.Description.ToLower().Contains(search));
                }

                // Total count
                var totalCount = await queryable.CountAsync();

                // Pagination
                var items = await queryable
                    .OrderByDescending(q => q.CreatedAt)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(q => new QuotationListDto
                    {
                        Id = q.Id,
                        FormattedNumber = q.FormattedNumber,
                        QuotationDate = q.QuotationDate,
                        CustomerName = q.CustomerName,
                        Description = q.Description.Length > 100 ? q.Description.Substring(0, 100) + "..." : q.Description,
                        Status = q.Status.ToString(),
                        CreatedByName = q.CreatedByUser != null ? q.CreatedByUser.FullName : null
                    })
                    .ToListAsync();

                return new PagedResultDto<QuotationListDto>(items, query.Page, query.PageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quotations");
                throw;
            }
        }

        public async Task<QuotationResponseDto?> GetQuotationByIdAsync(int id)
        {
            try
            {
                return await _context.Quotations
                    .Include(q => q.Customer)
                    .Include(q => q.CreatedByUser)
                    .Include(q => q.UpdatedByUser)
                    .Where(q => q.Id == id)
                    .Select(q => new QuotationResponseDto
                    {
                        Id = q.Id,
                        FormattedNumber = q.FormattedNumber,
                        SequenceNumber = q.SequenceNumber,
                        Year = q.Year,
                        Month = q.Month,
                        CustomerId = q.CustomerId,
                        CustomerName = q.CustomerName,
                        Description = q.Description,
                        QuotationDate = q.QuotationDate,
                        Notes = q.Notes,
                        Status = q.Status.ToString(),
                        CreatedAt = q.CreatedAt,
                        UpdatedAt = q.UpdatedAt,
                        Customer = q.Customer != null ? new CompanyListDto
                        {
                            Id = q.Customer.Id,
                            Code = q.Customer.Code,
                            Name = q.Customer.Name,
                            IsActive = q.Customer.IsActive
                        } : null,
                        CreatedByUser = q.CreatedByUser != null ? new UserInfoDto
                        {
                            UserId = q.CreatedByUser.UserId,
                            Username = q.CreatedByUser.Username,
                            FullName = q.CreatedByUser.FullName,
                            Email = q.CreatedByUser.Email,
                            PhotoUrl = q.CreatedByUser.PhotoUrl,
                            EmployeeId = q.CreatedByUser.EmployeeId,
                            Division = q.CreatedByUser.Division
                        } : null,
                        UpdatedByUser = q.UpdatedByUser != null ? new UserInfoDto
                        {
                            UserId = q.UpdatedByUser.UserId,
                            Username = q.UpdatedByUser.Username,
                            FullName = q.UpdatedByUser.FullName,
                            Email = q.UpdatedByUser.Email,
                            PhotoUrl = q.UpdatedByUser.PhotoUrl,
                            EmployeeId = q.UpdatedByUser.EmployeeId,
                            Division = q.UpdatedByUser.Division
                        } : null
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quotation by ID: {Id}", id);
                throw;
            }
        }

        public async Task<QuotationResponseDto> UpdateQuotationAsync(int id, QuotationUpdateDto dto, int userId)
        {
            try
            {
                var quotation = await _context.Quotations
                    .FirstOrDefaultAsync(q => q.Id == id)
                    ?? throw new ArgumentException($"Quotation with ID {id} not found");

                _logger.LogInformation("Updating quotation: {FormattedNumber}", quotation.FormattedNumber);

                // Update basic fields
                quotation.Description = dto.Description.Trim();
                quotation.Notes = dto.Notes?.Trim();
                quotation.Status = dto.Status;
                quotation.UpdatedBy = userId;
                quotation.UpdatedAt = DateTime.UtcNow;

                // Update customer if provided
                if (dto.CustomerId.HasValue && dto.CustomerId.Value != quotation.CustomerId)
                {
                    var customer = await _context.Companies
                        .Where(c => c.Id == dto.CustomerId.Value && c.IsActive)
                        .FirstOrDefaultAsync()
                        ?? throw new ArgumentException($"Customer with ID {dto.CustomerId.Value} not found or inactive");

                    quotation.CustomerId = customer.Id;
                    quotation.CustomerName = customer.Name;
                }

                // Update date if provided
                if (dto.QuotationDate.HasValue)
                {
                    quotation.QuotationDate = dto.QuotationDate.Value.Date;
                }

                await _context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await _activityLog.LogAsync("Quotation", id, "Update", userId,
                        $"Updated quotation: {quotation.FormattedNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write activity log for quotation update");
                }

                _logger.LogInformation("Quotation updated successfully: {FormattedNumber}", quotation.FormattedNumber);

                return await GetQuotationByIdAsync(id)
                    ?? throw new InvalidOperationException("Failed to retrieve updated quotation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quotation");
                throw;
            }
        }

        public async Task DeleteQuotationAsync(int id, int userId, string? userRole = null)
        {
            try
            {
                var quotation = await _context.Quotations
                    .FirstOrDefaultAsync(q => q.Id == id)
                    ?? throw new ArgumentException($"Quotation with ID {id} not found");

                // Super Admin can delete any status, others only Draft
                bool isSuperAdmin = string.Equals(userRole, "Super Admin", StringComparison.OrdinalIgnoreCase);
                if (!isSuperAdmin && quotation.Status != QuotationStatus.Draft)
                {
                    throw new InvalidOperationException(
                        $"Hanya quotation dengan status Draft yang bisa dihapus. Status saat ini: {quotation.Status}");
                }

                _logger.LogInformation("Deleting draft quotation: {FormattedNumber}", quotation.FormattedNumber);

                var formattedNumber = quotation.FormattedNumber;
                _context.Quotations.Remove(quotation);
                await _context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await _activityLog.LogAsync("Quotation", id, "Delete", userId,
                        $"Deleted quotation: {formattedNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write activity log for quotation deletion");
                }

                _logger.LogInformation("Quotation deleted successfully: {FormattedNumber}", formattedNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quotation");
                throw;
            }
        }
    }
}
