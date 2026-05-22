using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.WarehousePartBorrow;
using Pm.Enums;
using Pm.Helper;
using Pm.Models;
using Pm.Services;

namespace Pm.Services.WarehousePartBorrow
{
    public class WarehousePartBorrowService : IWarehousePartBorrowService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;

        public WarehousePartBorrowService(AppDbContext context, IActivityLogService activityLog)
        {
            _context = context;
            _activityLog = activityLog;
        }

        private static bool IsTechnician(string? roleName) =>
            string.Equals(roleName, "Teknisi", StringComparison.OrdinalIgnoreCase);

        public async Task<PagedResultDto<WarehousePartBorrowListDto>> GetAllAsync(
            WarehousePartBorrowQueryDto query, int currentUserId, string? roleName)
        {
            var q = _context.WarehousePartBorrows.AsNoTracking()
                .Include(b => b.BorrowedBy)
                .Include(b => b.RelatedRepairJob)
                .AsQueryable();

            if (IsTechnician(roleName))
                q = q.Where(b => b.BorrowedByUserId == currentUserId);

            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<WarehousePartBorrowStatus>(query.Status, true, out var st))
                q = q.Where(b => b.Status == st);

            if (query.BorrowedByUserId.HasValue)
                q = q.Where(b => b.BorrowedByUserId == query.BorrowedByUserId);

            if (query.FromDate.HasValue)
                q = q.Where(b => b.RequestedAt >= query.FromDate.Value);
            if (query.ToDate.HasValue)
                q = q.Where(b => b.RequestedAt <= query.ToDate.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(b =>
                    b.BorrowNumber.ToLower().Contains(s) ||
                    b.PartDescription.ToLower().Contains(s) ||
                    (b.PartCode != null && b.PartCode.ToLower().Contains(s)));
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(b => b.RequestedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(b => new WarehousePartBorrowListDto
                {
                    Id = b.Id,
                    BorrowNumber = b.BorrowNumber,
                    PartDescription = b.PartDescription,
                    PartCode = b.PartCode,
                    Quantity = b.Quantity,
                    Status = b.Status.ToString(),
                    BorrowedByName = b.BorrowedBy.FullName,
                    RequestedAt = b.RequestedAt,
                    RelatedJobNumber = b.RelatedRepairJob != null ? b.RelatedRepairJob.JobNumber : null
                })
                .ToListAsync();

            return new PagedResultDto<WarehousePartBorrowListDto>(items, query, total);
        }

        public async Task<List<WarehousePartBorrowListDto>> GetPendingAsync() =>
            await _context.WarehousePartBorrows.AsNoTracking()
                .Where(b => b.Status == WarehousePartBorrowStatus.PendingApproval)
                .OrderBy(b => b.RequestedAt)
                .Select(b => new WarehousePartBorrowListDto
                {
                    Id = b.Id,
                    BorrowNumber = b.BorrowNumber,
                    PartDescription = b.PartDescription,
                    PartCode = b.PartCode,
                    Quantity = b.Quantity,
                    Status = b.Status.ToString(),
                    BorrowedByName = b.BorrowedBy.FullName,
                    RequestedAt = b.RequestedAt,
                    RelatedJobNumber = b.RelatedRepairJob != null ? b.RelatedRepairJob.JobNumber : null
                })
                .ToListAsync();

        public async Task<WarehousePartBorrowDetailDto?> GetByIdAsync(int id, int currentUserId, string? roleName)
        {
            var b = await _context.WarehousePartBorrows
                .Include(x => x.BorrowedBy)
                .Include(x => x.RelatedRepairJob)
                .Include(x => x.StatusLogs).ThenInclude(l => l.User)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return null;
            if (IsTechnician(roleName) && b.BorrowedByUserId != currentUserId)
                throw new UnauthorizedAccessException("Akses ditolak.");
            return MapDetail(b);
        }

        public async Task<WarehousePartBorrowDetailDto> CreateAsync(CreateWarehousePartBorrowDto dto, int userId)
        {
            var number = await DocumentNumberHelper.NextBorrowNumberAsync(_context);
            var now = DateTime.UtcNow;
            var borrow = new Models.WarehousePartBorrow
            {
                BorrowNumber = number,
                BorrowedByUserId = userId,
                PartDescription = dto.PartDescription.Trim(),
                PartCode = dto.PartCode?.Trim(),
                Quantity = dto.Quantity,
                Purpose = dto.Purpose?.Trim(),
                RelatedRepairJobId = dto.RelatedRepairJobId,
                Status = WarehousePartBorrowStatus.PendingApproval,
                RequestedAt = now,
                CreatedAt = now
            };
            _context.WarehousePartBorrows.Add(borrow);
            await _context.SaveChangesAsync();
            await AddLogAsync(borrow.Id, null, WarehousePartBorrowStatus.PendingApproval, "Permintaan dibuat", userId);
            await _activityLog.LogAsync("WarehousePartBorrow", borrow.Id, "Create", userId, number);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(borrow.Id, userId, null))!;
        }

        public async Task<WarehousePartBorrowDetailDto> ApproveAsync(int id, ApproveBorrowDto dto, int userId)
        {
            var b = await GetBorrowTrackedAsync(id);
            if (b.Status != WarehousePartBorrowStatus.PendingApproval)
                throw new InvalidOperationException("Hanya permintaan pending yang dapat disetujui.");
            var from = b.Status;
            b.Status = WarehousePartBorrowStatus.Approved;
            b.ApprovedByUserId = userId;
            b.ApprovedAt = DateTime.UtcNow;
            b.ApprovalNote = dto.Note?.Trim();
            b.UpdatedAt = DateTime.UtcNow;
            await AddLogAsync(b.Id, from, WarehousePartBorrowStatus.Approved, dto.Note, userId);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(id, userId, null))!;
        }

        public async Task<WarehousePartBorrowDetailDto> RejectAsync(int id, RejectBorrowDto dto, int userId)
        {
            var b = await GetBorrowTrackedAsync(id);
            if (b.Status != WarehousePartBorrowStatus.PendingApproval)
                throw new InvalidOperationException("Hanya permintaan pending yang dapat ditolak.");
            var from = b.Status;
            b.Status = WarehousePartBorrowStatus.Rejected;
            b.RejectedByUserId = userId;
            b.RejectedAt = DateTime.UtcNow;
            b.RejectionReason = dto.Reason.Trim();
            b.UpdatedAt = DateTime.UtcNow;
            await AddLogAsync(b.Id, from, WarehousePartBorrowStatus.Rejected, dto.Reason, userId);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(id, userId, null))!;
        }

        public async Task<WarehousePartBorrowDetailDto> IssueAsync(int id, int userId)
        {
            var b = await GetBorrowTrackedAsync(id);
            if (b.Status != WarehousePartBorrowStatus.Approved)
                throw new InvalidOperationException("Part harus disetujui terlebih dahulu.");
            var from = b.Status;
            b.Status = WarehousePartBorrowStatus.Issued;
            b.IssuedByUserId = userId;
            b.IssuedAt = DateTime.UtcNow;
            b.UpdatedAt = DateTime.UtcNow;
            await AddLogAsync(b.Id, from, WarehousePartBorrowStatus.Issued, "Part diserahkan", userId);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(id, userId, null))!;
        }

        public async Task<WarehousePartBorrowDetailDto> ReturnAsync(int id, ReturnBorrowDto dto, int userId)
        {
            var b = await GetBorrowTrackedAsync(id);
            if (b.Status != WarehousePartBorrowStatus.Issued)
                throw new InvalidOperationException("Hanya peminjaman Issued yang dapat dikembalikan.");
            var from = b.Status;
            b.Status = WarehousePartBorrowStatus.Returned;
            b.ReturnedAt = DateTime.UtcNow;
            b.ReturnCondition = dto.ReturnCondition?.Trim();
            b.ReturnNote = dto.ReturnNote?.Trim();
            b.UpdatedAt = DateTime.UtcNow;
            await AddLogAsync(b.Id, from, WarehousePartBorrowStatus.Returned, dto.ReturnNote, userId);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(id, userId, null))!;
        }

        public async Task CancelAsync(int id, int userId)
        {
            var b = await GetBorrowTrackedAsync(id);
            if (b.Status != WarehousePartBorrowStatus.PendingApproval)
                throw new InvalidOperationException("Hanya permintaan pending yang dapat dibatalkan.");
            if (b.BorrowedByUserId != userId)
                throw new UnauthorizedAccessException("Hanya peminjam yang dapat membatalkan.");
            var from = b.Status;
            b.Status = WarehousePartBorrowStatus.Cancelled;
            b.UpdatedAt = DateTime.UtcNow;
            await AddLogAsync(b.Id, from, WarehousePartBorrowStatus.Cancelled, "Dibatalkan", userId);
            await _context.SaveChangesAsync();
        }

        private async Task<Models.WarehousePartBorrow> GetBorrowTrackedAsync(int id) =>
            await _context.WarehousePartBorrows.FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new KeyNotFoundException("Peminjaman tidak ditemukan.");

        private Task AddLogAsync(int borrowId, WarehousePartBorrowStatus? from,
            WarehousePartBorrowStatus to, string? note, int userId)
        {
            _context.WarehousePartBorrowStatusLogs.Add(new WarehousePartBorrowStatusLog
            {
                BorrowId = borrowId,
                FromStatus = from,
                ToStatus = to,
                Note = note,
                UserId = userId,
                At = DateTime.UtcNow
            });
            return Task.CompletedTask;
        }

        private static WarehousePartBorrowDetailDto MapDetail(Models.WarehousePartBorrow b) => new()
        {
            Id = b.Id,
            BorrowNumber = b.BorrowNumber,
            PartDescription = b.PartDescription,
            PartCode = b.PartCode,
            Quantity = b.Quantity,
            Status = b.Status.ToString(),
            BorrowedByName = b.BorrowedBy.FullName,
            RequestedAt = b.RequestedAt,
            RelatedJobNumber = b.RelatedRepairJob?.JobNumber,
            Purpose = b.Purpose,
            RelatedRepairJobId = b.RelatedRepairJobId,
            ApprovalNote = b.ApprovalNote,
            RejectionReason = b.RejectionReason,
            ApprovedAt = b.ApprovedAt,
            IssuedAt = b.IssuedAt,
            ReturnedAt = b.ReturnedAt,
            ReturnCondition = b.ReturnCondition,
            ReturnNote = b.ReturnNote,
            StatusLogs = b.StatusLogs.OrderByDescending(l => l.At).Select(l => new WarehousePartBorrowStatusLogDto
            {
                Id = l.Id,
                FromStatus = l.FromStatus?.ToString(),
                ToStatus = l.ToStatus.ToString(),
                Note = l.Note,
                UserName = l.User.FullName,
                At = l.At
            }).ToList()
        };
    }
}
