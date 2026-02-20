using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Helper;
using Pm.Models;
using Pm.Enums;

namespace Pm.Services
{
    public class GatepassService : IGatepassService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly ILogger<GatepassService> _logger;

        public GatepassService(AppDbContext context, IActivityLogService activityLog, ILogger<GatepassService> logger)
        {
            _context = context;
            _activityLog = activityLog;
            _logger = logger;
        }
        public async Task<GatepassResponseDto> CreateGatepassAsync(GatepassCreateDto dto, int userId)
        {
            try
            {
                _logger.LogInformation("Creating gatepass for destination={Destination}, date={Date}",
                    dto.Destination, dto.GatepassDate);

                var year = dto.GatepassDate.Year;
                var month = dto.GatepassDate.Month;

                // Use execution strategy for transaction
                var strategy = _context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Get all existing sequence numbers for this year
                        var existingSequences = await _context.Gatepasses
                            .Where(g => g.Year == year)
                            .Select(g => g.SequenceNumber)
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

                        // Generate formatted number: {seq:D2}/MKN/GP/{romanMonth}/{year}
                        _logger.LogInformation("Generating formatted number for seq={Sequence}, type=GP",
                            newSequence);
                        var romanMonth = RomanNumeralHelper.ToRoman(month);
                        var formattedNumber = $"{newSequence:D2}/MKN/GP/{romanMonth}/{year}";

                        var gatepass = new Models.Gatepass
                        {
                            FormattedNumber = formattedNumber,
                            SequenceNumber = newSequence,
                            Year = year,
                            Month = month,
                            Destination = dto.Destination.Trim(),
                            PicName = dto.PicName.Trim(),
                            PicContact = dto.PicContact?.Trim(),
                            GatepassDate = dto.GatepassDate.Date,
                            SignatureQRCode = dto.SignatureQRCode?.Trim(),
                            Notes = dto.Notes?.Trim(),
                            Status = dto.Status,
                            CreatedBy = userId,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Gatepasses.Add(gatepass);
                        await _context.SaveChangesAsync();

                        // Add items if provided
                        if (dto.Items?.Any() == true)
                        {
                            var items = dto.Items.Select(i => new GatepassItem
                            {
                                GatepassId = gatepass.Id,
                                ItemName = i.ItemName.Trim(),
                                Quantity = i.Quantity,
                                Unit = i.Unit?.Trim() ?? "unit",
                                Description = i.Description?.Trim(),
                                SerialNumber = i.SerialNumber?.Trim()
                            }).ToList();

                            _context.GatepassItems.AddRange(items);
                            await _context.SaveChangesAsync();
                        }

                        await transaction.CommitAsync();

                        // ActivityLog
                        try
                        {
                            await _activityLog.LogAsync("Gatepass", gatepass.Id, "Create", userId,
                                $"Created gatepass: {formattedNumber}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to write activity log for gatepass creation");
                        }

                        _logger.LogInformation("Gatepass created successfully: {FormattedNumber}", formattedNumber);

                        // Return response with full data
                        return await GetGatepassByIdAsync(gatepass.Id)
                            ?? throw new InvalidOperationException("Failed to retrieve created gatepass");
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
                _logger.LogError(ex, "Error creating gatepass");
                throw;
            }
        }

        public async Task<PagedResultDto<GatepassListDto>> GetGatepassesAsync(GatepassQueryDto query)
        {
            try
            {
                var queryable = _context.Gatepasses
                    .Include(g => g.Items)
                    .Include(g => g.CreatedByUser)
                    .AsQueryable();

                // Filters
                if (query.Status.HasValue)
                    queryable = queryable.Where(g => g.Status == query.Status.Value);

                if (query.Year.HasValue)
                    queryable = queryable.Where(g => g.Year == query.Year.Value);

                if (query.Month.HasValue)
                    queryable = queryable.Where(g => g.Month == query.Month.Value);

                if (query.StartDate.HasValue)
                    queryable = queryable.Where(g => g.GatepassDate >= query.StartDate.Value.Date);

                if (query.EndDate.HasValue)
                    queryable = queryable.Where(g => g.GatepassDate <= query.EndDate.Value.Date);

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    var search = query.Search.ToLower();
                    queryable = queryable.Where(g =>
                        g.FormattedNumber.ToLower().Contains(search) ||
                        g.Destination.ToLower().Contains(search) ||
                        g.PicName.ToLower().Contains(search));
                }

                // Total count
                var totalCount = await queryable.CountAsync();

                // Pagination
                var items = await queryable
                    .OrderByDescending(g => g.CreatedAt)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(g => new GatepassListDto
                    {
                        Id = g.Id,
                        FormattedNumber = g.FormattedNumber,
                        GatepassDate = g.GatepassDate,
                        Destination = g.Destination,
                        PicName = g.PicName,
                        Status = g.Status.ToString(),
                        CreatedByName = g.CreatedByUser != null ? g.CreatedByUser.FullName : null,
                        ItemCount = g.Items.Count,
                        IsSigned = g.SignedByUserId != null
                    })
                    .ToListAsync();

                return new PagedResultDto<GatepassListDto>(items, query.Page, query.PageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving gatepasses");
                throw;
            }
        }

        public async Task<GatepassResponseDto?> GetGatepassByIdAsync(int id)
        {
            try
            {
                return await _context.Gatepasses
                    .Include(g => g.Items)
                    .Include(g => g.CreatedByUser)
                    .Include(g => g.UpdatedByUser)
                    .Include(g => g.SignedByUser)
                    .Where(g => g.Id == id)
                    .Select(g => new GatepassResponseDto
                    {
                        Id = g.Id,
                        FormattedNumber = g.FormattedNumber,
                        SequenceNumber = g.SequenceNumber,
                        Year = g.Year,
                        Month = g.Month,
                        Destination = g.Destination,
                        PicName = g.PicName,
                        PicContact = g.PicContact,
                        GatepassDate = g.GatepassDate,
                        SignatureQRCode = g.SignatureQRCode,
                        Notes = g.Notes,
                        Status = g.Status.ToString(),
                        CreatedAt = g.CreatedAt,
                        UpdatedAt = g.UpdatedAt,
                        CreatedByUser = g.CreatedByUser != null ? new UserInfoDto
                        {
                            UserId = g.CreatedByUser.UserId,
                            Username = g.CreatedByUser.Username,
                            FullName = g.CreatedByUser.FullName,
                            Email = g.CreatedByUser.Email,
                            PhotoUrl = g.CreatedByUser.PhotoUrl,
                            EmployeeId = g.CreatedByUser.EmployeeId,
                            Division = g.CreatedByUser.Division
                        } : null,
                        UpdatedByUser = g.UpdatedByUser != null ? new UserInfoDto
                        {
                            UserId = g.UpdatedByUser.UserId,
                            Username = g.UpdatedByUser.Username,
                            FullName = g.UpdatedByUser.FullName,
                            Email = g.UpdatedByUser.Email,
                            PhotoUrl = g.UpdatedByUser.PhotoUrl,
                            EmployeeId = g.UpdatedByUser.EmployeeId,
                            Division = g.UpdatedByUser.Division
                        } : null,
                        SignedByUser = g.SignedByUser != null ? new UserInfoDto
                        {
                            UserId = g.SignedByUser.UserId,
                            Username = g.SignedByUser.Username,
                            FullName = g.SignedByUser.FullName,
                            Email = g.SignedByUser.Email,
                            PhotoUrl = g.SignedByUser.PhotoUrl,
                            EmployeeId = g.SignedByUser.EmployeeId,
                            Division = g.SignedByUser.Division
                        } : null,
                        SignedAt = g.SignedAt,
                        VerificationToken = g.VerificationToken,
                        Items = g.Items.Select(i => new GatepassItemResponseDto
                        {
                            Id = i.Id,
                            ItemName = i.ItemName,
                            Quantity = i.Quantity,
                            Unit = i.Unit,
                            Description = i.Description,
                            SerialNumber = i.SerialNumber
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving gatepass by ID: {Id}", id);
                throw;
            }
        }

        public async Task<GatepassResponseDto> UpdateGatepassAsync(int id, GatepassUpdateDto dto, int userId)
        {
            try
            {
                var gatepass = await _context.Gatepasses
                    .Include(g => g.Items)
                    .FirstOrDefaultAsync(g => g.Id == id)
                    ?? throw new ArgumentException($"Gatepass with ID {id} not found");

                _logger.LogInformation("Updating gatepass: {FormattedNumber}", gatepass.FormattedNumber);

                // Update fields
                gatepass.Destination = dto.Destination.Trim();
                gatepass.PicName = dto.PicName.Trim();
                gatepass.PicContact = dto.PicContact?.Trim();
                gatepass.SignatureQRCode = dto.SignatureQRCode?.Trim();
                gatepass.Notes = dto.Notes?.Trim();
                gatepass.Status = dto.Status;
                gatepass.UpdatedBy = userId;
                gatepass.UpdatedAt = DateTime.UtcNow;

                // Update items - remove old, add new
                if (dto.Items != null)
                {
                    _context.GatepassItems.RemoveRange(gatepass.Items);

                    var newItems = dto.Items.Select(i => new GatepassItem
                    {
                        GatepassId = gatepass.Id,
                        ItemName = i.ItemName.Trim(),
                        Quantity = i.Quantity,
                        Unit = i.Unit?.Trim() ?? "unit",
                        Description = i.Description?.Trim(),
                        SerialNumber = i.SerialNumber?.Trim()
                    }).ToList();

                    _context.GatepassItems.AddRange(newItems);
                }

                await _context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await _activityLog.LogAsync("Gatepass", id, "Update", userId,
                        $"Updated gatepass: {gatepass.FormattedNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write activity log for gatepass update");
                }

                _logger.LogInformation("Gatepass updated successfully: {FormattedNumber}", gatepass.FormattedNumber);

                return await GetGatepassByIdAsync(id)
                    ?? throw new InvalidOperationException("Failed to retrieve updated gatepass");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating gatepass");
                throw;
            }
        }

        public async Task DeleteGatepassAsync(int id, int userId, string? userRole = null)
        {
            try
            {
                var gatepass = await _context.Gatepasses
                    .FirstOrDefaultAsync(g => g.Id == id)
                    ?? throw new ArgumentException($"Gatepass with ID {id} not found");

                // Super Admin can delete any status, others only Draft
                bool isSuperAdmin = string.Equals(userRole, "Super Admin", StringComparison.OrdinalIgnoreCase);
                if (!isSuperAdmin && gatepass.Status != GatepassStatus.Draft)
                {
                    throw new InvalidOperationException(
                        $"Hanya gatepass dengan status Draft yang bisa dihapus. Status saat ini: {gatepass.Status}");
                }

                _logger.LogInformation("Deleting draft gatepass: {FormattedNumber}", gatepass.FormattedNumber);

                var formattedNumber = gatepass.FormattedNumber;
                _context.Gatepasses.Remove(gatepass); // Items cascade delete
                await _context.SaveChangesAsync();

                // ActivityLog
                try
                {
                    await _activityLog.LogAsync("Gatepass", id, "Delete", userId,
                        $"Deleted gatepass: {formattedNumber}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write activity log for gatepass deletion");
                }

                _logger.LogInformation("Gatepass deleted successfully: {FormattedNumber}", formattedNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gatepass");
                throw;
            }
        }

        public async Task<GatepassResponseDto> SignGatepassAsync(int id, int userId)
        {
            var gatepass = await _context.Gatepasses
                .Include(g => g.Items)
                .FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new KeyNotFoundException($"Gatepass with ID {id} not found");

            if (gatepass.SignedByUserId != null)
                throw new InvalidOperationException("Gatepass sudah ditandatangani");

            // Generate verification token
            gatepass.SignedByUserId = userId;
            gatepass.SignedAt = DateTime.UtcNow;
            gatepass.VerificationToken = Guid.NewGuid().ToString("N"); // 32 char hex
            gatepass.Status = GatepassStatus.Sent; // Auto-update status to Sent
            gatepass.UpdatedBy = userId;
            gatepass.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log activity
            try
            {
                await _activityLog.LogAsync("Gatepass", id, "Sign", userId,
                    $"Signed gatepass: {gatepass.FormattedNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write activity log for gatepass signing");
            }

            _logger.LogInformation("Gatepass signed: {FormattedNumber} by user {UserId}", gatepass.FormattedNumber, userId);

            return await GetGatepassByIdAsync(id) ?? throw new Exception("Failed to retrieve signed gatepass");
        }

        public async Task<GatepassResponseDto?> GetGatepassByVerificationTokenAsync(string token)
        {
            var gatepass = await _context.Gatepasses
                .Include(g => g.Items)
                .Include(g => g.CreatedByUser)
                .Include(g => g.SignedByUser)
                .Where(g => g.VerificationToken == token)
                .Select(g => new GatepassResponseDto
                {
                    Id = g.Id,
                    FormattedNumber = g.FormattedNumber,
                    SequenceNumber = g.SequenceNumber,
                    Year = g.Year,
                    Month = g.Month,
                    Destination = g.Destination,
                    PicName = g.PicName,
                    PicContact = g.PicContact,
                    GatepassDate = g.GatepassDate,
                    Notes = g.Notes,
                    Status = g.Status.ToString(),
                    CreatedAt = g.CreatedAt,
                    SignedAt = g.SignedAt,
                    VerificationToken = g.VerificationToken,
                    SignedByUser = g.SignedByUser != null ? new UserInfoDto
                    {
                        UserId = g.SignedByUser.UserId,
                        Username = g.SignedByUser.Username,
                        FullName = g.SignedByUser.FullName,
                        Email = g.SignedByUser.Email,
                        PhotoUrl = g.SignedByUser.PhotoUrl,
                        EmployeeId = g.SignedByUser.EmployeeId,
                        Division = g.SignedByUser.Division
                    } : null,
                    Items = g.Items.Select(i => new GatepassItemResponseDto
                    {
                        Id = i.Id,
                        ItemName = i.ItemName,
                        Quantity = i.Quantity,
                        Unit = i.Unit,
                        Description = i.Description,
                        SerialNumber = i.SerialNumber
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return gatepass;
        }
    }
}

