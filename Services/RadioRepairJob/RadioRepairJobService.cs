using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.RadioRepairJob;
using Pm.Enums;
using Pm.Models;
using Pm.Services;

namespace Pm.Services.RadioRepairJob
{
    public class RadioRepairJobService : IRadioRepairJobService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;

        public RadioRepairJobService(AppDbContext context, IActivityLogService activityLog)
        {
            _context = context;
            _activityLog = activityLog;
        }

        private IQueryable<Models.RadioRepairJob> BaseQuery() =>
            _context.RadioRepairJobs.AsNoTracking()
                .Include(j => j.AssignedTechnician)
                .Include(j => j.Radio);

        private static bool IsTechnician(string? roleName) =>
            Pm.Helper.OperationalRoleNames.IsTechnicianRole(roleName);

        public async Task<PagedResultDto<RadioRepairJobListDto>> GetAllAsync(
            RadioRepairJobQueryDto query, int currentUserId, string? roleName)
        {
            var q = BaseQuery();

            if (IsTechnician(roleName))
                q = q.Where(j => j.AssignedTechnicianUserId == currentUserId);

            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<RadioRepairJobStatus>(query.Status, true, out var st))
                q = q.Where(j => j.Status == st);

            if (query.TechnicianUserId.HasValue)
                q = q.Where(j => j.AssignedTechnicianUserId == query.TechnicianUserId);

            if (query.FromDate.HasValue)
                q = q.Where(j => j.OpenedAt >= query.FromDate.Value);
            if (query.ToDate.HasValue)
                q = q.Where(j => j.OpenedAt <= query.ToDate.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                q = q.Where(j =>
                    j.JobNumber.ToLower().Contains(s) ||
                    j.HelpdeskTicketNumber.ToLower().Contains(s) ||
                    j.RadioSerialNumber.ToLower().Contains(s) ||
                    j.DamageDescription.ToLower().Contains(s));
            }

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(j => j.OpenedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(j => new RadioRepairJobListDto
                {
                    Id = j.Id,
                    JobNumber = j.JobNumber,
                    HelpdeskTicketNumber = j.HelpdeskTicketNumber,
                    RadioSerialNumber = j.RadioSerialNumber,
                    RadioId = j.RadioId,
                    RadioCategory = j.Radio != null ? j.Radio.Category : null,
                    DamageDescription = j.DamageDescription,
                    Status = j.Status.ToString(),
                    AssignedTechnicianUserId = j.AssignedTechnicianUserId,
                    AssignedTechnicianName = j.AssignedTechnician.FullName,
                    OpenedAt = j.OpenedAt,
                    ClosedAt = j.ClosedAt
                })
                .ToListAsync();

            return new PagedResultDto<RadioRepairJobListDto>(items, query, total);
        }

        public async Task<RadioRepairDashboardDto> GetDashboardAsync(int currentUserId, string? roleName)
        {
            var q = _context.RadioRepairJobs.AsNoTracking();
            if (IsTechnician(roleName))
                q = q.Where(j => j.AssignedTechnicianUserId == currentUserId);

            var counts = await q.GroupBy(_ => 1).Select(g => new
            {
                Total = g.Count(),
                Received = g.Count(x => x.Status == RadioRepairJobStatus.Received),
                InProgress = g.Count(x => x.Status == RadioRepairJobStatus.InProgress),
                Monitoring = g.Count(x => x.Status == RadioRepairJobStatus.Monitoring),
                WaitingMaterialApproval = g.Count(x => x.Status == RadioRepairJobStatus.WaitingMaterialApproval),
                RepairCompleted = g.Count(x => x.Status == RadioRepairJobStatus.RepairCompleted),
                HandedToWarehouse = g.Count(x => x.Status == RadioRepairJobStatus.HandedToWarehouse),
                ReturnedToHelpdesk = g.Count(x => x.Status == RadioRepairJobStatus.ReturnedToHelpdesk),
                Cancelled = g.Count(x => x.Status == RadioRepairJobStatus.Cancelled)
            }).FirstOrDefaultAsync();

            return new RadioRepairDashboardDto
            {
                Total = counts?.Total ?? 0,
                Received = counts?.Received ?? 0,
                InProgress = counts?.InProgress ?? 0,
                Monitoring = counts?.Monitoring ?? 0,
                WaitingMaterialApproval = counts?.WaitingMaterialApproval ?? 0,
                RepairCompleted = counts?.RepairCompleted ?? 0,
                HandedToWarehouse = counts?.HandedToWarehouse ?? 0,
                ReturnedToHelpdesk = counts?.ReturnedToHelpdesk ?? 0,
                Cancelled = counts?.Cancelled ?? 0
            };
        }

        public async Task<RadioRepairJobDetailDto?> GetByIdAsync(int id, int currentUserId, string? roleName)
        {
            var job = await _context.RadioRepairJobs
                .Include(j => j.AssignedTechnician)
                .Include(j => j.OpenedBy)
                .Include(j => j.Radio)
                .Include(j => j.StatusLogs).ThenInclude(l => l.User)
                .Include(j => j.Handovers).ThenInclude(h => h.HandedOverBy)
                .Include(j => j.Handovers).ThenInclude(h => h.ReceivedBy)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return null;
            if (IsTechnician(roleName) && job.AssignedTechnicianUserId != currentUserId)
                throw new UnauthorizedAccessException("Anda tidak memiliki akses ke job ini.");

            return MapDetail(job);
        }

        public async Task<RadioRepairJobDetailDto> UpdateStatusAsync(
            int id, UpdateRadioRepairJobStatusDto dto, int userId, string? roleName)
        {
            var job = await _context.RadioRepairJobs.FirstOrDefaultAsync(j => j.Id == id)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (IsTechnician(roleName) && job.AssignedTechnicianUserId != userId)
                throw new UnauthorizedAccessException("Hanya teknisi penanggung job ini yang dapat mengubah status.");

            if (job.Status is RadioRepairJobStatus.HandedToWarehouse or RadioRepairJobStatus.ReturnedToHelpdesk)
                throw new InvalidOperationException("Job sudah diserahkan ke warehouse atau kembali ke helpdesk.");

            if (job.Status == RadioRepairJobStatus.Cancelled)
                throw new InvalidOperationException("Job sudah dibatalkan.");

            var from = job.Status;
            ValidateStatusTransition(from, dto.Status, isSupervisor: false);

            job.Status = dto.Status;
            job.UpdatedAt = DateTime.UtcNow;
            await AddStatusLogAsync(job.Id, from, dto.Status, dto.Note, userId);
            await WriteRepairHistoryAsync(job, from, dto.Status, dto.Note, userId);
            await _activityLog.LogAsync("RadioRepairJob", job.Id, "StatusChange", userId,
                $"Status {from} → {dto.Status}");

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(id, userId, roleName))!;
        }

        public async Task<RadioRepairJobDetailDto> ApproveMaterialAsync(int id, ApproveMaterialDto dto, int userId)
        {
            var job = await _context.RadioRepairJobs.FirstOrDefaultAsync(j => j.Id == id)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status != RadioRepairJobStatus.WaitingMaterialApproval)
                throw new InvalidOperationException("Job tidak dalam status menunggu persetujuan material.");

            if (dto.ResumeStatus != RadioRepairJobStatus.InProgress &&
                dto.ResumeStatus != RadioRepairJobStatus.Monitoring)
                throw new ArgumentException("Status lanjutan harus InProgress atau Monitoring.");

            var from = job.Status;
            job.Status = dto.ResumeStatus;
            job.UpdatedAt = DateTime.UtcNow;
            await AddStatusLogAsync(job.Id, from, dto.ResumeStatus, dto.Note ?? "Material disetujui", userId);
            await WriteRepairHistoryAsync(job, from, dto.ResumeStatus, dto.Note, userId);
            await _activityLog.LogAsync("RadioRepairJob", job.Id, "ApproveMaterial", userId, "Material disetujui");

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(id, userId, null))!;
        }

        public async Task CancelAsync(int id, int userId, string? roleName)
        {
            var job = await _context.RadioRepairJobs.FirstOrDefaultAsync(j => j.Id == id)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status is RadioRepairJobStatus.HandedToWarehouse or RadioRepairJobStatus.ReturnedToHelpdesk)
                throw new InvalidOperationException("Job yang sudah ke warehouse atau helpdesk tidak dapat dibatalkan.");

            var from = job.Status;
            job.Status = RadioRepairJobStatus.Cancelled;
            job.UpdatedAt = DateTime.UtcNow;
            await AddStatusLogAsync(job.Id, from, RadioRepairJobStatus.Cancelled, "Dibatalkan", userId);
            await _context.SaveChangesAsync();
        }

        private static void ValidateStatusTransition(
            RadioRepairJobStatus from, RadioRepairJobStatus to, bool isSupervisor)
        {
            if (to == RadioRepairJobStatus.Cancelled) return;
            var allowed = from switch
            {
                RadioRepairJobStatus.Received => new[] { RadioRepairJobStatus.InProgress, RadioRepairJobStatus.Cancelled },
                RadioRepairJobStatus.InProgress => new[] { RadioRepairJobStatus.Monitoring, RadioRepairJobStatus.WaitingMaterialApproval, RadioRepairJobStatus.RepairCompleted },
                RadioRepairJobStatus.Monitoring => new[] { RadioRepairJobStatus.InProgress, RadioRepairJobStatus.WaitingMaterialApproval, RadioRepairJobStatus.RepairCompleted },
                RadioRepairJobStatus.WaitingMaterialApproval => Array.Empty<RadioRepairJobStatus>(),
                RadioRepairJobStatus.RepairCompleted => Array.Empty<RadioRepairJobStatus>(),
                _ => Array.Empty<RadioRepairJobStatus>()
            };
            if (!allowed.Contains(to))
                throw new InvalidOperationException($"Transisi status dari {from} ke {to} tidak diizinkan.");
        }

        private async Task AddStatusLogAsync(int jobId, RadioRepairJobStatus? from, RadioRepairJobStatus to, string? note, int userId)
        {
            _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
            {
                JobId = jobId,
                FromStatus = from,
                ToStatus = to,
                Note = note,
                UserId = userId,
                At = DateTime.UtcNow
            });
        }

        private async Task WriteRepairHistoryAsync(
            Models.RadioRepairJob job, RadioRepairJobStatus from, RadioRepairJobStatus to, string? note, int userId)
        {
            if (!job.RadioId.HasValue) return;
            var tech = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == job.AssignedTechnicianUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
            _context.RadioHistories.Add(new RadioHistory
            {
                RadioId = job.RadioId.Value,
                Action = "RepairStatusChanged",
                Details = $"Job {job.JobNumber}: {from} → {to}. Teknisi: {tech}. {(note != null ? "Catatan: " + note : "")}",
                CreatedBy = await GetUserDisplayNameAsync(userId),
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task<string> GetUserDisplayNameAsync(int userId)
        {
            var u = await _context.Users.AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new { x.FullName, x.Username })
                .FirstOrDefaultAsync();
            if (u == null) return userId.ToString();
            return string.IsNullOrWhiteSpace(u.FullName) ? u.Username! : $"{u.FullName} ({u.Username})";
        }

        private static RadioRepairJobDetailDto MapDetail(Models.RadioRepairJob job) => new()
        {
            Id = job.Id,
            JobNumber = job.JobNumber,
            HelpdeskTicketNumber = job.HelpdeskTicketNumber,
            RadioSerialNumber = job.RadioSerialNumber,
            RadioId = job.RadioId,
            RadioCategory = job.Radio?.Category,
            BatterySerialNumber = job.BatterySerialNumber,
            DamageDescription = job.DamageDescription,
            Status = job.Status.ToString(),
            AssignedTechnicianUserId = job.AssignedTechnicianUserId,
            AssignedTechnicianName = job.AssignedTechnician.FullName,
            OpenedByName = job.OpenedBy.FullName,
            OpenedAt = job.OpenedAt,
            ClosedAt = job.ClosedAt,
            StatusLogs = job.StatusLogs.OrderByDescending(l => l.At).Select(l => new RadioRepairJobStatusLogDto
            {
                Id = l.Id,
                FromStatus = l.FromStatus?.ToString(),
                ToStatus = l.ToStatus.ToString(),
                Note = l.Note,
                UserName = l.User.FullName,
                At = l.At
            }).ToList(),
            Handovers = job.Handovers.OrderBy(h => h.HandoverAt).Select(h => new RadioRepairJobHandoverSummaryDto
            {
                Id = h.Id,
                HandoverNumber = h.HandoverNumber,
                HandoverType = h.HandoverType.ToString(),
                HandoverAt = h.HandoverAt,
                HandedOverByName = h.HandedOverBy.FullName,
                ReceivedByName = h.ReceivedBy.FullName,
                HasRadioPhoto = !string.IsNullOrEmpty(h.RadioPhotoBase64),
                HasHandedOverSignature = !string.IsNullOrEmpty(h.HandedOverSignatureBase64),
                HasReceiverSignature = !string.IsNullOrEmpty(h.ReceiverSignatureBase64)
            }).ToList()
        };
    }
}
