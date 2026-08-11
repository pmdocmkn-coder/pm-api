using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.RadioRepairJob;
using Pm.Enums;
using Pm.Models;
using Pm.Services;
using Pm.Services.Notification;
using Pm.DTOs.Notification;

namespace Pm.Services.RadioRepairJob
{
    public class RadioRepairJobService(AppDbContext context, IActivityLogService activityLog, INotificationService notificationService) : IRadioRepairJobService
    {
        private readonly AppDbContext _context = context;
        private readonly IActivityLogService _activityLog = activityLog;
        private readonly INotificationService _notificationService = notificationService;

        private IQueryable<Models.RadioRepairJob> BaseQuery() =>
            _context.RadioRepairJobs.AsNoTracking()
                .Include(j => j.AssignedTechnician)
                .Include(j => j.WorkshopTechnician)
                .Include(j => j.Radio)
                .Include(j => j.CustomStatus)
                .Where(j => !(j.Status == RadioRepairJobStatus.Received &&
                              j.Handovers.Any(h => h.Id == j.CurrentHandoverId && h.Status != "Completed")));

        private static IQueryable<Models.RadioRepairJob> ApplyDeletedFilter(
            IQueryable<Models.RadioRepairJob> q, bool includeDeleted) =>
            includeDeleted ? q.Where(j => j.IsDeleted) : q.Where(j => !j.IsDeleted);

        public async Task<PagedResultDto<RadioRepairJobListDto>> GetAllAsync(
            RadioRepairJobQueryDto query, int currentUserId, string? roleName)
        {
            var q = ApplyDeletedFilter(BaseQuery(), query.IncludeDeleted);

            if (!string.IsNullOrWhiteSpace(query.Status) &&
                Enum.TryParse<RadioRepairJobStatus>(query.Status, true, out var st))
            {
                q = q.Where(j => j.Status == st);
            }

            if (query.TechnicianUserId.HasValue)
                q = q.Where(j => j.AssignedTechnicianUserId == query.TechnicianUserId);

            if (query.FromDate.HasValue)
                q = q.Where(j => j.OpenedAt >= query.FromDate.Value);
            if (query.ToDate.HasValue)
                q = q.Where(j => j.OpenedAt <= query.ToDate.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = $"%{query.Search.Trim()}%";
                q = q.Where(j =>
                    EF.Functions.Like(j.HelpdeskTicketNumber, s) ||
                    EF.Functions.Like(j.RadioSerialNumber, s) ||
                    EF.Functions.Like(j.DamageDescription, s) ||
                    (j.EquipmentName != null && EF.Functions.Like(j.EquipmentName, s)) ||
                    (j.Radio != null && j.Radio.RadioId != null && EF.Functions.Like(j.Radio.RadioId, s)) ||
                    (j.Radio != null && j.Radio.Fleet != null && EF.Functions.Like(j.Radio.Fleet, s)) ||
                    (j.Radio != null && j.Radio.Type != null && EF.Functions.Like(j.Radio.Type, s)) ||
                    _context.Radios.Any(r =>
                        r.SerialNumber != null &&
                        r.SerialNumber == j.RadioSerialNumber &&
                        ((r.RadioId != null && EF.Functions.Like(r.RadioId, s)) ||
                         (r.Fleet != null && EF.Functions.Like(r.Fleet, s)) ||
                         (r.Type != null && EF.Functions.Like(r.Type, s)))));
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
                    UnitNumber = j.UnitNumber ?? (j.Radio != null ? j.Radio.NomorUnit : null),
                    RadioOwnerLabel = j.RadioOwnerLabel ?? (j.Radio != null ? j.Radio.Company : null),
                    OwnerDivision = j.OwnerDivision ?? (j.Radio != null ? j.Radio.Division : null),
                    OwnerDepartment = j.OwnerDepartment ?? (j.Radio != null ? j.Radio.Department : null),
                    PreviewPhotoBase64 = null, // loaded lazily on frontend
                    PhotoHandoverId = j.Handovers
                        .Where(h => !h.IsDeleted)
                        .OrderBy(h => h.HandoverType == RadioHandoverType.HelpdeskToTechnician ? 0 : 1)
                        .ThenBy(h => h.Id)
                        .Select(h => h.Id)
                        .FirstOrDefault(),
                    PhotoCount = j.Handovers
                        .Where(h => !h.IsDeleted)
                        .OrderBy(h => h.HandoverType == RadioHandoverType.HelpdeskToTechnician ? 0 : 1)
                        .ThenBy(h => h.Id)
                        .Select(h => h.Photos.Count > 0 ? h.Photos.Count : (!string.IsNullOrEmpty(h.RadioPhotoBase64) ? 1 : 0))
                        .FirstOrDefault(),
                    EquipmentTagType = j.EquipmentTagType != null ? j.EquipmentTagType.ToString() : null,
                    IsWarranty = j.IsWarranty,
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
                    WorkshopTechnicianId = j.WorkshopTechnicianId,
                    WorkshopTechnicianName = j.WorkshopTechnician != null ? j.WorkshopTechnician.Name : null,
                    CustomStatusId = j.CustomStatusId,
                    CustomStatusLabel = j.CustomStatus != null ? j.CustomStatus.Label : null,
                    CustomStatusColor = j.CustomStatus != null ? j.CustomStatus.Color : null,
                    OpenedAt = j.OpenedAt,
                    ClosedAt = j.ClosedAt,
                    FirstInProgressAt = j.StatusLogs.Where(l => l.ToStatus == RadioRepairJobStatus.InProgress).OrderBy(l => l.At).Select(l => (DateTime?)l.At).FirstOrDefault(),
                    WorkshopCompletedAt = j.StatusLogs.Where(l => l.ToStatus == RadioRepairJobStatus.RepairCompleted || l.ToStatus == RadioRepairJobStatus.ProcessScrap || l.ToStatus == RadioRepairJobStatus.HandedToWarehouse || l.ToStatus == RadioRepairJobStatus.ReturnedToHelpdesk || l.ToStatus == RadioRepairJobStatus.Scrapped).OrderBy(l => l.At).Select(l => (DateTime?)l.At).FirstOrDefault(),
                    AccumulatedProgressDurationMinutes = j.AccumulatedProgressDurationMinutes,
                    CurrentProgressStartedAt = j.CurrentProgressStartedAt,
                    IsDeleted = j.IsDeleted,
                    DeletedAt = j.DeletedAt,
                    HasBorrowRequest = j.PartBorrows.Any(),
                    HasActiveBorrowedPart = j.PartBorrows.Any(pb => pb.Status != WarehousePartBorrowStatus.Returned && pb.Status != WarehousePartBorrowStatus.Rejected),
                    HasReturnedBorrowedPart = j.PartBorrows.Any(pb => pb.Status == WarehousePartBorrowStatus.Returned),
                    PendingHandoverType = j.CurrentHandoverId.HasValue && j.CurrentHandoverId > 0
                        ? j.Handovers.Where(h => h.Id == j.CurrentHandoverId && h.Status != "Completed").Select(h => h.HandoverType.ToString()).FirstOrDefault()
                        : j.Handovers.Where(h => h.Status != "Completed" && !h.IsDeleted).OrderByDescending(h => h.Id).Select(h => h.HandoverType.ToString()).FirstOrDefault()
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
            return [.. paged.Data
                .GroupBy(j => j.HelpdeskTicketNumber)
                .OrderByDescending(g => g.Max(x => x.OpenedAt))
                .Select(g => new RadioRepairTicketGroupDto
                {
                    HelpdeskTicketNumber = g.Key,
                    RadioCount = g.Count(),
                    Radios = [.. g.OrderByDescending(x => x.OpenedAt)]
                })];
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
                .Include(j => j.Handovers).ThenInclude(h => h.WorkshopTechnician)
                .Include(j => j.Handovers).ThenInclude(h => h.HandedOverByWorkshopTechnician)
                .AsSplitQuery()
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return null;

            var dto = MapDetail(job, includeDeletedHandovers: false);
            await EnrichRadioMasterFieldsAsync([dto]);
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
                    Note = $"Status diubah ke: {customStatus.Label}{(string.IsNullOrWhiteSpace(dto.Note) ? "" : $". {dto.Note}")}",
                    UserId = userId,
                    At = DateTime.UtcNow
                });

                await _activityLog.LogAsync("RadioRepairJob", job.Id, "CustomStatusChange", userId,
                    $"Custom status → {customStatus.Label}");
                await _context.SaveChangesAsync();
                await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");

                // Notifikasi custom status ke Supv MKN
                var customAlatInfo = string.IsNullOrEmpty(job.EquipmentName) ? "" : $" ({job.EquipmentName})";
                var customTiketInfo = string.IsNullOrEmpty(job.HelpdeskTicketNumber) ? "" : $" — Tiket {job.HelpdeskTicketNumber}";
                var customTechName = await _context.WorkshopTechnicians.AsNoTracking()
                    .Where(t => t.Id == job.WorkshopTechnicianId)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync() ?? "Teknisi";

                await _notificationService.CreateForPermissionAsync(Pm.Helper.NotificationPermissions.RadioRepair, new CreateNotificationDto
                {
                    Title = $"Status: {customStatus.Label}",
                    Message = $"Radio SN {job.RadioSerialNumber}{customAlatInfo} kini dalam status \"{customStatus.Label}\" oleh {customTechName}{customTiketInfo}.",
                    Category = "repair",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });

                // Notifikasi ke Helpdesk
                await _notificationService.CreateForRoleAsync(Pm.Helper.OperationalRoleNames.Helpdesk, new CreateNotificationDto
                {
                    Title = $"Status Perbaikan: {customStatus.Label}",
                    Message = $"Radio SN {job.RadioSerialNumber}{customAlatInfo} dalam status \"{customStatus.Label}\"{customTiketInfo}.",
                    Category = "repair",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });

                return (await GetByIdAsync(id, userId, roleName))!;
            }

            // Clear custom status jika ada
            job.CustomStatusId = null;

            var from = job.Status;
            var isSupervisor = string.Equals(roleName, Pm.Helper.OperationalRoleNames.SupervisorWorkshop,
                StringComparison.OrdinalIgnoreCase);
            ValidateStatusTransition(from, dto.Status, isSupervisor);

            if (dto.Status == RadioRepairJobStatus.RepairCompleted && job.EquipmentTagType == null)
            {
                throw new InvalidOperationException("Mohon lengkapi data perbaikan dan pilih jenis Tag (Hijau/Kuning) terlebih dahulu sebelum mengubah status menjadi Selesai.");
            }

            job.Status = dto.Status;
            string? assignedTechName = null;
            if (dto.WorkshopTechnicianId.HasValue)
            {
                job.WorkshopTechnicianId = dto.WorkshopTechnicianId;
                assignedTechName = await _context.WorkshopTechnicians
                    .AsNoTracking()
                    .Where(t => t.Id == dto.WorkshopTechnicianId)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync();
            }
            job.UpdatedAt = DateTime.UtcNow;

            // --- Duration Calculation Logic ---
            bool isFromActive = from == RadioRepairJobStatus.InProgress || from == RadioRepairJobStatus.Monitoring;
            bool isToActive = dto.Status == RadioRepairJobStatus.InProgress || dto.Status == RadioRepairJobStatus.Monitoring;

            if (isFromActive && !isToActive)
            {
                // Pause duration
                if (job.CurrentProgressStartedAt.HasValue)
                {
                    job.AccumulatedProgressDurationMinutes += (int)(DateTime.UtcNow - job.CurrentProgressStartedAt.Value).TotalMinutes;
                    job.CurrentProgressStartedAt = null;
                }
            }
            else if (!isFromActive && isToActive)
            {
                // Start or resume duration
                if (from == RadioRepairJobStatus.WaitingMaterialApproval)
                {
                    job.AccumulatedProgressDurationMinutes = 0;
                }
                job.CurrentProgressStartedAt = DateTime.UtcNow;
            }
            // ----------------------------------

            var statusNote = dto.Note;
            if (isSupervisor && from != dto.Status)
            {
                statusNote = string.IsNullOrWhiteSpace(statusNote)
                    ? "Diubah oleh Supervisor MKN"
                    : statusNote; // pakai note dari supervisor langsung, tidak perlu prefix
            }

            // Tambahkan info teknisi yang di-assign ke note
            if (!string.IsNullOrEmpty(assignedTechName))
            {
                var techNote = $"Dikerjakan oleh: {assignedTechName}";
                statusNote = string.IsNullOrWhiteSpace(statusNote)
                    ? techNote
                    : $"{statusNote}. {techNote}";
            }

            // Ambil nama teknisi saat ini (jika ada) untuk dilampirkan ke log
            string? currentTechName = assignedTechName;
            if (string.IsNullOrEmpty(currentTechName) && job.WorkshopTechnicianId.HasValue)
            {
                currentTechName = await _context.WorkshopTechnicians
                    .AsNoTracking()
                    .Where(t => t.Id == job.WorkshopTechnicianId)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync();
            }

            await AddStatusLogAsync(job.Id, from, dto.Status, statusNote, userId, currentTechName);
            await WriteRepairHistoryAsync(job, from, dto.Status, statusNote, userId);
            await _activityLog.LogAsync("RadioRepairJob", job.Id, "StatusChange", userId,
                $"Status {from} → {dto.Status}{(isSupervisor ? " (supervisor override)" : "")}");

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");

            // === Trigger Notifications ===
            // Variabel info untuk semua pesan notifikasi
            var alatInfo = string.IsNullOrEmpty(job.EquipmentName) ? "" : $" ({job.EquipmentName})";
            var tiketInfo = string.IsNullOrEmpty(job.HelpdeskTicketNumber) ? "" : $" — Tiket {job.HelpdeskTicketNumber}";

            // Notifikasi Khusus (Supervisor / Warehouse)
            if (dto.Status == RadioRepairJobStatus.InProgress)
            {
                var techDisplayName = assignedTechName ?? currentTechName ?? "Teknisi";
                // Cek apakah ini lanjutan dari Monitoring — jika iya, sebut teknisi monitoring sebelumnya
                var fromMonitoring = from == RadioRepairJobStatus.Monitoring;
                string inProgressMsg;
                string inProgressTitle;
                if (fromMonitoring && !string.IsNullOrEmpty(currentTechName) && currentTechName != techDisplayName)
                {
                    // Teknisi berbeda — lanjutan dari monitoring oleh orang lain
                    inProgressTitle = "Perbaikan Dilanjutkan Pasca Monitoring";
                    inProgressMsg = $"Radio SN {job.RadioSerialNumber}{alatInfo} dilanjutkan perbaikannya oleh {techDisplayName} (sebelumnya dimonitoring oleh {currentTechName}){tiketInfo}.";
                }
                else if (fromMonitoring)
                {
                    // Teknisi sama atau tidak berubah — lanjutan monitoring oleh orang yang sama
                    inProgressTitle = "Perbaikan Dilanjutkan Pasca Monitoring";
                    inProgressMsg = $"Radio SN {job.RadioSerialNumber}{alatInfo} selesai dimonitoring dan kini dilanjutkan perbaikannya oleh {techDisplayName}{tiketInfo}.";
                }
                else
                {
                    inProgressTitle = "Radio Mulai Diperbaiki";
                    inProgressMsg = $"Radio SN {job.RadioSerialNumber}{alatInfo} sedang diperbaiki oleh {techDisplayName}{tiketInfo}.";
                }

                await _notificationService.CreateForPermissionAsync(
                    Pm.Helper.NotificationPermissions.RadioRepair,
                    new CreateNotificationDto
                    {
                        Title = inProgressTitle,
                        Message = inProgressMsg,
                        Category = "repair",
                        LinkUrl = "/radio-repair-dashboard",
                        ReferenceId = job.Id,
                        ReferenceType = "RadioRepairJob"
                    },
                    // Skip teknisi yang mengerjakan agar tidak duplikat dengan notif personal di bawah
                    excludeUserIds: job.AssignedTechnicianUserId != 0
                        ? new[] { job.AssignedTechnicianUserId }
                        : null
                );
            }
            else if (dto.Status == RadioRepairJobStatus.Monitoring)
            {
                var techForMonitoring = assignedTechName ?? currentTechName ?? "Teknisi";
                await _notificationService.CreateForPermissionAsync(Pm.Helper.NotificationPermissions.RadioRepair, new CreateNotificationDto
                {
                    Title = "Radio Sedang Dimonitoring",
                    Message = $"Radio SN {job.RadioSerialNumber}{alatInfo} sedang dalam tahap monitoring oleh {techForMonitoring}{tiketInfo}.",
                    Category = "repair",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }
            else if (dto.Status == RadioRepairJobStatus.ProcessScrap)
            {
                await _notificationService.CreateForPermissionAsync(Pm.Helper.NotificationPermissions.RadioRepair, new CreateNotificationDto
                {
                    Title = "Pengajuan Scrap Radio",
                    Message = $"Radio SN {job.RadioSerialNumber}{alatInfo} diajukan untuk di-scrap{tiketInfo}. Mohon ditinjau.",
                    Category = "scrap",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }
            else if (dto.Status == RadioRepairJobStatus.WaitingMaterialApproval)
            {
                var techForMaterial = assignedTechName ?? currentTechName ?? "Teknisi";
                await _notificationService.CreateForPermissionAsync(Pm.Helper.NotificationPermissions.RadioRepair, new CreateNotificationDto
                {
                    Title = "Persetujuan Material Diperlukan",
                    Message = $"{techForMaterial} membutuhkan persetujuan material untuk Radio SN {job.RadioSerialNumber}{alatInfo}{tiketInfo}.",
                    Category = "repair",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }
            else if (dto.Status == RadioRepairJobStatus.RepairCompleted)
            {
                var techForCompleted = assignedTechName ?? currentTechName ?? "Teknisi";
                await _notificationService.CreateForPermissionAsync(Pm.Helper.NotificationPermissions.RadioRepair, new CreateNotificationDto
                {
                    Title = "Perbaikan Radio Selesai",
                    Message = $"Radio SN {job.RadioSerialNumber}{alatInfo} telah selesai diperbaiki oleh {techForCompleted}{tiketInfo}.",
                    Category = "repair",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }
            else if (dto.Status == RadioRepairJobStatus.HandedToWarehouse)
            {
                await _notificationService.CreateForRoleAsync(Pm.Helper.OperationalRoleNames.Warehouse, new CreateNotificationDto
                {
                    Title = "Radio Masuk Warehouse",
                    Message = $"Radio SN {job.RadioSerialNumber}{alatInfo} telah diserahkan ke Warehouse{tiketInfo}.",
                    Category = "handover",
                    LinkUrl = "/radio-handover/warehouse",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }

            // Notifikasi untuk Helpdesk (Selalu menerima notifikasi untuk seluruh alur status)
            string hdTitle = "Update Status Radio";
            string hdMessage = $"Radio SN {job.RadioSerialNumber}{alatInfo} sedang diproses.";

            if (dto.Status == RadioRepairJobStatus.InProgress)
            {
                var techForHd = assignedTechName ?? currentTechName ?? "teknisi";
                if (from == RadioRepairJobStatus.Monitoring && !string.IsNullOrEmpty(currentTechName) && currentTechName != techForHd)
                {
                    hdTitle = "Perbaikan Dilanjutkan Pasca Monitoring";
                    hdMessage = $"Radio SN {job.RadioSerialNumber}{alatInfo} kini diperbaiki oleh {techForHd} (sebelumnya dimonitoring oleh {currentTechName}){tiketInfo}.";
                }
                else if (from == RadioRepairJobStatus.Monitoring)
                {
                    hdTitle = "Perbaikan Dilanjutkan Pasca Monitoring";
                    hdMessage = $"Radio SN {job.RadioSerialNumber}{alatInfo} selesai dimonitoring dan perbaikan dilanjutkan oleh {techForHd}{tiketInfo}.";
                }
                else
                {
                    hdTitle = "Radio Mulai Diperbaiki";
                    hdMessage = $"Radio SN {job.RadioSerialNumber}{alatInfo} sedang diperbaiki oleh {techForHd}{tiketInfo}.";
                }
            }
            else if (dto.Status == RadioRepairJobStatus.Monitoring)
            {
                hdTitle = "Radio Sedang Dimonitoring";
                hdMessage = $"Radio SN {job.RadioSerialNumber}{alatInfo} sedang dalam tahap monitoring{tiketInfo}.";
            }
            else if (dto.Status == RadioRepairJobStatus.WaitingMaterialApproval)
            {
                hdTitle = "Menunggu Persetujuan Material";
                hdMessage = $"Radio SN {job.RadioSerialNumber}{alatInfo} membutuhkan persetujuan material untuk melanjutkan perbaikan{tiketInfo}.";
            }
            else if (dto.Status == RadioRepairJobStatus.RepairCompleted)
            {
                hdTitle = "Perbaikan Selesai";
                hdMessage = $"Radio SN {job.RadioSerialNumber}{alatInfo} telah selesai diperbaiki{tiketInfo}.";
            }
            else if (dto.Status == RadioRepairJobStatus.HandedToWarehouse)
            {
                hdTitle = "Radio Diserahkan ke Warehouse";
                hdMessage = $"Radio SN {job.RadioSerialNumber}{alatInfo} telah diserahkan ke Warehouse{tiketInfo}.";
            }
            else if (dto.Status == RadioRepairJobStatus.ReturnedToHelpdesk)
            {
                hdTitle = "Radio Dikembalikan ke Helpdesk";
                hdMessage = $"Radio SN {job.RadioSerialNumber}{alatInfo} telah dikembalikan ke Helpdesk{tiketInfo}.";
            }
            else if (dto.Status == RadioRepairJobStatus.Received)
            {
                hdTitle = "Radio Diterima di Workshop";
                hdMessage = $"Radio SN {job.RadioSerialNumber}{alatInfo} telah diterima di Workshop{tiketInfo}.";
            }

            await _notificationService.CreateForRoleAsync(Pm.Helper.OperationalRoleNames.Helpdesk, new CreateNotificationDto
            {
                Title = hdTitle,
                Message = hdMessage,
                Category = dto.Status == RadioRepairJobStatus.HandedToWarehouse || dto.Status == RadioRepairJobStatus.ReturnedToHelpdesk ? "handover" : "repair",
                LinkUrl = "/radio-repair-dashboard",
                ReferenceId = job.Id,
                ReferenceType = "RadioRepairJob"
            });

            // Notif ke teknisi yang ditugaskan saat InProgress
            if (dto.Status == RadioRepairJobStatus.InProgress && job.AssignedTechnicianUserId != 0)
            {
                var techDisplayForNotif = assignedTechName ?? currentTechName ?? "Teknisi";
                string tekTitle, tekMsg;
                if (from == RadioRepairJobStatus.Monitoring)
                {
                    tekTitle = "Lanjutkan Perbaikan Pasca Monitoring";
                    tekMsg = $"Radio SN {job.RadioSerialNumber}{alatInfo} selesai dimonitoring. Silakan lanjutkan perbaikan, {techDisplayForNotif}{tiketInfo}.";
                }
                else
                {
                    tekTitle = "Perbaikan Radio Dimulai";
                    tekMsg = $"Radio SN {job.RadioSerialNumber}{alatInfo} mulai dikerjakan oleh {techDisplayForNotif}{tiketInfo}.";
                }
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = job.AssignedTechnicianUserId,
                    Title = tekTitle,
                    Message = tekMsg,
                    Category = "repair",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }
            // =============================

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
            {
                throw new ArgumentException("Status lanjutan harus InProgress atau Monitoring.");
            }

            var from = job.Status;
            job.Status = dto.ResumeStatus;
            string? assignedTechName = null;
            if (dto.WorkshopTechnicianId.HasValue)
            {
                job.WorkshopTechnicianId = dto.WorkshopTechnicianId;
                assignedTechName = await _context.WorkshopTechnicians
                    .AsNoTracking()
                    .Where(t => t.Id == dto.WorkshopTechnicianId)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync();
            }

            string? currentTechName = assignedTechName;
            if (string.IsNullOrEmpty(currentTechName) && job.WorkshopTechnicianId.HasValue)
            {
                currentTechName = await _context.WorkshopTechnicians
                    .AsNoTracking()
                    .Where(t => t.Id == job.WorkshopTechnicianId)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync();
            }

            var note = dto.Note ?? "Material disetujui";
            if (!string.IsNullOrEmpty(assignedTechName))
            {
                var techNote = $"Dikerjakan oleh: {assignedTechName}";
                note = $"{note}. {techNote}";
            }

            job.UpdatedAt = DateTime.UtcNow;

            // --- Duration Calculation Logic ---
            if (dto.ResumeStatus == RadioRepairJobStatus.InProgress)
            {
                // Reset duration to 0 and start
                job.AccumulatedProgressDurationMinutes = 0;
                job.CurrentProgressStartedAt = DateTime.UtcNow;
            }
            else if (dto.ResumeStatus == RadioRepairJobStatus.Monitoring)
            {
                // Resume duration (lanjut dari akumulasi, tidak reset)
                job.CurrentProgressStartedAt = DateTime.UtcNow;
            }
            // ----------------------------------
            await AddStatusLogAsync(job.Id, from, dto.ResumeStatus, note, userId, currentTechName);
            await WriteRepairHistoryAsync(job, from, dto.ResumeStatus, note, userId);
            await _activityLog.LogAsync("RadioRepairJob", job.Id, "ApproveMaterial", userId, note);

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");

            // Notif ke teknisi bahwa material disetujui dan bisa lanjut perbaikan
            if (job.AssignedTechnicianUserId != 0)
            {
                var techName = assignedTechName ?? currentTechName ?? "Anda";
                var approveAlatInfo = string.IsNullOrEmpty(job.EquipmentName) ? "" : $" ({job.EquipmentName})";
                var approveTiketInfo = string.IsNullOrEmpty(job.HelpdeskTicketNumber) ? "" : $" — Tiket {job.HelpdeskTicketNumber}";
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = job.AssignedTechnicianUserId,
                    Title = "Material Disetujui",
                    Message = $"Material untuk Radio SN {job.RadioSerialNumber}{approveAlatInfo} telah disetujui. Silakan lanjutkan perbaikan, {techName}{approveTiketInfo}.",
                    Category = "repair",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }

            // Notif ke Helpdesk
            await _notificationService.CreateForRoleAsync(Pm.Helper.OperationalRoleNames.Helpdesk, new CreateNotificationDto
            {
                Title = "Material Perbaikan Disetujui",
                Message = $"Material untuk Radio SN {job.RadioSerialNumber} telah disetujui. Perbaikan dilanjutkan.",
                Category = "repair",
                LinkUrl = "/radio-repair-dashboard",
                ReferenceId = job.Id,
                ReferenceType = "RadioRepairJob"
            });

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
            if (dto.WorkshopTechnicianId.HasValue && job.WorkshopTechnicianId != dto.WorkshopTechnicianId)
            {
                var oldTech = job.WorkshopTechnicianId.HasValue ? await _context.WorkshopTechnicians.AsNoTracking().Where(t => t.Id == job.WorkshopTechnicianId).Select(t => t.Name).FirstOrDefaultAsync() : "None";
                var newTech = await _context.WorkshopTechnicians.AsNoTracking().Where(t => t.Id == dto.WorkshopTechnicianId).Select(t => t.Name).FirstOrDefaultAsync() ?? "Unknown";
                changes.Add($"Teknisi Pekerja: \"{oldTech}\" → \"{newTech}\"");
                job.WorkshopTechnicianId = dto.WorkshopTechnicianId;
            }
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
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");
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

            var changes = new List<string>();

            if (dto.EquipmentTagType.HasValue && job.EquipmentTagType != dto.EquipmentTagType.Value)
            {
                changes.Add($"Tag Fisik: \"{job.EquipmentTagType}\" → \"{dto.EquipmentTagType.Value}\"");
                job.EquipmentTagType = dto.EquipmentTagType.Value;
            }

            if (dto.IsWarranty.HasValue && job.IsWarranty != dto.IsWarranty.Value)
            {
                changes.Add($"Status Garansi: \"{(job.IsWarranty ? "Ya" : "Tidak")}\" → \"{(dto.IsWarranty.Value ? "Ya" : "Tidak")}\"");
                job.IsWarranty = dto.IsWarranty.Value;
            }

            if (dto.DamageDescription != null)
            {
                var newDamage = dto.DamageDescription.Trim();
                if (!string.Equals(job.DamageDescription, newDamage, StringComparison.Ordinal))
                {
                    changes.Add($"Kerusakan: \"{job.DamageDescription}\" → \"{newDamage}\"");
                    job.DamageDescription = newDamage;
                }
            }

            if (dto.EquipmentTagType.HasValue)
            {
                // Map Green tag fields
                if (dto.EquipmentTagType.Value == EquipmentTagType.Good)
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
            }

            if (changes.Count == 0 && dto.EquipmentTagType == EquipmentTagType.Good)
            {
                changes.Add("Data perbaikan diupdate");
            }

            if (changes.Count == 0)
                return (await GetByIdAsync(id, userId, null))!;

            job.UpdatedAt = DateTime.UtcNow;

            var noteString = $"[Edit oleh teknisi] {string.Join("; ", changes)}";
            if (noteString.Length > 500) noteString = $"{noteString.AsSpan(0, 497)}...";

            // Catat di StatusLogs agar muncul di timeline — siapa yang ubah apa
            _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
            {
                JobId = job.Id,
                FromStatus = job.Status,
                ToStatus = job.Status,
                Note = noteString,
                UserId = userId,
                At = DateTime.UtcNow
            });

            await _activityLog.LogAsync("RadioRepairJob", job.Id, "TechnicianEdit", userId,
                string.Join("; ", changes));

            // Tulis ke RadioHistories agar muncul di modal Riwayat Perubahan Radio
            await WriteEditHistoryAsync(job, changes, "teknisi", userId);

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");
            return (await GetByIdAsync(id, userId, null))!;
        }

        public async Task<RadioRepairJobDetailDto> ApproveScrapAsync(int id, ApproveScrapDto dto, int userId, string? roleName)
        {
            var isSupervisor = string.Equals(roleName, Pm.Helper.OperationalRoleNames.SupervisorWorkshop, StringComparison.OrdinalIgnoreCase);
            var isHelpdesk = string.Equals(roleName, Pm.Helper.OperationalRoleNames.Helpdesk, StringComparison.OrdinalIgnoreCase);
            if (!isSupervisor && !isHelpdesk) throw new UnauthorizedAccessException("Hanya Supervisor atau Helpdesk yang dapat menyetujui / input data Scrap.");

            var job = await _context.RadioRepairJobs
                .Include(j => j.Radio)
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status != RadioRepairJobStatus.ProcessScrap && job.Status != RadioRepairJobStatus.ReturnedToHelpdesk)
                throw new InvalidOperationException("Job tidak dalam status Proses Radio Scrap atau Dikembalikan ke Helpdesk.");

            var from = job.Status;
            job.Status = RadioRepairJobStatus.Scrapped;
            job.ClosedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;

            var note = $"Radio disetujui untuk di-scrap. Tanggal: {dto.DateScrapped:dd/MM/yyyy}, Job: {dto.ScrapJobNumber}. Keterangan: {dto.Remarks}";

            if (job.Radio != null)
            {
                job.Radio.IsScrap = true;
                job.Radio.DateScrapped = dto.DateScrapped;
                job.Radio.ScrapJobNumber = dto.ScrapJobNumber ?? job.HelpdeskTicketNumber;
                job.Radio.Remarks = dto.Remarks;
                job.Radio.UpdatedAt = DateTime.UtcNow;
            }

            await AddStatusLogAsync(job.Id, from, job.Status, note, userId);
            await WriteRepairHistoryAsync(job, from, job.Status, note, userId);
            await _activityLog.LogAsync("RadioRepairJob", job.Id, "ApproveScrap", userId, note);

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");

            var scrapAlatInfo = string.IsNullOrEmpty(job.EquipmentName) ? "" : $" ({job.EquipmentName})";
            var scrapTiketInfo = string.IsNullOrEmpty(job.HelpdeskTicketNumber) ? "" : $" — Tiket {job.HelpdeskTicketNumber}";

            // Notif ke Helpdesk — radio disetujui scrap (hanya jika yang menyetujui Supervisor)
            if (isSupervisor)
            {
                await _notificationService.CreateForRoleAsync(Pm.Helper.OperationalRoleNames.Helpdesk, new CreateNotificationDto
                {
                    Title = "Radio Disetujui untuk Scrap",
                    Message = $"Radio SN {job.RadioSerialNumber}{scrapAlatInfo} telah disetujui untuk di-scrap oleh Supervisor MKN{scrapTiketInfo}.",
                    Category = "scrap",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }

            // Notif ke Teknisi (akun yang handle job ini)
            if (job.AssignedTechnicianUserId != 0)
            {
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = job.AssignedTechnicianUserId,
                    Title = "Radio Disetujui untuk Scrap",
                    Message = $"Radio SN {job.RadioSerialNumber}{scrapAlatInfo} telah disetujui untuk di-scrap{scrapTiketInfo}.",
                    Category = "scrap",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }

            return (await GetByIdAsync(id, userId, roleName))!;
        }

        public async Task<RadioRepairJobDetailDto> CancelScrapAsync(int id, int userId, string? roleName)
        {
            var isSupervisor = string.Equals(roleName, Pm.Helper.OperationalRoleNames.SupervisorWorkshop, StringComparison.OrdinalIgnoreCase);
            if (!isSupervisor) throw new UnauthorizedAccessException("Hanya Supervisor yang dapat membatalkan Scrap.");

            var job = await _context.RadioRepairJobs
                .Include(j => j.Radio)
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status != RadioRepairJobStatus.ProcessScrap && job.Status != RadioRepairJobStatus.Scrapped)
                throw new InvalidOperationException("Job tidak dalam status Scrap atau menunggu persetujuan Scrap.");

            var from = job.Status;
            job.Status = RadioRepairJobStatus.InProgress;
            job.ClosedAt = null;
            job.UpdatedAt = DateTime.UtcNow;

            const string note = "Pengajuan scrap dibatalkan oleh Supervisor. Perbaikan dilanjutkan.";

            if (job.Radio != null)
            {
                job.Radio.IsScrap = false;
                job.Radio.DateScrapped = null;
                job.Radio.ScrapJobNumber = null;
                job.Radio.Remarks = $"[Scrap Dibatalkan] {job.Radio.Remarks}";
                job.Radio.UpdatedAt = DateTime.UtcNow;
            }

            await AddStatusLogAsync(job.Id, from, job.Status, note, userId);
            await WriteRepairHistoryAsync(job, from, job.Status, note, userId);
            await _activityLog.LogAsync("RadioRepairJob", job.Id, "CancelScrap", userId, note);

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");

            // Notif ke Helpdesk — scrap dibatalkan
            var cancelAlatInfo = string.IsNullOrEmpty(job.EquipmentName) ? "" : $" ({job.EquipmentName})";
            var cancelTiketInfo = string.IsNullOrEmpty(job.HelpdeskTicketNumber) ? "" : $" — Tiket {job.HelpdeskTicketNumber}";
            await _notificationService.CreateForRoleAsync(Pm.Helper.OperationalRoleNames.Helpdesk, new CreateNotificationDto
            {
                Title = "Pengajuan Scrap Radio Dibatalkan",
                Message = $"Pengajuan scrap untuk Radio SN {job.RadioSerialNumber}{cancelAlatInfo} dibatalkan. Radio kembali dalam proses perbaikan{cancelTiketInfo}.",
                Category = "scrap",
                LinkUrl = "/radio-repair-dashboard",
                ReferenceId = job.Id,
                ReferenceType = "RadioRepairJob"
            });

            // Notif ke Teknisi
            if (job.AssignedTechnicianUserId != 0)
            {
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = job.AssignedTechnicianUserId,
                    Title = "Pengajuan Scrap Dibatalkan",
                    Message = $"Pengajuan scrap untuk Radio SN {job.RadioSerialNumber}{cancelAlatInfo} telah dibatalkan. Silakan lanjutkan perbaikan{cancelTiketInfo}.",
                    Category = "scrap",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }

            return (await GetByIdAsync(id, userId, roleName))!;
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
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");
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
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");
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
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");
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
            {
                throw new InvalidOperationException(
                    $"Sudah ada pekerjaan aktif untuk tiket {ticket} dengan SN {serial}.");
            }
        }

        private Task ValidateDuplicateTicketSerialAsync(string ticket, string serial, int? excludeJobId = null) =>
            ValidateDuplicateTicketSerialAsync(_context, ticket, serial, excludeJobId);

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

            RadioRepairJobStatus[] allowed = from switch
            {
                RadioRepairJobStatus.Received =>
                [
                    RadioRepairJobStatus.InProgress,
                    RadioRepairJobStatus.Monitoring,
                    RadioRepairJobStatus.WaitingMaterialApproval,
                    RadioRepairJobStatus.Cancelled
                ],
                RadioRepairJobStatus.InProgress => [RadioRepairJobStatus.Monitoring, RadioRepairJobStatus.WaitingMaterialApproval, RadioRepairJobStatus.RepairCompleted, RadioRepairJobStatus.ProcessScrap],
                RadioRepairJobStatus.Monitoring => [RadioRepairJobStatus.InProgress, RadioRepairJobStatus.WaitingMaterialApproval, RadioRepairJobStatus.RepairCompleted, RadioRepairJobStatus.ProcessScrap],
                RadioRepairJobStatus.WaitingMaterialApproval => [],
                RadioRepairJobStatus.RepairCompleted => [RadioRepairJobStatus.InProgress], // teknisi bisa rollback jika salah tekan
                RadioRepairJobStatus.ProcessScrap => [RadioRepairJobStatus.InProgress, RadioRepairJobStatus.Scrapped], // Supervisor can approve or reject
                RadioRepairJobStatus.Scrapped => [RadioRepairJobStatus.InProgress], // Supervisor can cancel scrap
                _ => []
            };
            if (!allowed.Contains(to))
                throw new InvalidOperationException($"Transisi status dari {from} ke {to} tidak diizinkan.");
        }

        private ValueTask<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<RadioRepairJobStatusLog>> AddStatusLogAsync(int jobId, RadioRepairJobStatus? from, RadioRepairJobStatus to, string? note, int userId, string? techName = null)
        {
            return _context.RadioRepairJobStatusLogs.AddAsync(new RadioRepairJobStatusLog
            {
                JobId = jobId,
                FromStatus = from,
                ToStatus = to,
                Note = note,
                UserId = userId,
                WorkshopTechnicianName = techName,
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
            IsWarranty = job.IsWarranty,
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
            AssignedTechnicianName = job.AssignedTechnician?.FullName ?? "Unknown",
            WorkshopTechnicianId = job.WorkshopTechnicianId,
            WorkshopTechnicianName = job.WorkshopTechnician?.Name,
            CustomStatusId = job.CustomStatusId,
            CustomStatusLabel = job.CustomStatus?.Label,
            CustomStatusColor = job.CustomStatus?.Color,
            OpenedByName = job.OpenedBy?.FullName ?? "Unknown",
            OpenedAt = job.OpenedAt,
            ClosedAt = job.ClosedAt,
            FirstInProgressAt = job.StatusLogs.Where(l => l.ToStatus == RadioRepairJobStatus.InProgress).OrderBy(l => l.At).Select(l => (DateTime?)l.At).FirstOrDefault(),
            WorkshopCompletedAt = job.StatusLogs.Where(l => l.ToStatus == RadioRepairJobStatus.RepairCompleted || l.ToStatus == RadioRepairJobStatus.ProcessScrap || l.ToStatus == RadioRepairJobStatus.HandedToWarehouse || l.ToStatus == RadioRepairJobStatus.ReturnedToHelpdesk || l.ToStatus == RadioRepairJobStatus.Scrapped).OrderBy(l => l.At).Select(l => (DateTime?)l.At).FirstOrDefault(),
            AccumulatedProgressDurationMinutes = job.AccumulatedProgressDurationMinutes,
            CurrentProgressStartedAt = job.CurrentProgressStartedAt,
            IsDeleted = job.IsDeleted,
            DeletedAt = job.DeletedAt,
            HasBorrowRequest = job.PartBorrows.Count > 0,
            HasActiveBorrowedPart = job.PartBorrows.Any(pb => pb.Status != WarehousePartBorrowStatus.Returned && pb.Status != WarehousePartBorrowStatus.Rejected),
            HasReturnedBorrowedPart = job.PartBorrows.Any(pb => pb.Status == WarehousePartBorrowStatus.Returned),
            PendingHandoverType = job.CurrentHandoverId.HasValue && job.CurrentHandoverId > 0
                ? job.Handovers.Where(h => h.Id == job.CurrentHandoverId && h.Status != "Completed").Select(h => h.HandoverType.ToString()).FirstOrDefault()
                : job.Handovers.Where(h => h.Status != "Completed" && !h.IsDeleted).OrderByDescending(h => h.Id).Select(h => h.HandoverType.ToString()).FirstOrDefault(),
            StatusLogs = [.. job.StatusLogs.OrderByDescending(l => l.At).Select(l => new RadioRepairJobStatusLogDto
            {
                Id = l.Id,
                FromStatus = l.FromStatus?.ToString(),
                ToStatus = l.ToStatus.ToString(),
                Note = l.Note,
                UserName = !string.IsNullOrEmpty(l.WorkshopTechnicianName)
                            ? $"{l.User?.FullName ?? l.User?.Username ?? "Unknown"} ({l.WorkshopTechnicianName})"
                            : l.User?.FullName ?? l.User?.Username ?? "Unknown",
                WorkshopTechnicianName = l.WorkshopTechnicianName,
                At = l.At
            })],
            Handovers = [.. job.Handovers
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
                HandedOverByName = h.HandedOverByWorkshopTechnician != null ? h.HandedOverByWorkshopTechnician.Name : h.HandedOverBy.FullName,
                ReceivedByName = h.WorkshopTechnician != null ? h.WorkshopTechnician.Name : h.ReceivedBy.FullName,
                ReceivedByUserId = h.ReceivedByUserId,
                HasRadioPhoto = !string.IsNullOrEmpty(h.RadioPhotoBase64),
                HasHandedOverSignature = !string.IsNullOrEmpty(h.HandedOverSignatureBase64),
                HasReceiverSignature = !string.IsNullOrEmpty(h.ReceiverSignatureBase64),
                Status = h.Status,
                Remarks = h.Remarks,
                PicReceiverName = h.PicReceiverName,
                IsPartial = h.IsPartial,
                ContainsMainRadioUnit = h.ContainsMainRadioUnit,
                Accessories = [.. h.Accessories.Select(a => a.Quantity + " " + a.Unit + " " + a.ItemName)]
            })],
            PrimaryHandover = job.Handovers
                .Where(h => !h.IsDeleted && h.HandoverType == RadioHandoverType.HelpdeskToTechnician)
                .OrderBy(h => h.HandoverAt)
                .Select(h => MapPrimaryHandover(h, job))
                .FirstOrDefault()
        };

        /// <summary>Isi ID Radio & Fleet dari master (FK atau lookup SN) jika belum terisi di DTO.</summary>
        private async Task EnrichRadioMasterFieldsAsync(List<RadioRepairJobListDto> items)
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
                : [];

            var allBySerial = serialKeys.Count > 0
                ? await _context.Radios.AsNoTracking()
                    .Where(r => r.SerialNumber != null)
                    .ToListAsync()
                : [];

            var serialMap = allBySerial
                .Where(r => serialKeys.Contains(r.SerialNumber!.Trim(), StringComparer.OrdinalIgnoreCase))
                .GroupBy(r => r.SerialNumber!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Id).First(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                Models.Radio? radio = null;
                if (item.RadioId.HasValue && radiosById.TryGetValue(item.RadioId.Value, out var byId))
                {
                    radio = byId;
                }
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
            HandedOverByName = h.HandedOverByWorkshopTechnician != null ? h.HandedOverByWorkshopTechnician.Name : h.HandedOverBy.FullName,
            ReceivedByName = h.WorkshopTechnician != null ? h.WorkshopTechnician.Name : h.ReceivedBy.FullName,
            Status = h.Status,
            EquipmentName = h.EquipmentName,
            UnitNumber = h.UnitNumber,
            RadioOwnerLabel = h.RadioOwnerLabel,
            OwnerDivision = h.OwnerDivision,
            OwnerDepartment = h.OwnerDepartment,
            RadioSerialNumber = h.RadioSerialNumber,
            BatterySerialNumber = h.BatterySerialNumber,
            DamageDescription = job.DamageDescription,
            Accessories = [.. h.Accessories.Select(a => new RadioRepairHandoverAccessoryDto
            {
                ItemName = string.IsNullOrWhiteSpace(a.ItemName) ? (a.AccessoryCode ?? "") : a.ItemName,
                Quantity = a.Quantity,
                Unit = a.Unit,
                Description = a.Description,
                SerialNumber = a.SerialNumber
            })]
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

        public async Task PurgeJobAsync(int jobId, int userId)
        {
            var job = await _context.RadioRepairJobs
                .Include(j => j.Handovers)
                    .ThenInclude(h => h.Accessories)
                .Include(j => j.Handovers)
                    .ThenInclude(h => h.Photos)
                .FirstOrDefaultAsync(j => j.Id == jobId)
                ?? throw new KeyNotFoundException("Pekerjaan perbaikan tidak ditemukan.");

            var ticket = job.HelpdeskTicketNumber;
            var serial = job.RadioSerialNumber;
            var handoverNumbers = job.Handovers.Select(h => h.HandoverNumber).ToList();

            // 1. Hapus semua aksesoris & foto dari handover terkait
            foreach (var h in job.Handovers)
            {
                _context.RadioHandoverAccessories.RemoveRange(h.Accessories);
                _context.RadioHandoverPhotos.RemoveRange(h.Photos);
            }

            // 2. Lepas referensi CurrentHandoverId agar tidak constraint error
            job.CurrentHandoverId = null;
            await _context.SaveChangesAsync();

            // 3. Hapus semua handover milik job ini
            _context.RadioHandovers.RemoveRange(job.Handovers);

            // 4. Hapus semua status log milik job ini
            var logs = await _context.RadioRepairJobStatusLogs
                .Where(l => l.JobId == jobId)
                .ToListAsync();
            _context.RadioRepairJobStatusLogs.RemoveRange(logs);

            // 5. Hapus job itu sendiri
            _context.RadioRepairJobs.Remove(job);

            // 6. Catat aktivitas
            await _activityLog.LogAsync("RadioRepairJob", jobId, "PurgeJob", userId,
                $"Hapus tuntas job tiket {ticket}, SN {serial}, STR: [{string.Join(", ", handoverNumbers)}]");

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");
        }
    }
}
