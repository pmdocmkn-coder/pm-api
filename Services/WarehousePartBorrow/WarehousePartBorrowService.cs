using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.WarehousePartBorrow;
using Pm.Enums;
using Pm.Helper;
using Pm.Models;
using Pm.Services;
using Pm.Services.Notification;
using Pm.DTOs.Notification;

namespace Pm.Services.WarehousePartBorrow
{
    public class WarehousePartBorrowService : IWarehousePartBorrowService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly INotificationService _notificationService;

        public WarehousePartBorrowService(AppDbContext context, IActivityLogService activityLog, INotificationService notificationService)
        {
            _context = context;
            _activityLog = activityLog;
            _notificationService = notificationService;
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
                OperationalRoleNames.SupervisorWorkshop, 
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
                    IssuedAt = b.IssuedAt,
                    RelatedJobNumber = b.RelatedRepairJob != null ? b.RelatedRepairJob.HelpdeskTicketNumber : null,
                    TicketNumber = b.TicketNumber,
                    BorrowerName = b.BorrowerName,
                    Purpose = b.Purpose
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
                    IssuedAt = b.IssuedAt,
                    RelatedJobNumber = b.RelatedRepairJob != null ? b.RelatedRepairJob.HelpdeskTicketNumber : null,
                    TicketNumber = b.TicketNumber,
                    BorrowerName = b.BorrowerName,
                    Purpose = b.Purpose
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
                BorrowerName = dto.BorrowerName?.Trim(),
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
            await _notificationService.BroadcastRefreshDataAsync("WarehouseBorrow");

            // Ambil info peminjam dan rolenya untuk personalisasi notif
            var borrower = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            var borrowerRoleName = borrower?.Role?.RoleName ?? "";
            var borrowerDisplayName = dto.BorrowerName?.Trim() ?? borrower?.FullName ?? "Teknisi";
            var isTechnician = OperationalRoleNames.IsTechnicianRole(borrowerRoleName);

            // Notif konfirmasi ke peminjam — pengajuan berhasil dibuat
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                RecipientUserId = userId,
                Title = "Pengajuan Dikirim ✓",
                Message = $"Pengajuan peminjaman part ({number}) berhasil dikirim dan menunggu persetujuan.",
                Category = "Warehouse",
                LinkUrl = "/warehouse/borrow-history",
                ReferenceId = borrow.Id,
                ReferenceType = "WarehouseBorrow"
            });

            // Notifikasi ke Supervisor Warehouse & Warehouse untuk persetujuan
            var pendingNotifDto = new CreateNotificationDto
            {
                Title = "Permintaan Part Baru",
                Message = $"Permintaan peminjaman part ({number}) dari {borrowerDisplayName} membutuhkan persetujuan.",
                Category = "Warehouse",
                LinkUrl = "/warehouse/supervision",
                ReferenceId = borrow.Id,
                ReferenceType = "WarehouseBorrow"
            };
            await _notificationService.CreateForRoleAsync(OperationalRoleNames.SupervisorWarehouse, pendingNotifDto);
            await _notificationService.CreateForRoleAsync(OperationalRoleNames.Warehouse, pendingNotifDto);

            // Notif ke Supv MKN hanya jika peminjam adalah Teknisi WSK
            if (isTechnician)
            {
                await _notificationService.CreateForPermissionAsync(NotificationPermissions.WarehouseBorrow, new CreateNotificationDto
                {
                    Title = "Teknisi Ajukan Peminjaman Part",
                    Message = $"Teknisi {borrowerDisplayName} mengajukan peminjaman part ({number}). Menunggu persetujuan Supervisor Warehouse.",
                    Category = "Warehouse",
                    LinkUrl = "/warehouse/supervision",
                    ReferenceId = borrow.Id,
                    ReferenceType = "WarehouseBorrow"
                });
            }

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
            await _notificationService.BroadcastRefreshDataAsync("WarehouseBorrow");

            // Notifikasi ke peminjam
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                RecipientUserId = b.BorrowedByUserId,
                Title = "Peminjaman Disetujui ✓",
                Message = $"Permintaan part ({b.BorrowNumber}) telah disetujui. Silakan ambil di Warehouse.",
                Category = "Warehouse",
                LinkUrl = "/warehouse/borrow-history",
                ReferenceId = b.Id,
                ReferenceType = "WarehouseBorrow"
            });

            // Notifikasi ke Warehouse agar segera menyiapkan barang
            await _notificationService.CreateForRoleAsync(OperationalRoleNames.Warehouse, new CreateNotificationDto
            {
                Title = "Part Siap Diserahkan",
                Message = $"Peminjaman ({b.BorrowNumber}) telah disetujui. Silakan siapkan part untuk diserahkan.",
                Category = "Warehouse",
                LinkUrl = "/warehouse/supervision",
                ReferenceId = b.Id,
                ReferenceType = "WarehouseBorrow"
            });

            // Notif ke Supv MKN jika peminjam adalah Teknisi WSK
            var borrowerForApprove = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == b.BorrowedByUserId);
            if (OperationalRoleNames.IsTechnicianRole(borrowerForApprove?.Role?.RoleName))
            {
                await _notificationService.CreateForPermissionAsync(NotificationPermissions.WarehouseBorrow, new CreateNotificationDto
                {
                    Title = "Peminjaman Part Teknisi Disetujui",
                    Message = $"Peminjaman part ({b.BorrowNumber}) oleh {b.BorrowerName ?? borrowerForApprove?.FullName ?? "teknisi"} telah disetujui.",
                    Category = "Warehouse",
                    LinkUrl = "/warehouse/supervision",
                    ReferenceId = b.Id,
                    ReferenceType = "WarehouseBorrow"
                });
            }

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
            await _notificationService.BroadcastRefreshDataAsync("WarehouseBorrow");

            // Notifikasi ke peminjam
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                RecipientUserId = b.BorrowedByUserId,
                Title = "Peminjaman Ditolak ✕",
                Message = $"Permintaan part ({b.BorrowNumber}) ditolak: {dto.Reason}",
                Category = "Warehouse",
                LinkUrl = "/warehouse/borrow-history",
                ReferenceId = b.Id,
                ReferenceType = "WarehouseBorrow"
            });

            // Notifikasi ke Supv MKN
            await _notificationService.CreateForPermissionAsync(NotificationPermissions.WarehouseBorrow, new CreateNotificationDto
            {
                Title = "Peminjaman Part Ditolak",
                Message = $"Permintaan part ({b.BorrowNumber}) ditolak: {dto.Reason}",
                Category = "Warehouse",
                LinkUrl = "/warehouse/supervision",
                ReferenceId = b.Id,
                ReferenceType = "WarehouseBorrow"
            });

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
            await _notificationService.BroadcastRefreshDataAsync("WarehouseBorrow");

            // Notifikasi ke peminjam
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                RecipientUserId = b.BorrowedByUserId,
                Title = "Part Diserahkan 📦",
                Message = $"Part untuk peminjaman ({b.BorrowNumber}) telah diserahkan. Harap kembalikan setelah selesai.",
                Category = "Warehouse",
                LinkUrl = "/warehouse/borrow-history",
                ReferenceId = b.Id,
                ReferenceType = "WarehouseBorrow"
            });

            // Notifikasi ke Supervisor Warehouse & Supv MKN
            var issueNotifDto = new CreateNotificationDto
            {
                Title = "Part Diserahkan ke Teknisi",
                Message = $"Part ({b.BorrowNumber}) telah diserahkan ke {(b.BorrowerName ?? b.BorrowedBy?.FullName ?? "teknisi")}.",
                Category = "Warehouse",
                LinkUrl = "/warehouse/supervision",
                ReferenceId = b.Id,
                ReferenceType = "WarehouseBorrow"
            };
            await _notificationService.CreateForRoleAsync(OperationalRoleNames.SupervisorWarehouse, issueNotifDto);
            await _notificationService.CreateForPermissionAsync(NotificationPermissions.WarehouseBorrow, issueNotifDto);

            return (await GetByIdAsync(id, userId, null))!;
        }

        public async Task<WarehousePartBorrowDetailDto> SignReceiverAsync(int id, SignReceiverBorrowDto dto, int userId)
        {
            var b = await GetBorrowTrackedAsync(id);
            if (b.Status != WarehousePartBorrowStatus.Issued)
                throw new InvalidOperationException("Hanya dapat menandatangani peminjaman yang sudah diserahkan (Issued).");
            
            b.ReceiverSignatureBase64 = dto.ReceiverSignatureBase64;
            b.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("WarehouseBorrow");

            return (await GetByIdAsync(id, userId, null))!;
        }

        public async Task<WarehousePartBorrowDetailDto> ReturnAsync(int id, ReturnBorrowDto dto, int userId, string? roleName)
        {
            var b = await GetBorrowTrackedAsync(id);
            if (b.Status != WarehousePartBorrowStatus.Issued)
                throw new InvalidOperationException("Hanya peminjaman Issued yang dapat dikembalikan.");
            
            // Catatan: Teknisi lain diperbolehkan mengembalikan part atas nama peminjam asli.
            // Siapa yang mengembalikan tercatat di status log (userId).
                
            var from = b.Status;
            b.Status = WarehousePartBorrowStatus.Returned;
            b.ReturnedAt = DateTime.UtcNow;
            b.ReturnCondition = dto.ReturnCondition?.Trim();
            b.ReturnNote = dto.ReturnNote?.Trim();
            b.ReturnedByName = dto.ReturnedByName?.Trim();
            b.ReturnIssuerSignatureBase64 = dto.ReturnIssuerSignatureBase64;
            b.ReturnReceiverSignatureBase64 = dto.ReturnReceiverSignatureBase64;
            b.UpdatedAt = DateTime.UtcNow;
            await AddLogAsync(b.Id, from, WarehousePartBorrowStatus.Returned, dto.ReturnNote, userId);
            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("WarehouseBorrow");

            // Notifikasi ke Warehouse, Supervisor Warehouse, Supv MKN, Admin
            var notifDto = new CreateNotificationDto
            {
                Title = "Part Dikembalikan ↩️",
                Message = $"Part ({b.BorrowNumber}) telah dikembalikan oleh {(dto.ReturnedByName?.Trim() ?? "teknisi")}.",
                Category = "Warehouse",
                LinkUrl = "/warehouse/supervision",
                ReferenceId = b.Id,
                ReferenceType = "WarehouseBorrow"
            };
            await _notificationService.CreateForRoleAsync(OperationalRoleNames.SupervisorWarehouse, notifDto);
            await _notificationService.CreateForRoleAsync(OperationalRoleNames.Warehouse, notifDto);
            await _notificationService.CreateForPermissionAsync(NotificationPermissions.WarehouseBorrow, notifDto);

            // Notifikasi ke peminjam (akun yang membuat peminjaman)
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                RecipientUserId = b.BorrowedByUserId,
                Title = "Part Berhasil Dikembalikan ✓",
                Message = $"Part ({b.BorrowNumber}) telah diterima kembali oleh Warehouse.",
                Category = "Warehouse",
                LinkUrl = "/warehouse/borrow-history",
                ReferenceId = b.Id,
                ReferenceType = "WarehouseBorrow"
            });

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
            await _notificationService.BroadcastRefreshDataAsync("WarehouseBorrow");

            var notifDto = new CreateNotificationDto
            {
                Title = "Peminjaman Dibatalkan",
                Message = $"Permintaan part ({b.BorrowNumber}) dibatalkan oleh peminjam.",
                Category = "Warehouse",
                LinkUrl = "/warehouse/supervision",
                ReferenceId = b.Id,
                ReferenceType = "WarehouseBorrow"
            };
            await _notificationService.CreateForRoleAsync(OperationalRoleNames.SupervisorWarehouse, notifDto);
            await _notificationService.CreateForRoleAsync(OperationalRoleNames.Warehouse, notifDto);
            await _notificationService.CreateForPermissionAsync(NotificationPermissions.WarehouseBorrow, notifDto);
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
            BorrowerName = b.BorrowerName,
            Purpose = b.Purpose,
            RelatedRepairJobId = b.RelatedRepairJobId,
            ApprovalNote = b.ApprovalNote,
            RejectionReason = b.RejectionReason,
            ApprovedAt = b.ApprovedAt,
            IssuedAt = b.IssuedAt,
            ReturnedAt = b.ReturnedAt,
            ReturnCondition = b.ReturnCondition,
            ReturnNote = b.ReturnNote,
            ReturnedByName = b.ReturnedByName,
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
            await _notificationService.BroadcastRefreshDataAsync("WarehouseBorrow");
        }
    }
}
