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

        /// <summary>
        /// Cek apakah role ini adalah role non-admin (teknisi/workshop/field).
        /// Role Warehouse, Supervisor Warehouse, Supervisor, Helpdesk, dan Super Admin 
        /// dianggap sebagai admin yang bisa melihat semua data.
        /// </summary>
        private static bool IsFieldRole(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return false;
            // Jika rolenya teknisi (dari OperationalRoleNames) → pasti field role
            if (OperationalRoleNames.IsTechnicianRole(roleName)) return true;
            // Role admin/warehouse/supervisor → bukan field role
            var adminRoles = new[] { 
                OperationalRoleNames.Warehouse, 
                OperationalRoleNames.SupervisorWarehouse, 
                OperationalRoleNames.SupervisorMkn, 
                OperationalRoleNames.Helpdesk,
                "Super Admin", "Admin" 
            };
            return !adminRoles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<PagedResultDto<WarehousePartBorrowListDto>> GetAllAsync(
            WarehousePartBorrowQueryDto query, int currentUserId, string? roleName)
        {
            var q = _context.WarehousePartBorrows.AsNoTracking()
                .Where(b => b.IsActive)
                .Include(b => b.BorrowedBy)
                .Include(b => b.RelatedRepairJob)
                .Include(b => b.Items)
                .AsQueryable();

            if (IsFieldRole(roleName))
            {
                q = q.Where(b => b.BorrowedByUserId == currentUserId);
                // Teknisi hanya melihat data setelah WH menyerahkan barang (Issued/Returned)
                q = q.Where(b => b.Status == WarehousePartBorrowStatus.Issued
                               || b.Status == WarehousePartBorrowStatus.Returned);
            }

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
                    b.Items.Any(i => i.PartDescription.ToLower().Contains(s) || 
                                     (i.PartCode != null && i.PartCode.ToLower().Contains(s))));
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(b => b.RequestedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(b => new WarehousePartBorrowListDto
                {
                    Id = b.Id,
                    BorrowNumber = b.BorrowNumber,
                    Items = b.Items.Select(i => new WarehousePartBorrowItemDto { 
                        Id = i.Id, 
                        PartDescription = i.PartDescription, 
                        PartCode = i.PartCode, 
                        Quantity = i.Quantity 
                    }).ToList(),
                    Status = b.Status.ToString(),
                    BorrowedByName = b.BorrowedBy.FullName,
                    RequestedAt = b.RequestedAt,
                    RelatedJobNumber = b.RelatedRepairJob != null ? b.RelatedRepairJob.HelpdeskTicketNumber : null
                })
                .ToListAsync();

            return new PagedResultDto<WarehousePartBorrowListDto>(items, query, total);
        }

        public async Task<List<WarehousePartBorrowListDto>> GetPendingAsync() =>
            await _context.WarehousePartBorrows.AsNoTracking()
                .Where(b => b.IsActive)
                .Include(b => b.Items)
                .Include(b => b.BorrowedBy)
                .Include(b => b.RelatedRepairJob)
                .Where(b => b.Status == WarehousePartBorrowStatus.PendingApproval)
                .OrderBy(b => b.RequestedAt)
                .Select(b => new WarehousePartBorrowListDto
                {
                    Id = b.Id,
                    BorrowNumber = b.BorrowNumber,
                    Items = b.Items.Select(i => new WarehousePartBorrowItemDto { 
                        Id = i.Id, 
                        PartDescription = i.PartDescription, 
                        PartCode = i.PartCode, 
                        Quantity = i.Quantity 
                    }).ToList(),
                    Status = b.Status.ToString(),
                    BorrowedByName = b.BorrowedBy.FullName,
                    RequestedAt = b.RequestedAt,
                    RelatedJobNumber = b.RelatedRepairJob != null ? b.RelatedRepairJob.HelpdeskTicketNumber : null
                })
                .ToListAsync();

        public async Task<WarehousePartBorrowDetailDto?> GetByIdAsync(int id, int currentUserId, string? roleName)
        {
            var b = await _context.WarehousePartBorrows
                .Where(x => x.IsActive)
                .Include(x => x.Items)
                .Include(x => x.BorrowedBy)
                .Include(x => x.RelatedRepairJob)
                .Include(x => x.StatusLogs).ThenInclude(l => l.User)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return null;
            if (IsFieldRole(roleName) && b.BorrowedByUserId != currentUserId)
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
                Purpose = dto.Purpose?.Trim(),
                RelatedRepairJobId = dto.RelatedRepairJobId,
                TicketNumber = dto.TicketNumber?.Trim(),
                Status = WarehousePartBorrowStatus.PendingApproval,
                RequestedAt = now,
                CreatedAt = now,
                Items = dto.Items.Select(i => new WarehousePartBorrowItem {
                    PartDescription = i.PartDescription.Trim(),
                    PartCode = i.PartCode?.Trim(),
                    Quantity = i.Quantity
                }).ToList()
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

        public async Task<WarehousePartBorrowDetailDto> IssueAsync(int id, IssueBorrowDto dto, int userId)
        {
            var b = await GetBorrowTrackedAsync(id);
            if (b.Status != WarehousePartBorrowStatus.Approved)
                throw new InvalidOperationException("Part harus disetujui terlebih dahulu.");
            var from = b.Status;
            b.Status = WarehousePartBorrowStatus.Issued;
            b.IssuedByUserId = userId;
            b.IssuedAt = DateTime.UtcNow;
            b.IssuerSignatureBase64 = dto.IssuerSignatureBase64;
            b.ReceiverSignatureBase64 = dto.ReceiverSignatureBase64;
            b.UpdatedAt = DateTime.UtcNow;
            await AddLogAsync(b.Id, from, WarehousePartBorrowStatus.Issued, "Part diserahkan", userId);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(id, userId, null))!;
        }

        public async Task<WarehousePartBorrowDetailDto> ReturnAsync(int id, ReturnBorrowDto dto, int userId, string? roleName)
        {
            var b = await GetBorrowTrackedAsync(id);
            if (b.Status != WarehousePartBorrowStatus.Issued)
                throw new InvalidOperationException("Hanya peminjaman Issued yang dapat dikembalikan.");
            
            if (IsFieldRole(roleName) && b.BorrowedByUserId != userId)
                throw new UnauthorizedAccessException("Teknisi hanya dapat mengembalikan part yang mereka pinjam sendiri.");
                
            var from = b.Status;
            b.Status = WarehousePartBorrowStatus.Returned;
            b.ReturnedAt = DateTime.UtcNow;
            b.ReturnCondition = dto.ReturnCondition?.Trim();
            b.ReturnNote = dto.ReturnNote?.Trim();
            b.ReturnIssuerSignatureBase64 = dto.ReturnIssuerSignatureBase64;
            b.ReturnReceiverSignatureBase64 = dto.ReturnReceiverSignatureBase64;
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
            await _activityLog.LogAsync("WarehousePartBorrow", b.Id, "Cancel", userId, $"Batalkan {b.BorrowNumber}");
            await _context.SaveChangesAsync();
        }

        private async Task<Models.WarehousePartBorrow> GetBorrowTrackedAsync(int id) =>
            await _context.WarehousePartBorrows.FirstOrDefaultAsync(b => b.Id == id && b.IsActive)
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
            Items = b.Items.Select(i => new WarehousePartBorrowItemDto { 
                Id = i.Id, 
                PartDescription = i.PartDescription, 
                PartCode = i.PartCode, 
                Quantity = i.Quantity 
            }).ToList(),
            Status = b.Status.ToString(),
            BorrowedByName = b.BorrowedBy.FullName,
            RequestedAt = b.RequestedAt,
            RelatedJobNumber = b.RelatedRepairJob?.HelpdeskTicketNumber,
            TicketNumber = b.TicketNumber,
            Purpose = b.Purpose,
            RelatedRepairJobId = b.RelatedRepairJobId,
            ApprovalNote = b.ApprovalNote,
            RejectionReason = b.RejectionReason,
            ApprovedAt = b.ApprovedAt,
            IssuedAt = b.IssuedAt,
            ReturnedAt = b.ReturnedAt,
            ReturnCondition = b.ReturnCondition,
            ReturnNote = b.ReturnNote,
            IssuerSignatureBase64 = b.IssuerSignatureBase64,
            ReceiverSignatureBase64 = b.ReceiverSignatureBase64,
            ReturnIssuerSignatureBase64 = b.ReturnIssuerSignatureBase64,
            ReturnReceiverSignatureBase64 = b.ReturnReceiverSignatureBase64,
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

        public async Task DeleteAsync(int id)
        {
            var borrow = await _context.WarehousePartBorrows.FindAsync(id);
            if (borrow == null || !borrow.IsActive)
                throw new KeyNotFoundException("Peminjaman tidak ditemukan.");

            borrow.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}
