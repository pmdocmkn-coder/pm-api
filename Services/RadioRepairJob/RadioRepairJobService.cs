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
                .Include(j => j.Radio)
                .Include(j => j.CustomStatus)
                .Where(j => !(j.Status == RadioRepairJobStatus.Received && 
                              j.Handovers.Any(h => h.Id == j.CurrentHandoverId && h.Status != "Completed")));

        private static bool IsTechnician(string? roleName) =>
            Pm.Helper.OperationalRoleNames.IsTechnicianRole(roleName);

        private static IQueryable<Models.RadioRepairJob> ApplyDeletedFilter(
            IQueryable<Models.RadioRepairJob> q, bool includeDeleted) =>
            includeDeleted ? q.Where(j => j.IsDeleted) : q.Where(j => !j.IsDeleted);

        public async Task<PagedResultDto<RadioRepairJobListDto>> GetAllAsync(
            RadioRepairJobQueryDto query, int currentUserId, string? roleName)
        {
            var q = ApplyDeletedFilter(BaseQuery(), query.IncludeDeleted);

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
                    j.HelpdeskTicketNumber.ToLower().Contains(s) ||
                    j.RadioSerialNumber.ToLower().Contains(s) ||
                    j.DamageDescription.ToLower().Contains(s) ||
                    (j.EquipmentName != null && j.EquipmentName.ToLower().Contains(s)) ||
                    (j.Radio != null && j.Radio.RadioId != null && j.Radio.RadioId.ToLower().Contains(s)) ||
                    (j.Radio != null && j.Radio.Fleet != null && j.Radio.Fleet.ToLower().Contains(s)) ||
                    (j.Radio != null && j.Radio.Type != null && j.Radio.Type.ToLower().Contains(s)) ||
                    _context.Radios.Any(r =>
                        r.SerialNumber != null &&
                        r.SerialNumber.ToLower() == j.RadioSerialNumber.ToLower() &&
                        ((r.RadioId != null && r.RadioId.ToLower().Contains(s)) ||
                         (r.Fleet != null && r.Fleet.ToLower().Contains(s)) ||
                         (r.Type != null && r.Type.ToLower().Contains(s)))));
            }

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(j => j.OpenedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(j => new RadioRepairJobListDto
                {
                    Id = j.Id,
                    HelpdeskTicketNumber = j.HelpdeskTicketNumber,
                    RadioSerialNumber = j.RadioSerialNumber,
                    RadioId = j.RadioId,
                    RadioMasterRadioId = j.Radio != null ? j.Radio.RadioId : null,
                    RadioFleet = j.Radio != null ? j.Radio.Fleet : null,
                    RadioCategory = j.Radio != null ? j.Radio.Category : null,
                    EquipmentName = j.EquipmentName ?? (j.Radio != null ? j.Radio.Type : null),
                    PreviewPhotoBase64 = j.Handovers
                        .Where(h => h.HandoverType == RadioHandoverType.HelpdeskToTechnician && !h.IsDeleted)
                        .OrderBy(h => h.HandoverAt)
                        .Select(h => h.RadioPhotoBase64)
                        .FirstOrDefault(),
                    EquipmentTagType = j.EquipmentTagType != null ? j.EquipmentTagType.ToString() : null,
                    OriginFrom = j.OriginFrom,
                    RepairDataDescription = j.RepairDataDescription,
                    RepairedByName = j.RepairedByName,
                    FrequencyError = j.FrequencyError,
                    AfReading = j.AfReading,
                    PowerReading = j.PowerReading,
                    VoltageOutNoLoad = j.VoltageOutNoLoad,
                    VoltageOutWithLoad = j.VoltageOutWithLoad,
                    PhysicalCondition = j.PhysicalCondition,
                    DisplayCondition = j.DisplayCondition,
                    DamageDescription = j.DamageDescription,
                    Status = j.Status.ToString(),
                    AssignedTechnicianUserId = j.AssignedTechnicianUserId,
                    AssignedTechnicianName = j.AssignedTechnician.FullName,
                    CustomStatusId = j.CustomStatusId,
                    CustomStatusLabel = j.CustomStatus != null ? j.CustomStatus.Label : null,
                    CustomStatusColor = j.CustomStatus != null ? j.CustomStatus.Color : null,
                    OpenedAt = j.OpenedAt,
                    ClosedAt = j.ClosedAt,
                    IsDeleted = j.IsDeleted,
                    DeletedAt = j.DeletedAt
                })
                .ToListAsync();

            await EnrichRadioMasterFieldsAsync(items);

            return new PagedResultDto<RadioRepairJobListDto>(items, query, total);
        }

        public async Task<List<RadioRepairTicketGroupDto>> GetGroupedByTicketAsync(
            RadioRepairJobQueryDto query, int currentUserId, string? roleName, bool includeDeleted)
        {
            query.IncludeDeleted = includeDeleted;
            query.Page = 1;
            query.PageSize = 500;
            var paged = await GetAllAsync(query, currentUserId, roleName);
            return paged.Data
                .GroupBy(j => j.HelpdeskTicketNumber)
                .OrderByDescending(g => g.Max(x => x.OpenedAt))
                .Select(g => new RadioRepairTicketGroupDto
                {
                    HelpdeskTicketNumber = g.Key,
                    RadioCount = g.Count(),
                    Radios = g.OrderByDescending(x => x.OpenedAt).ToList()
                })
                .ToList();
        }

        public async Task<RadioRepairDashboardDto> GetDashboardAsync(int currentUserId, string? roleName)
        {
            var q = _context.RadioRepairJobs.AsNoTracking().Where(j => !j.IsDeleted);

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
            }).OrderBy(_ => 1).FirstOrDefaultAsync();

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
                .Include(j => j.CustomStatus)
                .Include(j => j.StatusLogs).ThenInclude(l => l.User)
                .Include(j => j.Handovers).ThenInclude(h => h.HandedOverBy)
                .Include(j => j.Handovers).ThenInclude(h => h.ReceivedBy)
                .Include(j => j.Handovers).ThenInclude(h => h.Accessories)
                .AsSplitQuery()
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return null;

            var dto = MapDetail(job, includeDeletedHandovers: false);
            await EnrichRadioMasterFieldsAsync(new List<RadioRepairJobListDto> { dto });
            return dto;
        }

        public async Task<RadioRepairJobDetailDto> UpdateStatusAsync(
            int id, UpdateRadioRepairJobStatusDto dto, int userId, string? roleName)
        {
            var job = await _context.RadioRepairJobs.FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status is RadioRepairJobStatus.HandedToWarehouse or RadioRepairJobStatus.ReturnedToHelpdesk)
                throw new InvalidOperationException("Job sudah diserahkan ke warehouse atau kembali ke helpdesk.");

            if (job.Status == RadioRepairJobStatus.Cancelled)
                throw new InvalidOperationException("Job sudah dibatalkan.");

            // Handle custom status — job tetap InProgress di enum, tapi punya label custom
            if (dto.CustomStatusId.HasValue)
            {
                var customStatus = await _context.RepairJobCustomStatuses
                    .FirstOrDefaultAsync(s => s.Id == dto.CustomStatusId && s.IsActive)
                    ?? throw new KeyNotFoundException("Status custom tidak ditemukan atau tidak aktif.");

                var fromStatus = job.Status;
                var fromCustomLabel = job.CustomStatusId.HasValue
                    ? (await _context.RepairJobCustomStatuses.AsNoTracking()
                        .Where(s => s.Id == job.CustomStatusId)
                        .Select(s => s.Label)
                        .FirstOrDefaultAsync() ?? job.CustomStatusId.ToString())
                    : fromStatus.ToString();

                job.Status = RadioRepairJobStatus.InProgress;
                job.CustomStatusId = dto.CustomStatusId;
                job.UpdatedAt = DateTime.UtcNow;

                var note = dto.Note ?? $"Status custom: {customStatus.Label}";
                _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
                {
                    JobId = job.Id,
                    FromStatus = fromStatus,
                    ToStatus = RadioRepairJobStatus.InProgress,
                    Note = $"[Custom] {fromCustomLabel} → {customStatus.Label}. {note}",
                    UserId = userId,
                    At = DateTime.UtcNow
                });

                await _activityLog.LogAsync("RadioRepairJob", job.Id, "CustomStatusChange", userId,
                    $"Custom status → {customStatus.Label}");
                await _context.SaveChangesAsync();
                return (await GetByIdAsync(id, userId, roleName))!;
            }

            // Clear custom status jika ada
            job.CustomStatusId = null;

            var from = job.Status;
            var isSupervisor = string.Equals(roleName, Pm.Helper.OperationalRoleNames.SupervisorMkn,
                StringComparison.OrdinalIgnoreCase);
            ValidateStatusTransition(from, dto.Status, isSupervisor);

            job.Status = dto.Status;
            job.UpdatedAt = DateTime.UtcNow;

            var statusNote = dto.Note;
            if (isSupervisor && from != dto.Status)
                statusNote = string.IsNullOrWhiteSpace(statusNote)
                    ? $"[Override supervisor] {from} → {dto.Status}"
                    : $"[Override supervisor] {statusNote}";

            await AddStatusLogAsync(job.Id, from, dto.Status, statusNote, userId);
            await WriteRepairHistoryAsync(job, from, dto.Status, statusNote, userId);
            await _activityLog.LogAsync("RadioRepairJob", job.Id, "StatusChange", userId,
                $"Status {from} → {dto.Status}{(isSupervisor ? " (supervisor override)" : "")}");

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(id, userId, roleName))!;
        }

        public async Task<RadioRepairJobDetailDto> ApproveMaterialAsync(int id, ApproveMaterialDto dto, int userId)
        {
            var job = await _context.RadioRepairJobs.FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted)
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

        public async Task<RadioRepairJobDetailDto> UpdateAsync(int id, UpdateRadioRepairJobDto dto, int userId)
        {
            var job = await _context.RadioRepairJobs
                .Include(j => j.AssignedTechnician)
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status is RadioRepairJobStatus.HandedToWarehouse or RadioRepairJobStatus.ReturnedToHelpdesk)
                throw new InvalidOperationException("Job yang sudah ke warehouse atau kembali ke helpdesk tidak dapat diedit.");

            await ValidateDuplicateTicketSerialAsync(
                dto.HelpdeskTicketNumber.Trim(),
                dto.RadioSerialNumber.Trim(),
                excludeJobId: id);

            if (dto.RadioId.HasValue)
                await ValidateRadioSerialLinkAsync(dto.RadioId, dto.RadioSerialNumber);

            var tech = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == dto.AssignedTechnicianUserId && u.IsActive)
                ?? throw new ArgumentException("Teknisi tidak valid.");

            // Kumpulkan perubahan sebelum apply
            var changes = new List<string>();
            if (!string.Equals(job.HelpdeskTicketNumber, dto.HelpdeskTicketNumber.Trim()))
                changes.Add($"Tiket: \"{job.HelpdeskTicketNumber}\" → \"{dto.HelpdeskTicketNumber.Trim()}\"");
            if (!string.Equals(job.RadioSerialNumber, dto.RadioSerialNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                changes.Add($"SN: \"{job.RadioSerialNumber}\" → \"{dto.RadioSerialNumber.Trim()}\"");
            if (!string.Equals(job.DamageDescription, dto.DamageDescription.Trim()))
                changes.Add($"Kerusakan: \"{job.DamageDescription}\" → \"{dto.DamageDescription.Trim()}\"");
            if (job.AssignedTechnicianUserId != dto.AssignedTechnicianUserId)
                changes.Add($"Teknisi: \"{job.AssignedTechnician?.FullName ?? job.AssignedTechnicianUserId.ToString()}\" → \"{tech.FullName}\"");

            job.HelpdeskTicketNumber = dto.HelpdeskTicketNumber.Trim();
            job.RadioSerialNumber = dto.RadioSerialNumber.Trim();
            job.BatterySerialNumber = dto.BatterySerialNumber?.Trim();
            job.DamageDescription = dto.DamageDescription.Trim();
            job.AssignedTechnicianUserId = dto.AssignedTechnicianUserId;
            job.RadioId = dto.RadioId;
            if (dto.EquipmentName != null)
                job.EquipmentName = string.IsNullOrWhiteSpace(dto.EquipmentName) ? null : dto.EquipmentName.Trim();
            if (dto.UnitNumber != null)
                job.UnitNumber = string.IsNullOrWhiteSpace(dto.UnitNumber) ? null : dto.UnitNumber.Trim();
            if (dto.RadioOwnerLabel != null)
                job.RadioOwnerLabel = string.IsNullOrWhiteSpace(dto.RadioOwnerLabel) ? null : dto.RadioOwnerLabel.Trim();
            if (dto.OwnerDivision != null)
                job.OwnerDivision = string.IsNullOrWhiteSpace(dto.OwnerDivision) ? null : dto.OwnerDivision.Trim();
            if (dto.OwnerDepartment != null)
                job.OwnerDepartment = string.IsNullOrWhiteSpace(dto.OwnerDepartment) ? null : dto.OwnerDepartment.Trim();
            job.UpdatedAt = DateTime.UtcNow;

            // Catat di StatusLogs agar muncul di timeline detail panel
            if (changes.Count > 0)
            {
                _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
                {
                    JobId = job.Id,
                    FromStatus = job.Status,
                    ToStatus = job.Status,
                    Note = $"[Edit supervisor] {string.Join("; ", changes)}",
                    UserId = userId,
                    At = DateTime.UtcNow
                });
            }

            await _activityLog.LogAsync("RadioRepairJob", job.Id, "Update", userId,
                $"Edit tiket {job.HelpdeskTicketNumber}, SN {job.RadioSerialNumber}, teknisi {tech.FullName}");

            // Tulis ke RadioHistories agar muncul di modal Riwayat Perubahan Radio
            await WriteEditHistoryAsync(job, changes, $"supervisor ({tech.FullName})", userId);

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(id, userId, null))!;
        }

        /// <summary>
        /// Update oleh teknisi — hanya boleh ubah keterangan kerusakan.
        /// Setiap perubahan dicatat di StatusLogs dengan action "Edit" agar terlihat siapa yang mengubah apa.
        /// </summary>
        public async Task<RadioRepairJobDetailDto> TechnicianUpdateAsync(
            int id, TechnicianUpdateRepairJobDto dto, int userId)
        {
            var job = await _context.RadioRepairJobs.FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status is RadioRepairJobStatus.HandedToWarehouse or RadioRepairJobStatus.ReturnedToHelpdesk)
                throw new InvalidOperationException("Job yang sudah ke warehouse atau helpdesk tidak dapat diedit.");

            var newDamage = dto.DamageDescription?.Trim() ?? "";
            var changes = new List<string>();

            if (dto.EquipmentTagType.HasValue && job.EquipmentTagType != dto.EquipmentTagType.Value)
            {
                changes.Add($"Tag Fisik: \"{job.EquipmentTagType}\" → \"{dto.EquipmentTagType.Value}\"");
                job.EquipmentTagType = dto.EquipmentTagType.Value;
            }

            if (!string.Equals(job.DamageDescription, newDamage, StringComparison.Ordinal))
            {
                changes.Add($"Kerusakan: \"{job.DamageDescription}\" → \"{newDamage}\"");
                job.DamageDescription = newDamage;
            }
            
            // Map Green tag fields
            if (dto.EquipmentTagType == EquipmentTagType.Good)
            {
                job.OriginFrom = dto.OriginFrom;
                job.RepairDataDescription = dto.RepairDataDescription;
                job.RepairedByName = dto.RepairedByName;
                job.FrequencyError = dto.FrequencyError;
                job.AfReading = dto.AfReading;
                job.PowerReading = dto.PowerReading;
                job.VoltageOutNoLoad = dto.VoltageOutNoLoad;
                job.VoltageOutWithLoad = dto.VoltageOutWithLoad;
                job.PhysicalCondition = dto.PhysicalCondition;
                job.DisplayCondition = dto.DisplayCondition;
            }
            else
            {
                // Jika kuning, bersihkan data hijau
                job.OriginFrom = null;
                job.RepairDataDescription = null;
                job.RepairedByName = null;
                job.FrequencyError = null;
                job.AfReading = null;
                job.PowerReading = null;
                job.VoltageOutNoLoad = null;
                job.VoltageOutWithLoad = null;
                job.PhysicalCondition = null;
                job.DisplayCondition = null;
            }

            if (changes.Count == 0 && dto.EquipmentTagType == EquipmentTagType.Good)
            {
                changes.Add("Data perbaikan diupdate");
            }

            if (changes.Count == 0)
                return (await GetByIdAsync(id, userId, null))!;

            job.UpdatedAt = DateTime.UtcNow;

            // Catat di StatusLogs agar muncul di timeline — siapa yang ubah apa
            _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
            {
                JobId = job.Id,
                FromStatus = job.Status,
                ToStatus = job.Status,
                Note = $"[Edit oleh teknisi] {string.Join("; ", changes)}",
                UserId = userId,
                At = DateTime.UtcNow
            });

            await _activityLog.LogAsync("RadioRepairJob", job.Id, "TechnicianEdit", userId,
                string.Join("; ", changes));

            // Tulis ke RadioHistories agar muncul di modal Riwayat Perubahan Radio
            await WriteEditHistoryAsync(job, changes, "teknisi", userId);

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(id, userId, null))!;
        }

        public async Task SoftDeleteAsync(int id, int userId)
        {
            var job = await _context.RadioRepairJobs
                .Include(j => j.Handovers)
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status is RadioRepairJobStatus.HandedToWarehouse or RadioRepairJobStatus.ReturnedToHelpdesk)
                throw new InvalidOperationException("Job yang sudah ke warehouse atau helpdesk tidak dapat dihapus.");

            var now = DateTime.UtcNow;
            job.IsDeleted = true;
            job.DeletedAt = now;
            job.DeletedByUserId = userId;
            job.UpdatedAt = now;

            foreach (var h in job.Handovers.Where(x => !x.IsDeleted))
            {
                h.IsDeleted = true;
                h.DeletedAt = now;
                h.DeletedByUserId = userId;
                h.UpdatedAt = now;
            }

            await _activityLog.LogAsync("RadioRepairJob", job.Id, "SoftDelete", userId,
                $"Arsip pekerjaan tiket {job.HelpdeskTicketNumber}, SN {job.RadioSerialNumber}");

            await _context.SaveChangesAsync();
        }

        public async Task RestoreAsync(int id, int userId)
        {
            var job = await _context.RadioRepairJobs
                .Include(j => j.Handovers)
                .FirstOrDefaultAsync(j => j.Id == id && j.IsDeleted)
                ?? throw new KeyNotFoundException("Job arsip tidak ditemukan.");

            job.IsDeleted = false;
            job.DeletedAt = null;
            job.DeletedByUserId = null;
            job.UpdatedAt = DateTime.UtcNow;

            await _activityLog.LogAsync("RadioRepairJob", job.Id, "Restore", userId,
                $"Pulihkan tiket {job.HelpdeskTicketNumber}, SN {job.RadioSerialNumber}");

            await _context.SaveChangesAsync();
        }

        public async Task DeletePermanentAsync(int id, int userId)
        {
            var job = await _context.RadioRepairJobs
                .Include(j => j.Handovers).ThenInclude(h => h.Accessories)
                .Include(j => j.Handovers).ThenInclude(h => h.Photos)
                .Include(j => j.StatusLogs)
                .FirstOrDefaultAsync(j => j.Id == id)
                ?? throw new KeyNotFoundException("Job arsip tidak ditemukan.");

            if (!job.IsDeleted)
                throw new InvalidOperationException("Pekerjaan harus berada di arsip sebelum dihapus permanen.");

            var ticket = job.HelpdeskTicketNumber;
            var sn = job.RadioSerialNumber;

            job.CurrentHandoverId = null;
            foreach (var h in job.Handovers.ToList())
            {
                _context.RadioHandoverAccessories.RemoveRange(h.Accessories);
                _context.RadioHandoverPhotos.RemoveRange(h.Photos);
                _context.RadioHandovers.Remove(h);
            }

            _context.RadioRepairJobStatusLogs.RemoveRange(job.StatusLogs);
            _context.RadioRepairJobs.Remove(job);

            await _activityLog.LogAsync("RadioRepairJob", id, "PermanentlyDeleted", userId,
                $"Hapus permanen tiket {ticket}, SN {sn}");

            await _context.SaveChangesAsync();
        }

        internal static async Task ValidateDuplicateTicketSerialAsync(
            AppDbContext context,
            string ticket,
            string serial,
            int? excludeJobId = null)
        {
            var exists = await context.RadioRepairJobs.AnyAsync(j =>
                !j.IsDeleted &&
                j.HelpdeskTicketNumber == ticket &&
                j.RadioSerialNumber == serial &&
                j.Status != RadioRepairJobStatus.Cancelled &&
                (!excludeJobId.HasValue || j.Id != excludeJobId.Value));
            if (exists)
                throw new InvalidOperationException(
                    $"Sudah ada pekerjaan aktif untuk tiket {ticket} dengan SN {serial}.");
        }

        private async Task ValidateDuplicateTicketSerialAsync(string ticket, string serial, int? excludeJobId = null) =>
            await ValidateDuplicateTicketSerialAsync(_context, ticket, serial, excludeJobId);

        private static async Task ValidateRadioSerialLinkAsync(AppDbContext context, int? radioId, string serial)
        {
            if (!radioId.HasValue) return;
            var radio = await context.Radios.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == radioId.Value)
                ?? throw new ArgumentException("Radio tidak ditemukan di master.");
            if (!string.Equals(radio.SerialNumber, serial.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Serial number tidak cocok dengan data master radio.");
        }

        private Task ValidateRadioSerialLinkAsync(int? radioId, string serial) =>
            ValidateRadioSerialLinkAsync(_context, radioId, serial);

        private static void ValidateStatusTransition(
            RadioRepairJobStatus from, RadioRepairJobStatus to, bool isSupervisor)
        {
            if (to == RadioRepairJobStatus.Cancelled) return;

            // Supervisor bisa override ke status manapun (termasuk rollback dari RepairCompleted)
            // kecuali status yang sudah final (HandedToWarehouse, ReturnedToHelpdesk)
            if (isSupervisor)
            {
                var supervisorForbidden = new[]
                {
                    RadioRepairJobStatus.HandedToWarehouse,
                    RadioRepairJobStatus.ReturnedToHelpdesk
                };
                if (supervisorForbidden.Contains(to))
                    throw new InvalidOperationException($"Status {to} hanya bisa dicapai melalui proses serah terima.");
                return; // supervisor bebas ke status lain
            }

            var allowed = from switch
            {
                RadioRepairJobStatus.Received => new[]
                {
                    RadioRepairJobStatus.InProgress,
                    RadioRepairJobStatus.Monitoring,
                    RadioRepairJobStatus.WaitingMaterialApproval,
                    RadioRepairJobStatus.Cancelled
                },
                RadioRepairJobStatus.InProgress => new[] { RadioRepairJobStatus.Monitoring, RadioRepairJobStatus.WaitingMaterialApproval, RadioRepairJobStatus.RepairCompleted },
                RadioRepairJobStatus.Monitoring => new[] { RadioRepairJobStatus.InProgress, RadioRepairJobStatus.WaitingMaterialApproval, RadioRepairJobStatus.RepairCompleted },
                RadioRepairJobStatus.WaitingMaterialApproval => Array.Empty<RadioRepairJobStatus>(),
                RadioRepairJobStatus.RepairCompleted => new[] { RadioRepairJobStatus.InProgress }, // teknisi bisa rollback jika salah tekan
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
                Details = $"Tiket {job.HelpdeskTicketNumber}: {from} → {to}. Teknisi: {tech}. {(note != null ? "Catatan: " + note : "")}",
                CreatedBy = await GetUserDisplayNameAsync(userId),
                CreatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Tulis ke RadioHistories saat ada perubahan field data (edit metadata job).
        /// Hanya dipanggil jika job terhubung ke master radio (RadioId != null).
        /// </summary>
        private async Task WriteEditHistoryAsync(
            Models.RadioRepairJob job, List<string> changes, string editorLabel, int userId)
        {
            if (!job.RadioId.HasValue) return;
            if (changes.Count == 0) return;
            _context.RadioHistories.Add(new RadioHistory
            {
                RadioId = job.RadioId.Value,
                Action = "RepairJobEdited",
                Details = $"Tiket {job.HelpdeskTicketNumber} diedit oleh {editorLabel}. Perubahan: {string.Join("; ", changes)}",
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

        private static RadioRepairJobDetailDto MapDetail(Models.RadioRepairJob job, bool includeDeletedHandovers = false) => new()
        {
            Id = job.Id,
            HelpdeskTicketNumber = job.HelpdeskTicketNumber,
            RadioSerialNumber = job.RadioSerialNumber,
            RadioId = job.RadioId,
            RadioMasterRadioId = job.Radio?.RadioId,
            RadioFleet = job.Radio?.Fleet,
            RadioCategory = job.Radio?.Category,
            BatterySerialNumber = job.BatterySerialNumber,
            EquipmentName = job.EquipmentName ?? job.Radio?.Type,
            UnitNumber = job.UnitNumber ?? job.Radio?.NomorUnit,
            RadioOwnerLabel = job.RadioOwnerLabel ?? FormatJobOwnerLabel(job.Radio),
            OwnerDivision = job.OwnerDivision ?? job.Radio?.Division,
            OwnerDepartment = job.OwnerDepartment ?? job.Radio?.Department,
            EquipmentTagType = job.EquipmentTagType?.ToString(),
            OriginFrom = job.OriginFrom,
            RepairDataDescription = job.RepairDataDescription,
            RepairedByName = job.RepairedByName,
            FrequencyError = job.FrequencyError,
            AfReading = job.AfReading,
            PowerReading = job.PowerReading,
            VoltageOutNoLoad = job.VoltageOutNoLoad,
            VoltageOutWithLoad = job.VoltageOutWithLoad,
            PhysicalCondition = job.PhysicalCondition,
            DisplayCondition = job.DisplayCondition,
            DamageDescription = job.DamageDescription,
            Status = job.Status.ToString(),
            AssignedTechnicianUserId = job.AssignedTechnicianUserId,
            AssignedTechnicianName = job.AssignedTechnician.FullName,
            CustomStatusId = job.CustomStatusId,
            CustomStatusLabel = job.CustomStatus?.Label,
            CustomStatusColor = job.CustomStatus?.Color,
            OpenedByName = job.OpenedBy.FullName,
            OpenedAt = job.OpenedAt,
            ClosedAt = job.ClosedAt,
            IsDeleted = job.IsDeleted,
            DeletedAt = job.DeletedAt,
            StatusLogs = job.StatusLogs.OrderByDescending(l => l.At).Select(l => new RadioRepairJobStatusLogDto
            {
                Id = l.Id,
                FromStatus = l.FromStatus?.ToString(),
                ToStatus = l.ToStatus.ToString(),
                Note = l.Note,
                UserName = l.User.FullName,
                At = l.At
            }).ToList(),
            Handovers = job.Handovers
                .Where(h => includeDeletedHandovers || !h.IsDeleted)
                .OrderBy(h => h.HandoverAt)
                .Select(h => new RadioRepairJobHandoverSummaryDto
            {
                Id = h.Id,
                HandoverNumber = h.HandoverNumber,
                HandoverType = h.HandoverType.ToString(),
                HandoverAt = h.HandoverAt,
                SignedAt = h.SignedAt,
                EquipmentTagType = h.EquipmentTagType.ToString(),
                HandedOverByName = h.HandedOverBy.FullName,
                ReceivedByName = h.ReceivedBy.FullName,
                HasRadioPhoto = !string.IsNullOrEmpty(h.RadioPhotoBase64),
                HasHandedOverSignature = !string.IsNullOrEmpty(h.HandedOverSignatureBase64),
                HasReceiverSignature = !string.IsNullOrEmpty(h.ReceiverSignatureBase64)
            }).ToList(),
            PrimaryHandover = job.Handovers
                .Where(h => !h.IsDeleted && h.HandoverType == RadioHandoverType.HelpdeskToTechnician)
                .OrderBy(h => h.HandoverAt)
                .Select(h => MapPrimaryHandover(h, job))
                .FirstOrDefault()
        };

        /// <summary>Isi ID Radio & Fleet dari master (FK atau lookup SN) jika belum terisi di DTO.</summary>
        private async Task EnrichRadioMasterFieldsAsync(IList<RadioRepairJobListDto> items)
        {
            if (items.Count == 0) return;

            var radioIds = items.Where(i => i.RadioId.HasValue).Select(i => i.RadioId!.Value).Distinct().ToList();
            var serialKeys = items
                .Select(i => i.RadioSerialNumber.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var radiosById = radioIds.Count > 0
                ? await _context.Radios.AsNoTracking()
                    .Where(r => radioIds.Contains(r.Id))
                    .ToDictionaryAsync(r => r.Id)
                : new Dictionary<int, Models.Radio>();

            var allBySerial = serialKeys.Count > 0
                ? await _context.Radios.AsNoTracking()
                    .Where(r => r.SerialNumber != null)
                    .ToListAsync()
                : new List<Models.Radio>();

            var serialMap = allBySerial
                .Where(r => serialKeys.Contains(r.SerialNumber!.Trim(), StringComparer.OrdinalIgnoreCase))
                .GroupBy(r => r.SerialNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Id).First(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                Models.Radio? radio = null;
                if (item.RadioId.HasValue && radiosById.TryGetValue(item.RadioId.Value, out var byId))
                    radio = byId;
                else if (serialMap.TryGetValue(item.RadioSerialNumber.Trim(), out var bySn))
                {
                    radio = bySn;
                    item.RadioId ??= radio.Id;
                }

                if (radio == null) continue;

                if (string.IsNullOrWhiteSpace(item.RadioMasterRadioId))
                    item.RadioMasterRadioId = radio.RadioId?.Trim();
                if (string.IsNullOrWhiteSpace(item.RadioFleet))
                    item.RadioFleet = radio.Fleet?.Trim();
                if (string.IsNullOrWhiteSpace(item.RadioCategory))
                    item.RadioCategory = radio.Category;
                // Selalu pakai nama alat dari master radio jika job terhubung ke master
                // agar konsisten dengan data yang tampil di detail panel
                if (!string.IsNullOrWhiteSpace(radio.Type))
                    item.EquipmentName = radio.Type.Trim();
            }
        }

        private static string? FormatJobOwnerLabel(Models.Radio? radio)
        {
            if (radio == null) return null;
            if (!string.IsNullOrWhiteSpace(radio.Company)) return radio.Company.Trim();
            return radio.Category;
        }

        private static RadioRepairPrimaryHandoverDto MapPrimaryHandover(Models.RadioHandover h, Models.RadioRepairJob job) => new()
        {
            Id = h.Id,
            HandoverNumber = h.HandoverNumber,
            HandoverAt = h.HandoverAt,
            HandedOverByName = h.HandedOverBy.FullName,
            ReceivedByName = h.ReceivedBy.FullName,
            Status = h.Status,
            EquipmentName = h.EquipmentName,
            UnitNumber = h.UnitNumber,
            RadioOwnerLabel = h.RadioOwnerLabel,
            OwnerDivision = h.OwnerDivision,
            OwnerDepartment = h.OwnerDepartment,
            RadioSerialNumber = h.RadioSerialNumber,
            BatterySerialNumber = h.BatterySerialNumber,
            DamageDescription = job.DamageDescription,
            Accessories = h.Accessories.Select(a => new RadioRepairHandoverAccessoryDto
            {
                ItemName = string.IsNullOrWhiteSpace(a.ItemName) ? (a.AccessoryCode ?? "") : a.ItemName,
                Quantity = a.Quantity,
                Unit = a.Unit,
                Description = a.Description,
                SerialNumber = a.SerialNumber
            }).ToList()
        };

        public async Task ResetTestingDataAsync(int userId)
        {
            // Menghapus data perbaikan dan handover untuk tujuan testing/reset
            await _context.RadioHandoverAccessories.ExecuteDeleteAsync();
            await _context.RadioHandoverPhotos.ExecuteDeleteAsync();
            
            // Set CurrentHandoverId null agar tidak terjadi constraint error saat delete handover
            await _context.RadioRepairJobs.ExecuteUpdateAsync(s => s.SetProperty(j => j.CurrentHandoverId, (int?)null));
            
            await _context.RadioHandovers.ExecuteDeleteAsync();
            await _context.RadioRepairJobStatusLogs.ExecuteDeleteAsync();
            await _context.RadioRepairJobs.ExecuteDeleteAsync();

            await _activityLog.LogAsync("RadioRepairJob", 0, "ResetTestingData", userId, "Super Admin melakukan reset data serah terima dan perbaikan (testing data)");
        }
    }
}
