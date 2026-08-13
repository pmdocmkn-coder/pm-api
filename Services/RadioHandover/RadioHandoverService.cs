using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.RadioHandover;
using Pm.Enums;
using Pm.Helper;
using Pm.Models;
using Pm.Services;
using Pm.Services.Media;
using Pm.Services.RadioRepairJob;
using Pm.Services.Notification;
using Pm.DTOs.Notification;

namespace Pm.Services.RadioHandover
{
    public class RadioHandoverService(
        AppDbContext _context,
        IActivityLogService _activityLog,
        IImageBase64Validator _imageValidator,
        INotificationService _notificationService) : IRadioHandoverService
    {

        public async Task<PagedResultDto<RadioHandoverListDto>> GetAllAsync(
            RadioHandoverQueryDto query, int currentUserId, string? roleName)
        {
            var q = _context.RadioHandovers.AsNoTracking()
                .Include(h => h.HandedOverBy)
                .Include(h => h.ReceivedBy)
                .Include(h => h.WorkshopTechnician)
                .Include(h => h.HandedOverByWorkshopTechnician)
                .Include(h => h.RadioRepairJob)
                .Include(h => h.Photos)
                .AsQueryable();

            q = query.IncludeDeleted
                ? q.Where(h => h.IsDeleted)
                : q.Where(h => !h.IsDeleted);

            if (string.Equals(roleName, "Warehouse", StringComparison.OrdinalIgnoreCase))
            {
                // All Warehouse users see the same data (TekToWH + WHToHD)
                // TTD button visibility is controlled on the frontend by matching receivedByUserId
                q = q.Where(h => 
                    h.HandoverType == RadioHandoverType.TechnicianToWarehouse ||
                    h.HandoverType == RadioHandoverType.WarehouseToHelpdesk ||
                    h.HandoverType == RadioHandoverType.HelpdeskToWarehouse
                );
            }

            if (query.HandoverType.HasValue)
                q = q.Where(h => h.HandoverType == query.HandoverType);

            if (query.JobId.HasValue)
                q = q.Where(h => h.RadioRepairJobId == query.JobId);

            if (query.ReceivedByUserId.HasValue)
                q = q.Where(h => h.ReceivedByUserId == query.ReceivedByUserId);

            if (!string.IsNullOrEmpty(query.Status))
                q = q.Where(h => h.Status == query.Status);

            if (query.FromDate.HasValue)
                q = q.Where(h => h.HandoverAt >= query.FromDate.Value);
            if (query.ToDate.HasValue)
                q = q.Where(h => h.HandoverAt <= query.ToDate.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                // Avoid using StringComparison in Contains so EF Core can translate it to SQL LIKE
                q = q.Where(h =>
                    h.HandoverNumber.Contains(s) ||
                    h.RadioSerialNumber.Contains(s) ||
                    (h.RadioRepairJob != null && h.RadioRepairJob.HelpdeskTicketNumber != null && h.RadioRepairJob.HelpdeskTicketNumber.Contains(s)) ||
                    (h.NoJobErp != null && h.NoJobErp.Contains(s)) ||
                    (h.HandedOverBy != null && h.HandedOverBy.FullName != null && h.HandedOverBy.FullName.Contains(s)) ||
                    (h.ReceivedBy != null && h.ReceivedBy.FullName != null && h.ReceivedBy.FullName.Contains(s)) ||
                    (h.WorkshopTechnician != null && h.WorkshopTechnician.Name != null && h.WorkshopTechnician.Name.Contains(s)) ||
                    (h.HandedOverByWorkshopTechnician != null && h.HandedOverByWorkshopTechnician.Name != null && h.HandedOverByWorkshopTechnician.Name.Contains(s))
                );
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(h => h.Status == "PendingReceiverSignature")
                .ThenByDescending(h => h.HandoverAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(h => new RadioHandoverListDto
                {
                    Id = h.Id,
                    HandoverNumber = h.HandoverNumber,
                    HandoverType = h.HandoverType.ToString(),
                    RadioRepairJobId = h.RadioRepairJobId,
                    JobStatus = h.RadioRepairJob.Status.ToString(),
                    HelpdeskTicketNumber = h.RadioRepairJob!.HelpdeskTicketNumber,
                    NoJobErp = h.NoJobErp,
                    RadioSerialNumber = h.RadioSerialNumber,
                    EquipmentName = h.EquipmentName,
                    UnitNumber = h.UnitNumber,
                    RadioOwnerLabel = h.RadioOwnerLabel,
                    OwnerDivision = h.OwnerDivision,
                    OwnerDepartment = h.OwnerDepartment,
                    IsDeleted = h.IsDeleted,
                    DeletedAt = h.DeletedAt,
                    ReceivedByUserId = h.ReceivedByUserId,
                    HandedOverByName = h.HandedOverBy.FullName,
                    ReceivedByName = h.ReceivedBy.FullName,
                    WorkshopTechnicianId = h.WorkshopTechnicianId,
                    WorkshopTechnicianName = h.WorkshopTechnician != null ? h.WorkshopTechnician.Name : null,
                    HandedOverByWorkshopTechnicianId = h.HandedOverByWorkshopTechnicianId,
                    HandedOverByWorkshopTechnicianName = h.HandedOverByWorkshopTechnician != null ? h.HandedOverByWorkshopTechnician.Name : null,
                    HandoverAt = h.HandoverAt,
                    SignedAt = h.SignedAt,
                    EquipmentTagType = h.EquipmentTagType.ToString(),
                    HasRadioPhoto = h.RadioPhotoBase64 != null && h.RadioPhotoBase64.Length > 0,
                    HasHandedOverSignature = h.HandedOverSignatureBase64 != null && h.HandedOverSignatureBase64.Length > 0,
                    HasReceiverSignature = h.ReceiverSignatureBase64 != null && h.ReceiverSignatureBase64.Length > 0,
                    Status = h.Status,
                    PhotoCount = h.Photos.Count > 0 ? h.Photos.Count : (h.RadioPhotoBase64 != null ? 1 : 0),
                    PreviewPhotoBase64 = null, // loaded lazily via /thumbnail endpoint
                    PicReceiverName = h.PicReceiverName,
                    Remarks = h.Remarks,
                    IsPartial = h.IsPartial,
                    ContainsMainRadioUnit = h.ContainsMainRadioUnit,
                    IsScrap = h.RadioRepairJob != null && (h.RadioRepairJob.Status == RadioRepairJobStatus.ProcessScrap || h.RadioRepairJob.Status == RadioRepairJobStatus.Scrapped || h.RadioRepairJob.Handovers.Any(ho => ho.HandoverType == RadioHandoverType.TechnicianToHelpdesk)),
                    IsPendingScrapData = h.Radio != null && h.Radio.IsScrap && !h.Radio.DateScrapped.HasValue,
                    // Compute: apakah masih ada barang yang bisa diserahkan ke WH?
                    // True jika handover ini TechnicianToHelpdesk + Completed + belum SEMUA item diserahkan ke WH
                    HasRemainingItemsForWarehouse =
                        h.HandoverType == RadioHandoverType.TechnicianToHelpdesk &&
                        h.Status == "Completed" &&
                        h.RadioRepairJob != null &&
                        (
                            // Unit radio utama belum diserahkan ke WH?
                            !h.RadioRepairJob.Handovers.Any(ho =>
                                ho.HandoverType == RadioHandoverType.HelpdeskToWarehouse &&
                                !ho.IsDeleted &&
                                ho.ContainsMainRadioUnit)
                            ||
                            // Masih ada aksesoris yang belum diserahkan?
                            // Bandingkan jumlah aksesoris dari primary (TekToHD) vs yang sudah dikirim (HdToWH)
                            h.Accessories.Count >
                            h.RadioRepairJob.Handovers
                                .Where(ho => ho.HandoverType == RadioHandoverType.HelpdeskToWarehouse && !ho.IsDeleted)
                                .SelectMany(ho => ho.Accessories)
                                .Count()
                        )
                })
                .ToListAsync();

            return new PagedResultDto<RadioHandoverListDto>(items, query, total);
        }

        public async Task<RadioHandoverDetailDto?> GetByIdAsync(int id)
        {
            var h = await _context.RadioHandovers
                .Include(x => x.HandedOverBy)
                .Include(x => x.ReceivedBy)
                .Include(x => x.WorkshopTechnician)
                .Include(x => x.HandedOverByWorkshopTechnician)
                .Include(x => x.Radio)
                .Include(x => x.RadioRepairJob)
                .Include(x => x.Accessories)
                .Include(x => x.Photos)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == id);
            return h == null ? null : MapDetail(h);
        }

        public async Task<RadioHandoverDetailDto> CreateAsync(CreateRadioHandoverDto dto, int currentUserId)
        {
            var photos = ResolvePhotoList(dto);
            _imageValidator.ValidatePhotoList(photos, "Foto radio");
            _imageValidator.ValidateRequired(dto.HandedOverSignatureBase64, StoredImageKind.Signature, "TTD penyerah");

            if (dto.HandoverType == RadioHandoverType.HelpdeskToTechnician || 
                dto.HandoverType == RadioHandoverType.TechnicianToWarehouse ||
                dto.HandoverType == RadioHandoverType.WarehouseToHelpdesk ||
                dto.HandoverType == RadioHandoverType.TechnicianToHelpdesk ||
                dto.HandoverType == RadioHandoverType.HelpdeskToWarehouse)
            {
                if (!string.IsNullOrWhiteSpace(dto.ReceiverSignatureBase64))
                    _imageValidator.Validate(dto.ReceiverSignatureBase64, StoredImageKind.Signature, "TTD penerima");
            }
            else
            {
                _imageValidator.ValidateRequired(dto.ReceiverSignatureBase64, StoredImageKind.Signature, "TTD penerima");
            }

            return dto.HandoverType switch
            {
                RadioHandoverType.HelpdeskToTechnician => await CreateHelpdeskToTechnicianAsync(dto, photos, currentUserId),
                RadioHandoverType.TechnicianToWarehouse => await CreateTechnicianToWarehouseAsync(dto, photos, currentUserId),
                RadioHandoverType.WarehouseToHelpdesk => await CreateWarehouseToHelpdeskAsync(dto, photos, currentUserId),
                RadioHandoverType.TechnicianToHelpdesk => await CreateTechnicianToHelpdeskAsync(dto, photos, currentUserId),
                RadioHandoverType.HelpdeskToWarehouse => await CreateHelpdeskToWarehouseAsync(dto, photos, currentUserId),
                _ => throw new ArgumentException("Tipe serah terima tidak dikenal.")
            };
        }

        private static List<string> ResolvePhotoList(CreateRadioHandoverDto dto)
        {
            if (dto.RadioPhotos != null && dto.RadioPhotos.Count > 0)
                return [.. dto.RadioPhotos.Where(p => !string.IsNullOrWhiteSpace(p))];
            if (!string.IsNullOrWhiteSpace(dto.RadioPhotoBase64))
                return [dto.RadioPhotoBase64];
            return [];
        }

        private async Task<RadioHandoverDetailDto> CreateHelpdeskToTechnicianAsync(
            CreateRadioHandoverDto dto, List<string> photos, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.HelpdeskTicketNumber))
                throw new ArgumentException("No tiket helpdesk wajib diisi.");
            if (!dto.WorkshopTechnicianId.HasValue)
                throw new ArgumentException("Teknisi workshop wajib dipilih saat serah terima Helpdesk ke Teknisi.");
            ValidateTagFieldsForCreate(dto);

            await ValidateRadioSerialAsync(dto.RadioId, dto.RadioSerialNumber);
            await ValidateTechnicianReceiverAsync(dto.ReceivedByUserId);
            var equipment = await ResolveEquipmentFieldsAsync(dto);
            await RadioRepairJobService.ValidateDuplicateTicketSerialAsync(
                _context,
                dto.HelpdeskTicketNumber!.Trim(),
                dto.RadioSerialNumber.Trim());

            var ticket = dto.HelpdeskTicketNumber.Trim();
            var serial = dto.RadioSerialNumber.Trim();
            var strNumber = await DocumentNumberHelper.NextHandoverNumberAsync(_context);
            var now = DateTime.UtcNow;

            await SyncMasterRadioFieldsAsync(dto.RadioId, equipment, currentUserId, strNumber);

            var job = new Models.RadioRepairJob
            {
                JobNumber = RepairJobReference.InternalKey(ticket, serial),
                HelpdeskTicketNumber = ticket,
                RadioId = dto.RadioId,
                RadioSerialNumber = serial,
                BatterySerialNumber = dto.BatterySerialNumber?.Trim(),
                EquipmentName = equipment.EquipmentName,
                UnitNumber = equipment.UnitNumber,
                RadioOwnerLabel = equipment.RadioOwnerLabel,
                OwnerDivision = equipment.OwnerDivision,
                OwnerDepartment = equipment.OwnerDepartment,
                DamageDescription = ResolveJobDamageDescription(dto),
                EquipmentTagType = dto.EquipmentTagType,   // simpan tag type dari STR ke job
                IsWarranty = dto.IsWarranty,
                OriginFrom = dto.OriginFrom?.Trim(),
                RepairDataDescription = dto.RepairDataDescription?.Trim(),
                RepairedByName = dto.RepairedByName?.Trim(),
                Status = RadioRepairJobStatus.Received,
                AssignedTechnicianUserId = dto.ReceivedByUserId,
                WorkshopTechnicianId = dto.WorkshopTechnicianId,
                OpenedByUserId = currentUserId,
                OpenedAt = now,
                CreatedAt = now
            };
            _context.RadioRepairJobs.Add(job);
            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob"); // ← Dashboard Perbaikan

            _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
            {
                JobId = job.Id,
                FromStatus = null,
                ToStatus = RadioRepairJobStatus.Received,
                Note = $"Job dibuat dari serah terima HD→Tek (tag {(dto.EquipmentTagType == EquipmentTagType.Good ? "hijau" : "kuning")})",
                UserId = currentUserId,
                At = now
            });

            var receiverComplete = !string.IsNullOrWhiteSpace(dto.ReceiverSignatureBase64);
            var handover = BuildHandover(dto, photos, strNumber, job.Id, currentUserId, dto.ReceivedByUserId, now, receiverComplete, equipment);
            _context.RadioHandovers.Add(handover);
            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");

            job.CurrentHandoverId = handover.Id;
            job.UpdatedAt = now;

            if (job.RadioId.HasValue)
                await AddRepairOpenedHistoryAsync(job, handover, currentUserId);

            var statusNote = receiverComplete ? "lengkap" : "menunggu TTD teknisi";
            await _activityLog.LogAsync("RadioRepairJob", job.Id, "Create", currentUserId,
                $"Job dari HD→Tek tiket {job.HelpdeskTicketNumber}, SN {job.RadioSerialNumber}");
            await _activityLog.LogAsync("RadioHandover", handover.Id, "Create",
                currentUserId, $"STR {strNumber} HD→Tek ({statusNote}), tiket {job.HelpdeskTicketNumber}");

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob"); // ← Dashboard Perbaikan

            // Get Technician Name
            var technicianName = "Teknisi";
            if (dto.WorkshopTechnicianId.HasValue)
            {
                var techUser = await _context.WorkshopTechnicians.FindAsync(dto.WorkshopTechnicianId.Value);
                if (techUser != null) technicianName = techUser.Name;
            }

            // Trigger Notification ke Teknisi yang menerima (personal — hanya 1 orang)
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                RecipientUserId = dto.ReceivedByUserId,
                Title = "Radio Masuk Workshop — Untuk Anda",
                Message = $"Radio SN {serial} diserahkan ke Workshop dari Helpdesk (Tiket {ticket}). Anda ditunjuk sebagai penerima: {technicianName}.",
                Category = "handover",
                LinkUrl = "/radio-handover",
                ReferenceId = handover.Id,
                ReferenceType = "RadioHandover"
            });

            // Notif ke Helpdesk & Supv WKS via permission HD→Tek
            // excludeUserIds: skip teknisi penerima agar tidak dapat 2 notif (sudah dapat personal di atas)
            await _notificationService.CreateForPermissionAsync(
                Pm.Helper.NotificationPermissions.RadioHandoverHdTek,
                new CreateNotificationDto
                {
                    Title = "Radio Masuk Workshop",
                    Message = $"Radio SN {serial} diserahkan ke Workshop (Tiket {ticket}) untuk teknisi: {technicianName}. Menunggu TTD penerima.",
                    Category = "handover",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                },
                excludeUserIds: [dto.ReceivedByUserId, currentUserId] // skip teknisi penerima dan pembuat
            );

            return (await GetByIdAsync(handover.Id))!;
        }

        private async Task<RadioHandoverDetailDto> CreateTechnicianToWarehouseAsync(
            CreateRadioHandoverDto dto, List<string> photos, int currentUserId)
        {
            if (!dto.RadioRepairJobId.HasValue)
                throw new ArgumentException("RadioRepairJobId wajib untuk serah terima Tek→WH.");

            if (!dto.HandedOverByWorkshopTechnicianId.HasValue)
                throw new ArgumentException("Teknisi yang menyerahkan wajib dipilih saat serah terima Teknisi ke Warehouse.");

            var job = await _context.RadioRepairJobs.FirstOrDefaultAsync(j => j.Id == dto.RadioRepairJobId)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status != RadioRepairJobStatus.RepairCompleted && job.Status != RadioRepairJobStatus.Scrapped)
                throw new InvalidOperationException("Job harus berstatus RepairCompleted atau Scrapped.");

            var currentRole = await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.UserId == currentUserId)
                .Select(u => u.Role!.RoleName)
                .FirstOrDefaultAsync();
            if (!OperationalRoleNames.IsTechnicianRole(currentRole))
                throw new UnauthorizedAccessException("Hanya user dengan role teknisi yang dapat serah terima ke warehouse.");

            var pendingHandover = await _context.RadioHandovers.AnyAsync(h => 
                h.RadioRepairJobId == job.Id && 
                h.HandoverType == RadioHandoverType.TechnicianToWarehouse && 
                h.Status == "PendingReceiverSignature" && 
                !h.IsDeleted);
            if (pendingHandover)
                throw new InvalidOperationException("Masih ada serah terima ke Warehouse yang menunggu tanda tangan penerima.");

            await ValidateUserRoleAsync(dto.ReceivedByUserId, OperationalRoleNames.Warehouse);

            var strNumber = await DocumentNumberHelper.NextHandoverNumberAsync(_context);
            var now = DateTime.UtcNow;

            await ApplyInheritedTagFieldsAsync(dto, job.Id, RadioHandoverType.TechnicianToWarehouse);

            var isReceiverSignatureComplete = !string.IsNullOrWhiteSpace(dto.ReceiverSignatureBase64);
            var handover = BuildHandover(dto, photos, strNumber, job.Id, currentUserId, dto.ReceivedByUserId, now, isReceiverSignatureComplete);
            handover.RadioId = job.RadioId ?? dto.RadioId;
            handover.RadioSerialNumber = job.RadioSerialNumber;
            handover.BatterySerialNumber = job.BatterySerialNumber ?? dto.BatterySerialNumber;

            _context.RadioHandovers.Add(handover);
            await _context.SaveChangesAsync();

            job.CurrentHandoverId = handover.Id;
            job.UpdatedAt = now;

            if (isReceiverSignatureComplete)
            {
                var fromStatus = job.Status;
                job.Status = RadioRepairJobStatus.HandedToWarehouse;

                _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
                {
                    JobId = job.Id,
                    FromStatus = fromStatus,
                    ToStatus = RadioRepairJobStatus.HandedToWarehouse,
                    Note = $"Serah terima {strNumber}",
                    UserId = currentUserId,
                    At = now
                });
            }

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob"); // ← Dashboard Perbaikan

            if (job.RadioId.HasValue)
                await AddRepairWarehouseHistoryAsync(job, handover, currentUserId);

            await _activityLog.LogAsync("RadioHandover", handover.Id, "Create",
                currentUserId, $"STR {strNumber} Tek→WH, tiket {job.HelpdeskTicketNumber}");

            await _context.SaveChangesAsync();

            var technicianName = "Teknisi";
            if (dto.HandedOverByWorkshopTechnicianId.HasValue)
            {
                var tech = await _context.WorkshopTechnicians.FindAsync(dto.HandedOverByWorkshopTechnicianId.Value);
                if (tech != null) technicianName = tech.Name;
            }

            if (!isReceiverSignatureComplete)
            {
                // Notif untuk TTD
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = dto.ReceivedByUserId,
                    Title = "Tanda Tangan Serah Terima",
                    Message = $"Anda ditunjuk sebagai penerima untuk STR {strNumber} (SN: {job.RadioSerialNumber}). Mohon lengkapi tanda tangan Anda.",
                    Category = "handover",
                    LinkUrl = "/radio-handover/warehouse",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });
            }
            else
            {
                // Notif ke Warehouse & Supv WKS via permission Tek→WH
                await _notificationService.CreateForPermissionAsync(Pm.Helper.NotificationPermissions.RadioHandoverTekWh, new CreateNotificationDto
                {
                    Title = "Radio Masuk Warehouse",
                    Message = $"Radio SN {job.RadioSerialNumber} telah diserahkan oleh Teknisi {technicianName} ke Warehouse. Menunggu serah terima ke Helpdesk.",
                    Category = "handover",
                    LinkUrl = "/radio-handover/warehouse",
                    ReferenceId = handover.Id,       // handover.Id bukan job.Id — agar ?handoverId= bisa buka detail yang benar
                    ReferenceType = "RadioHandover"
                });
                
                // Notif ke Helpdesk (role fix)
                await _notificationService.CreateForRoleAsync(Pm.Helper.OperationalRoleNames.Helpdesk, new CreateNotificationDto
                {
                    Title = "Radio Diserahkan ke Warehouse",
                    Message = $"Radio SN {job.RadioSerialNumber} telah diserahkan oleh Teknisi {technicianName} ke Warehouse.",
                    Category = "handover",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }

            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            return (await GetByIdAsync(handover.Id))!;
        }

        private async Task<RadioHandoverDetailDto> CreateTechnicianToHelpdeskAsync(
            CreateRadioHandoverDto dto, List<string> photos, int currentUserId)
        {
            if (!dto.RadioRepairJobId.HasValue)
                throw new ArgumentException("RadioRepairJobId wajib untuk serah terima Tek→HD.");

            if (!dto.HandedOverByWorkshopTechnicianId.HasValue)
                throw new ArgumentException("Teknisi yang menyerahkan wajib dipilih saat serah terima Teknisi ke Helpdesk.");

            var job = await _context.RadioRepairJobs.FirstOrDefaultAsync(j => j.Id == dto.RadioRepairJobId)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status != RadioRepairJobStatus.Scrapped)
                throw new InvalidOperationException("Job harus berstatus Scrapped.");

            var currentRole = await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.UserId == currentUserId)
                .Select(u => u.Role!.RoleName)
                .FirstOrDefaultAsync();
            if (!OperationalRoleNames.IsTechnicianRole(currentRole))
                throw new UnauthorizedAccessException("Hanya user dengan role teknisi yang dapat serah terima ke Helpdesk.");

            var pendingHandover = await _context.RadioHandovers.AnyAsync(h => 
                h.RadioRepairJobId == job.Id && 
                h.HandoverType == RadioHandoverType.TechnicianToHelpdesk && 
                h.Status == "PendingReceiverSignature" && 
                !h.IsDeleted);
            if (pendingHandover)
                throw new InvalidOperationException("Masih ada serah terima ke Helpdesk yang menunggu tanda tangan penerima.");

            await ValidateUserRoleAsync(dto.ReceivedByUserId, OperationalRoleNames.Helpdesk);

            var strNumber = await DocumentNumberHelper.NextHandoverNumberAsync(_context);
            var now = DateTime.UtcNow;

            await ApplyInheritedTagFieldsAsync(dto, job.Id, RadioHandoverType.TechnicianToHelpdesk);

            var isReceiverSignatureComplete = !string.IsNullOrWhiteSpace(dto.ReceiverSignatureBase64);
            var handover = BuildHandover(dto, photos, strNumber, job.Id, currentUserId, dto.ReceivedByUserId, now, isReceiverSignatureComplete);
            handover.RadioId = job.RadioId ?? dto.RadioId;
            handover.RadioSerialNumber = job.RadioSerialNumber;
            handover.BatterySerialNumber = job.BatterySerialNumber ?? dto.BatterySerialNumber;

            _context.RadioHandovers.Add(handover);
            await _context.SaveChangesAsync();

            job.CurrentHandoverId = handover.Id;
            job.UpdatedAt = now;

            if (isReceiverSignatureComplete)
            {
                var fromStatus = job.Status;
                job.Status = RadioRepairJobStatus.ReturnedToHelpdesk;

                _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
                {
                    JobId = job.Id,
                    FromStatus = fromStatus,
                    ToStatus = RadioRepairJobStatus.ReturnedToHelpdesk,
                    Note = $"Serah terima scrap {strNumber}",
                    UserId = currentUserId,
                    At = now
                });
            }

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob"); // ← Dashboard Perbaikan

            if (job.RadioId.HasValue)
                await AddRepairReturnedToHelpdeskHistoryAsync(job, handover, currentUserId);

            await _activityLog.LogAsync("RadioHandover", handover.Id, "Create",
                currentUserId, $"STR {strNumber} Tek→HD, tiket {job.HelpdeskTicketNumber}");

            await _context.SaveChangesAsync();

            var technicianName = "Teknisi";
            if (dto.HandedOverByWorkshopTechnicianId.HasValue)
            {
                var tech = await _context.WorkshopTechnicians.FindAsync(dto.HandedOverByWorkshopTechnicianId.Value);
                if (tech != null) technicianName = tech.Name;
            }

            if (!isReceiverSignatureComplete)
            {
                // Notif untuk TTD
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = dto.ReceivedByUserId,
                    Title = "Tanda Tangan Serah Terima",
                    Message = $"Anda ditunjuk sebagai penerima untuk STR {strNumber} (SN: {job.RadioSerialNumber}). Mohon lengkapi tanda tangan Anda.",
                    Category = "handover",
                    LinkUrl = "/radio-handover?tab=incoming",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });
            }
            else
            {
                // Notif ke Helpdesk & Supv WKS via permission Tek→HD
                await _notificationService.CreateForPermissionAsync(Pm.Helper.NotificationPermissions.RadioHandoverWhHd, new CreateNotificationDto
                {
                    Title = "Radio Scrap Dikembalikan ke Helpdesk",
                    Message = $"Radio SN {job.RadioSerialNumber} telah diserahkan oleh Teknisi {technicianName} ke Helpdesk.",
                    Category = "handover",
                    LinkUrl = "/radio-handover?tab=incoming",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });
            }

            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            return (await GetByIdAsync(handover.Id))!;
        }

        private async Task<RadioHandoverDetailDto> CreateHelpdeskToWarehouseAsync(
            CreateRadioHandoverDto dto, List<string> photos, int currentUserId)
        {
            if (!dto.RadioRepairJobId.HasValue)
                throw new ArgumentException("RadioRepairJobId wajib untuk serah terima HD→WH.");

            var job = await _context.RadioRepairJobs
                .Include(j => j.Radio)
                .FirstOrDefaultAsync(j => j.Id == dto.RadioRepairJobId)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status != RadioRepairJobStatus.ReturnedToHelpdesk 
                && job.Status != RadioRepairJobStatus.Scrapped
                && job.Status != RadioRepairJobStatus.HandedToWarehouse)
                throw new InvalidOperationException("Job harus berstatus ReturnedToHelpdesk, Scrapped, atau HandedToWarehouse (untuk sisa aksesoris).");

            if (job.Radio != null && job.Radio.IsScrap && !job.Radio.DateScrapped.HasValue)
                throw new InvalidOperationException("Radio Scrap belum memiliki data scrap yang lengkap. Harap lengkapi terlebih dahulu sebelum menyerahkan ke Warehouse.");

            var pendingHandover = await _context.RadioHandovers.AnyAsync(h => 
                h.RadioRepairJobId == job.Id && 
                h.HandoverType == RadioHandoverType.HelpdeskToWarehouse && 
                h.Status == "PendingReceiverSignature" && 
                !h.IsDeleted);
            if (pendingHandover)
                throw new InvalidOperationException("Masih ada serah terima ke Warehouse yang menunggu tanda tangan penerima.");

            await ValidateUserRoleAsync(currentUserId, OperationalRoleNames.Helpdesk);
            await ValidateUserRoleAsync(dto.ReceivedByUserId, OperationalRoleNames.Warehouse);

            var strNumber = await DocumentNumberHelper.NextHandoverNumberAsync(_context);
            var now = DateTime.UtcNow;

            await ApplyInheritedTagFieldsAsync(dto, job.Id, RadioHandoverType.HelpdeskToWarehouse);

            if (job.Status == RadioRepairJobStatus.Scrapped || job.Status == RadioRepairJobStatus.ProcessScrap || (job.Radio != null && job.Radio.IsScrap))
            {
                dto.EquipmentTagType = EquipmentTagType.Damaged;
            }

            var isReceiverSignatureComplete = !string.IsNullOrWhiteSpace(dto.ReceiverSignatureBase64);
            var handover = BuildHandover(dto, photos, strNumber, job.Id, currentUserId, dto.ReceivedByUserId, now, isReceiverSignatureComplete);
            handover.HandoverType = RadioHandoverType.HelpdeskToWarehouse;
            handover.RadioId = job.RadioId ?? dto.RadioId;
            handover.RadioSerialNumber = job.RadioSerialNumber;
            handover.BatterySerialNumber = job.BatterySerialNumber ?? dto.BatterySerialNumber;

            _context.RadioHandovers.Add(handover);
            await _context.SaveChangesAsync();

            job.CurrentHandoverId = handover.Id;
            job.UpdatedAt = now;

            if (isReceiverSignatureComplete)
            {
                var fromStatus = job.Status;

                // Hanya ubah status ke HandedToWarehouse jika unit radio utama ikut diserahkan.
                // Jika parsial (aksesoris saja), status tetap Scrapped agar handover berikutnya bisa dilakukan.
                if (handover.ContainsMainRadioUnit)
                {
                    job.Status = RadioRepairJobStatus.HandedToWarehouse;
                }

                _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
                {
                    JobId = job.Id,
                    FromStatus = fromStatus,
                    ToStatus = job.Status,
                    Note = handover.ContainsMainRadioUnit
                        ? $"Serah terima scrap ke warehouse {strNumber}"
                        : $"Serah terima aksesoris scrap ke warehouse {strNumber} (Parsial)",
                    UserId = currentUserId,
                    At = now
                });
            }

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob"); // ← Dashboard Perbaikan

            await _activityLog.LogAsync("RadioHandover", handover.Id, "Create",
                currentUserId, $"STR {strNumber} HD→WH, tiket {job.HelpdeskTicketNumber}");

            await _context.SaveChangesAsync();

            if (!isReceiverSignatureComplete)
            {
                // Notif untuk TTD
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = dto.ReceivedByUserId,
                    Title = "Tanda Tangan Serah Terima",
                    Message = $"Anda ditunjuk sebagai penerima untuk STR {strNumber} (SN: {job.RadioSerialNumber}). Mohon lengkapi tanda tangan Anda.",
                    Category = "handover",
                    LinkUrl = "/radio-handover/warehouse",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });
            }
            else
            {
                await _notificationService.CreateForPermissionAsync(Pm.Helper.NotificationPermissions.RadioHandoverWhHd, new CreateNotificationDto
                {
                    Title = "Radio Masuk Warehouse",
                    Message = $"Radio SN {job.RadioSerialNumber} telah diserahkan oleh Helpdesk ke Warehouse.",
                    Category = "handover",
                    LinkUrl = "/radio-handover/warehouse",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });
            }

            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            return (await GetByIdAsync(handover.Id))!;
        }

        private async Task<RadioHandoverDetailDto> CreateWarehouseToHelpdeskAsync(
            CreateRadioHandoverDto dto, List<string> photos, int currentUserId)
        {
            if (!dto.RadioRepairJobId.HasValue)
                throw new ArgumentException("RadioRepairJobId wajib untuk serah terima WH→Helpdesk.");

            var job = await _context.RadioRepairJobs.FirstOrDefaultAsync(j => j.Id == dto.RadioRepairJobId)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status != RadioRepairJobStatus.HandedToWarehouse)
                throw new InvalidOperationException("Job harus berstatus HandedToWarehouse (sudah diterima dari teknisi).");

            var alreadyReturned = await _context.RadioHandovers.AnyAsync(h =>
                h.RadioRepairJobId == job.Id && h.HandoverType == RadioHandoverType.WarehouseToHelpdesk && h.ContainsMainRadioUnit);
            if (alreadyReturned)
                throw new InvalidOperationException("Unit Radio Utama sudah diserahkan ke Helpdesk pada serah terima sebelumnya.");

            await ValidateUserRoleAsync(currentUserId, OperationalRoleNames.Warehouse);
            await ValidateUserRoleAsync(dto.ReceivedByUserId, OperationalRoleNames.Helpdesk);

            var strNumber = await DocumentNumberHelper.NextHandoverNumberAsync(_context);
            var now = DateTime.UtcNow;

            bool isScrap = job.Status == RadioRepairJobStatus.Scrapped || job.Status == RadioRepairJobStatus.ProcessScrap || (job.Radio != null && job.Radio.IsScrap) || await _context.RadioHandovers.AnyAsync(h => h.RadioRepairJobId == job.Id && h.HandoverType == RadioHandoverType.TechnicianToHelpdesk);

            if (isScrap)
            {
                dto.EquipmentTagType = EquipmentTagType.Damaged;
            }
            else if (dto.EquipmentTagType != EquipmentTagType.Damaged)
            {
                dto.EquipmentTagType = EquipmentTagType.Good;
            }

            await ApplyInheritedTagFieldsAsync(dto, job.Id, RadioHandoverType.WarehouseToHelpdesk);

            var receiverComplete = !string.IsNullOrWhiteSpace(dto.ReceiverSignatureBase64);
            var handover = BuildHandover(dto, photos, strNumber, job.Id, currentUserId, dto.ReceivedByUserId, now, receiverComplete);
            handover.HandoverType = RadioHandoverType.WarehouseToHelpdesk;
            handover.RadioId = job.RadioId ?? dto.RadioId;
            handover.RadioSerialNumber = job.RadioSerialNumber;
            handover.BatterySerialNumber = job.BatterySerialNumber ?? dto.BatterySerialNumber;

            _context.RadioHandovers.Add(handover);
            await _context.SaveChangesAsync();

            job.CurrentHandoverId = handover.Id;
            job.UpdatedAt = now;

            if (receiverComplete)
            {
                var fromStatus = job.Status;
                if (dto.ContainsMainRadioUnit)
                {
                    job.Status = RadioRepairJobStatus.ReturnedToHelpdesk;
                    job.ClosedAt = now;
                }

                _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
                {
                    JobId = job.Id,
                    FromStatus = fromStatus,
                    ToStatus = job.Status,
                    Note = $"Serah terima {strNumber} ke Helpdesk{(dto.ContainsMainRadioUnit ? "" : " (Parsial/Aksesoris)")}",
                    UserId = currentUserId,
                    At = now
                });
            }

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob"); // ← Dashboard Perbaikan

            if (job.RadioId.HasValue)
                await AddRepairReturnedToHelpdeskHistoryAsync(job, handover, currentUserId);

            await _activityLog.LogAsync("RadioHandover", handover.Id, "Create",
                currentUserId, $"STR {strNumber} WH→HD ({(receiverComplete ? "lengkap" : "menunggu TTD Helpdesk")}), tiket {job.HelpdeskTicketNumber}");

            await _context.SaveChangesAsync();

            var warehouseUser = await _context.Users.FindAsync(currentUserId);
            var warehouseName = warehouseUser?.FullName ?? "Warehouse";

            if (!receiverComplete)
            {
                // Notif untuk TTD
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = dto.ReceivedByUserId,
                    Title = "Tanda Tangan Serah Terima",
                    Message = $"Anda ditunjuk sebagai penerima untuk STR {strNumber} (SN: {job.RadioSerialNumber}). Mohon lengkapi tanda tangan Anda.",
                    Category = "handover",
                    LinkUrl = "/radio-handover/warehouse?tab=outgoing",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });
            }

            // Notif ke Helpdesk, Warehouse & Supv WKS via permission WH→HD
            await _notificationService.CreateForPermissionAsync(Pm.Helper.NotificationPermissions.RadioHandoverWhHd, new CreateNotificationDto
            {
                Title = "Radio Diserahkan ke Helpdesk",
                Message = $"Radio SN {job.RadioSerialNumber} telah diserahkan dari Warehouse ke Helpdesk oleh {warehouseName}. {(receiverComplete ? "Proses perbaikan selesai." : "Menunggu TTD Helpdesk penerima.")}",
                Category = "handover",
                LinkUrl = "/radio-handover/warehouse?tab=outgoing",
                ReferenceId = handover.Id,      // handover.Id agar ?handoverId= buka detail yang benar
                ReferenceType = "RadioHandover"
            }, excludeUserIds: [currentUserId, dto.ReceivedByUserId]);

            // Notif ke Teknisi workshop yang mengerjakan radio ini
            if (job.AssignedTechnicianUserId != 0)
            {
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = job.AssignedTechnicianUserId,
                    Title = "Radio Sudah Kembali ke Helpdesk",
                    Message = $"Radio SN {job.RadioSerialNumber} yang Anda kerjakan telah diserahkan kembali ke Helpdesk oleh Warehouse. Proses selesai.",
                    Category = "repair",             // category repair → navigasi ke /radio-repair-dashboard?jobId=
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
                });
            }

            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob"); // ← Dashboard Perbaikan
            return (await GetByIdAsync(handover.Id))!;
        }

        public async Task<RadioHandoverDetailDto> CompleteReceiverSignatureAsync(
            int id, CompleteReceiverSignatureDto dto, int currentUserId)
        {
            _imageValidator.ValidateRequired(dto.ReceiverSignatureBase64, StoredImageKind.Signature, "TTD penerima");

            var handover = await _context.RadioHandovers
                .Include(h => h.RadioRepairJob)
                .FirstOrDefaultAsync(h => h.Id == id)
                ?? throw new KeyNotFoundException("Serah terima tidak ditemukan.");

            if (handover.Status == "Completed")
                throw new InvalidOperationException("Serah terima sudah selesai.");

            if (handover.ReceivedByUserId != currentUserId && handover.HandedOverByUserId != currentUserId)
                throw new UnauthorizedAccessException("Hanya pengguna yang ditunjuk sebagai penerima atau penyerah yang dapat melengkapi TTD.");

            var now = DateTime.UtcNow;
            handover.ReceiverSignatureBase64 = dto.ReceiverSignatureBase64;
            handover.PicReceiverName = dto.PicReceiverName;
            handover.Remarks = dto.Remarks;
            handover.Status = "Completed";
            handover.SignedAt = now;
            handover.UpdatedAt = now;

            if (handover.RadioRepairJob != null)
            {
                handover.RadioRepairJob.UpdatedAt = now;
                var oldStatus = handover.RadioRepairJob.Status;

                if (handover.HandoverType == RadioHandoverType.TechnicianToWarehouse)
                {
                    handover.RadioRepairJob.Status = RadioRepairJobStatus.HandedToWarehouse;
                }
                else if (handover.HandoverType == RadioHandoverType.WarehouseToHelpdesk)
                {
                    if (handover.ContainsMainRadioUnit)
                    {
                        handover.RadioRepairJob.Status = RadioRepairJobStatus.ReturnedToHelpdesk;
                        handover.RadioRepairJob.ClosedAt = now;
                    }
                }
                else if (handover.HandoverType == RadioHandoverType.HelpdeskToWarehouse)
                {
                    if (handover.ContainsMainRadioUnit)
                    {
                        handover.RadioRepairJob.Status = RadioRepairJobStatus.HandedToWarehouse;
                    }
                }
                else if (handover.HandoverType == RadioHandoverType.TechnicianToHelpdesk)
                {
                    handover.RadioRepairJob.Status = RadioRepairJobStatus.ReturnedToHelpdesk;
                }

                string statusNote = handover.HandoverType switch
                {
                    RadioHandoverType.HelpdeskToTechnician => "Teknisi melengkapi TTD penerima (Radio diterima, menunggu assign teknisi)",
                    RadioHandoverType.TechnicianToWarehouse => "Warehouse melengkapi TTD penerima",
                    RadioHandoverType.WarehouseToHelpdesk => "Helpdesk melengkapi TTD penerima",
                    RadioHandoverType.HelpdeskToWarehouse => "Warehouse melengkapi TTD penerima (Radio scrap dari Helpdesk)",
                    RadioHandoverType.TechnicianToHelpdesk => "Helpdesk melengkapi TTD penerima (Radio scrap dari Teknisi)",
                    _ => "TTD penerima dilengkapi"
                };

                _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
                {
                    JobId = handover.RadioRepairJob.Id,
                    FromStatus = oldStatus,
                    ToStatus = handover.RadioRepairJob.Status,
                    Note = statusNote,
                    UserId = currentUserId,
                    At = now
                });
            }

            await _activityLog.LogAsync("RadioHandover", handover.Id, "CompleteReceiver",
                currentUserId, $"STR {handover.HandoverNumber} — TTD penerima dilengkapi");

            await _context.SaveChangesAsync();

            // Broadcast refresh ke semua halaman yang relevan
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");

            // Ambil info untuk notifikasi
            var serial = handover.RadioRepairJob?.RadioSerialNumber ?? "-";
            var strNumber = handover.HandoverNumber ?? $"#{handover.Id}";
            var receiverName = (await _context.Users.FindAsync(currentUserId))?.FullName ?? "Penerima";

            if (handover.HandoverType == RadioHandoverType.HelpdeskToTechnician)
            {
                // Notif ke Helpdesk
                await _notificationService.CreateForRoleAsync(OperationalRoleNames.Helpdesk, new CreateNotificationDto
                {
                    Title = "TTD Penerima Lengkap",
                    Message = $"Teknisi sudah menandatangani STR {strNumber} (SN: {serial}). Radio diterima di Workshop.",
                    Category = "handover",
                    LinkUrl = "/radio-handover",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });

                var helpdeskUserIds = await _context.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role != null && u.Role.RoleName == OperationalRoleNames.Helpdesk)
                    .Select(u => u.UserId)
                    .ToListAsync();

                var excludedFromPermission = new List<int> { currentUserId };
                excludedFromPermission.AddRange(helpdeskUserIds);

                await _notificationService.CreateForPermissionAsync(
                    Pm.Helper.NotificationPermissions.RadioHandoverHdTek,
                    new CreateNotificationDto
                    {
                        Title = "Radio Siap Dikerjakan",
                        Message = $"STR {strNumber} (SN: {serial}) sudah ditandatangani kedua pihak. Radio menunggu di Workshop.",
                        Category = "handover",
                        LinkUrl = "/radio-repair-dashboard",
                        ReferenceId = handover.Id,
                        ReferenceType = "RadioHandover"
                    },
                    excludeUserIds: excludedFromPermission
                );
            }
            else if (handover.HandoverType == RadioHandoverType.TechnicianToWarehouse)
            {
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = handover.HandedOverByUserId,
                    Title = "TTD Penerima Lengkap",
                    Message = $"Warehouse ({receiverName}) sudah menandatangani STR {strNumber} (SN: {serial}).",
                    Category = "handover",
                    LinkUrl = "/radio-handover/warehouse",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });
            }
            else if (handover.HandoverType == RadioHandoverType.WarehouseToHelpdesk)
            {
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = handover.HandedOverByUserId,
                    Title = "TTD Penerima Lengkap",
                    Message = $"Helpdesk ({receiverName}) sudah menandatangani STR {strNumber} (SN: {serial}). Proses perbaikan selesai.",
                    Category = "handover",
                    LinkUrl = "/radio-handover",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });
            }
            else if (handover.HandoverType == RadioHandoverType.TechnicianToHelpdesk)
            {
                // Notif ke teknisi penyerah bahwa Helpdesk sudah TTD (radio scrap diterima)
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = handover.HandedOverByUserId,
                    Title = "TTD Penerima Lengkap (Scrap)",
                    Message = $"Helpdesk ({receiverName}) sudah menandatangani STR {strNumber} (SN: {serial}). Radio scrap diterima oleh Helpdesk.",
                    Category = "handover",
                    LinkUrl = "/radio-repair-dashboard",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });
            }
            else if (handover.HandoverType == RadioHandoverType.HelpdeskToWarehouse)
            {
                // Notif ke helpdesk penyerah bahwa Warehouse sudah TTD (radio scrap masuk WH)
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = handover.HandedOverByUserId,
                    Title = "TTD Penerima Lengkap (Scrap)",
                    Message = $"Warehouse ({receiverName}) sudah menandatangani STR {strNumber} (SN: {serial}). Radio scrap masuk Warehouse.",
                    Category = "handover",
                    LinkUrl = "/radio-handover",
                    ReferenceId = handover.Id,
                    ReferenceType = "RadioHandover"
                });
            }

            return (await GetByIdAsync(handover.Id))!;
        }

        public async Task<RadioHandoverDetailDto> ResetReceiverSignatureAsync(int id, int currentUserId)
        {
            await ValidateUserRoleAsync(currentUserId, OperationalRoleNames.Warehouse);

            var handover = await _context.RadioHandovers
                .Include(h => h.RadioRepairJob)
                .FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted)
                ?? throw new KeyNotFoundException("Serah terima tidak ditemukan.");

            if (handover.Status != "Completed")
                throw new InvalidOperationException("Serah terima belum selesai, tidak dapat dibatalkan.");

            if (handover.HandoverType != RadioHandoverType.WarehouseToHelpdesk)
                throw new InvalidOperationException("Hanya serah terima dari Warehouse ke Helpdesk yang dapat direset oleh Warehouse.");

            var now = DateTime.UtcNow;

            handover.ReceiverSignatureBase64 = null;
            handover.SignedAt = null;
            handover.Status = "PendingReceiverSignature";
            handover.UpdatedAt = now;

            if (handover.RadioRepairJob != null && handover.ContainsMainRadioUnit)
            {
                var oldStatus = handover.RadioRepairJob.Status;
                handover.RadioRepairJob.Status = RadioRepairJobStatus.HandedToWarehouse;
                handover.RadioRepairJob.ClosedAt = null;
                handover.RadioRepairJob.UpdatedAt = now;

                _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
                {
                    JobId = handover.RadioRepairJob.Id,
                    FromStatus = oldStatus,
                    ToStatus = RadioRepairJobStatus.HandedToWarehouse,
                    Note = "Status Done dibatalkan, menunggu TTD ulang dari Helpdesk",
                    UserId = currentUserId,
                    At = now
                });
            }

            await _activityLog.LogAsync("RadioHandover", handover.Id, "ResetReceiverSignature",
                currentUserId, $"STR {handover.HandoverNumber} — TTD penerima direset oleh Warehouse");

            await _context.SaveChangesAsync();

            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");

            return (await GetByIdAsync(id))!;
        }

        private sealed record EquipmentSnapshot(
            string EquipmentName,
            string? UnitNumber,
            string? RadioOwnerLabel,
            string? OwnerDivision,
            string? OwnerDepartment);

        private async Task<EquipmentSnapshot> ResolveEquipmentFieldsAsync(CreateRadioHandoverDto dto)
        {
            var manualName = dto.EquipmentName?.Trim();
            var manualUnit = dto.UnitNumber?.Trim();
            var manualOwner = dto.RadioOwnerLabel?.Trim();
            var manualDiv = dto.OwnerDivision?.Trim();
            var manualDept = dto.OwnerDepartment?.Trim();

            if (dto.RadioId.HasValue)
            {
                var radio = await _context.Radios.AsNoTracking().FirstOrDefaultAsync(r => r.Id == dto.RadioId)
                    ?? throw new KeyNotFoundException("Radio tidak ditemukan.");
                var name = !string.IsNullOrWhiteSpace(manualName) ? manualName : radio.Type?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = "Radio";
                var unit = !string.IsNullOrWhiteSpace(manualUnit) ? manualUnit : radio.NomorUnit?.Trim();
                var owner = !string.IsNullOrWhiteSpace(manualOwner) ? manualOwner : FormatRadioOwnerLabel(radio);
                var div = !string.IsNullOrWhiteSpace(manualDiv) ? manualDiv : radio.Division?.Trim();
                var dept = !string.IsNullOrWhiteSpace(manualDept) ? manualDept : radio.Department?.Trim();
                return new EquipmentSnapshot(name, unit, owner, div, dept);
            }

            if (string.IsNullOrWhiteSpace(manualName))
                throw new ArgumentException("Tipe/nama alat wajib diisi jika SN belum terdaftar di master radio.");

            return new EquipmentSnapshot(
                manualName,
                string.IsNullOrWhiteSpace(manualUnit) ? null : manualUnit,
                string.IsNullOrWhiteSpace(manualOwner) ? null : manualOwner,
                string.IsNullOrWhiteSpace(manualDiv) ? null : manualDiv,
                string.IsNullOrWhiteSpace(manualDept) ? null : manualDept);
        }

        private static string? FormatRadioOwnerLabel(Models.Radio radio)
        {
            if (!string.IsNullOrWhiteSpace(radio.Company)) return radio.Company.Trim();
            return radio.Category;
        }

        private static Models.RadioHandover BuildHandover(
            CreateRadioHandoverDto dto, List<string> photos, string strNumber, int jobId,
            int handedOverByUserId, int receivedByUserId, DateTime now, bool receiverSignatureComplete,
            EquipmentSnapshot? equipment = null)
        {
            var handover = new Models.RadioHandover
            {
                HandoverNumber = strNumber,
                HandoverType = dto.HandoverType,
                RadioRepairJobId = jobId,
                RadioId = dto.RadioId,
                RadioSerialNumber = dto.RadioSerialNumber.Trim(),
                BatterySerialNumber = dto.BatterySerialNumber?.Trim(),
                NoJobErp = dto.NoJobErp?.Trim(),
                EquipmentName = equipment?.EquipmentName,
                UnitNumber = equipment?.UnitNumber,
                RadioOwnerLabel = equipment?.RadioOwnerLabel,
                OwnerDivision = equipment?.OwnerDivision,
                OwnerDepartment = equipment?.OwnerDepartment,
                RadioPhotoBase64 = photos.FirstOrDefault(),
                HandedOverSignatureBase64 = dto.HandedOverSignatureBase64,
                ReceiverSignatureBase64 = dto.ReceiverSignatureBase64,
                Remarks = dto.Remarks?.Trim(),
                PicReceiverName = dto.PicReceiverName?.Trim(),
                HandedOverByUserId = handedOverByUserId,
                ReceivedByUserId = receivedByUserId,
                WorkshopTechnicianId = dto.WorkshopTechnicianId,
                HandedOverByWorkshopTechnicianId = dto.HandedOverByWorkshopTechnicianId,
                HandoverAt = now,
                SignedAt = receiverSignatureComplete ? now : null,
                Status = receiverSignatureComplete ? "Completed" : "PendingReceiverSignature",
                CreatedAt = now,
                IsPartial = dto.IsPartial,
                ContainsMainRadioUnit = dto.ContainsMainRadioUnit
            };
            ApplyTagFields(handover, dto);

            for (var i = 0; i < photos.Count; i++)
            {
                handover.Photos.Add(new RadioHandoverPhoto
                {
                    SortOrder = i,
                    PhotoBase64 = photos[i]
                });
            }

            foreach (var item in dto.Accessories)
            {
                if (string.IsNullOrWhiteSpace(item.ItemName)) continue;
                handover.Accessories.Add(new RadioHandoverAccessory
                {
                    ItemName = item.ItemName.Trim(),
                    Quantity = item.Quantity < 1 ? 1 : item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "EA" : item.Unit.Trim(),
                    Description = item.Description?.Trim(),
                    SerialNumber = item.SerialNumber?.Trim()
                });
            }

            return handover;
        }

        private static void ValidateTagFieldsForCreate(CreateRadioHandoverDto dto)
        {
            if (dto.EquipmentTagType == EquipmentTagType.Good)
            {
                ValidateGoodTagFields(dto);
                return;
            }

            if (string.IsNullOrWhiteSpace(dto.DamageDescription))
                throw new ArgumentException("Keterangan kerusakan wajib untuk tag kuning (peralatan rusak).");
        }

        private static void ValidateGoodTagFields(CreateRadioHandoverDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RepairDataDescription))
                throw new ArgumentException("Data perbaikan wajib untuk tag hijau (peralatan baik).");
        }

        private static string ResolveJobDamageDescription(CreateRadioHandoverDto dto)
        {
            if (dto.EquipmentTagType == EquipmentTagType.Damaged)
                return dto.DamageDescription!.Trim();
            return string.IsNullOrWhiteSpace(dto.DamageDescription)
                ? dto.RepairDataDescription!.Trim()
                : dto.DamageDescription.Trim();
        }

        private static void ApplyTagFields(Models.RadioHandover handover, CreateRadioHandoverDto dto)
        {
            handover.EquipmentTagType = dto.EquipmentTagType;
            handover.OriginFrom = dto.OriginFrom?.Trim();
            handover.RepairDataDescription = dto.RepairDataDescription?.Trim();
            handover.RepairedByName = dto.RepairedByName?.Trim();
            handover.FrequencyError = dto.FrequencyError?.Trim();
            handover.AfReading = dto.AfReading?.Trim();
            handover.PowerReading = dto.PowerReading?.Trim();
            handover.VoltageOutNoLoad = dto.VoltageOutNoLoad?.Trim();
            handover.VoltageOutWithLoad = dto.VoltageOutWithLoad?.Trim();
            handover.PhysicalCondition = dto.PhysicalCondition?.Trim();
            handover.DisplayCondition = dto.DisplayCondition?.Trim();
        }

        private async Task ApplyInheritedTagFieldsAsync(
            CreateRadioHandoverDto dto, int jobId, RadioHandoverType handoverType)
        {
            var prev = await _context.RadioHandovers.AsNoTracking()
                .Where(h => h.RadioRepairJobId == jobId && !h.IsDeleted)
                .OrderByDescending(h => h.HandoverAt)
                .FirstOrDefaultAsync();

            if (handoverType == RadioHandoverType.WarehouseToHelpdesk
                && dto.EquipmentTagType == EquipmentTagType.Good
                && string.IsNullOrWhiteSpace(dto.RepairedByName)
                && prev != null)
            {
                var tech = await _context.Users.AsNoTracking()
                    .Where(u => u.UserId == prev.HandedOverByUserId)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync();
                dto.RepairedByName ??= tech;
            }

            if (handoverType == RadioHandoverType.TechnicianToWarehouse || handoverType == RadioHandoverType.WarehouseToHelpdesk)
            {
                var job = await _context.RadioRepairJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId);
                if (job != null && job.EquipmentTagType.HasValue)
                {
                    dto.EquipmentTagType = job.EquipmentTagType.Value;
                    if (job.EquipmentTagType == EquipmentTagType.Good)
                    {
                        dto.OriginFrom ??= job.OriginFrom;
                        dto.RepairDataDescription ??= job.RepairDataDescription;
                        dto.RepairedByName ??= job.RepairedByName;
                        dto.FrequencyError ??= job.FrequencyError;
                        dto.AfReading ??= job.AfReading;
                        dto.PowerReading ??= job.PowerReading;
                        dto.VoltageOutNoLoad ??= job.VoltageOutNoLoad;
                        dto.VoltageOutWithLoad ??= job.VoltageOutWithLoad;
                        dto.PhysicalCondition ??= job.PhysicalCondition;
                        dto.DisplayCondition ??= job.DisplayCondition;
                    }
                }
                else if (prev != null)
                {
                    dto.EquipmentTagType = prev.EquipmentTagType;
                }
            }

            if (string.IsNullOrWhiteSpace(dto.OriginFrom) && prev != null)
                dto.OriginFrom ??= prev.OriginFrom ?? prev.RadioOwnerLabel;
        }



        private async Task ValidateRadioSerialAsync(int? radioId, string serialNumber)
        {
            if (!radioId.HasValue) return;
            var radio = await _context.Radios.AsNoTracking().FirstOrDefaultAsync(r => r.Id == radioId)
                ?? throw new KeyNotFoundException("Radio tidak ditemukan.");
            if (!string.Equals(radio.SerialNumber?.Trim(), serialNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Serial number tidak cocok dengan radio master.");
        }

        private async Task ValidateUserRoleAsync(int userId, string roleName)
        {
            var ok = await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .AnyAsync(u => u.UserId == userId && u.IsActive && u.Role != null &&
                               u.Role.RoleName == roleName);
            if (!ok) throw new ArgumentException($"User harus aktif dengan role {roleName}.");
        }

        private async Task AddRepairOpenedHistoryAsync(
            Models.RadioRepairJob job, Models.RadioHandover handover, int userId)
        {
            var techName = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == job.AssignedTechnicianUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
            _context.RadioHistories.Add(new RadioHistory
            {
                RadioId = job.RadioId!.Value,
                Action = "RepairOpened",
                Details = $"Tiket: {job.HelpdeskTicketNumber}, STR: {handover.HandoverNumber}, Kerusakan: {job.DamageDescription}, Teknisi: {techName}",
                CreatedBy = await GetUserDisplayNameAsync(userId),
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task AddRepairReturnedToHelpdeskHistoryAsync(
            Models.RadioRepairJob job, Models.RadioHandover handover, int userId)
        {
            var hdName = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == handover.ReceivedByUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
            _context.RadioHistories.Add(new RadioHistory
            {
                RadioId = job.RadioId!.Value,
                Action = "RepairReturnedToHelpdesk",
                Details = $"Tiket: {job.HelpdeskTicketNumber}, STR: {handover.HandoverNumber}, Penerima Helpdesk: {hdName}",
                CreatedBy = await GetUserDisplayNameAsync(userId),
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task AddRepairWarehouseHistoryAsync(
            Models.RadioRepairJob job, Models.RadioHandover handover, int userId)
        {
            var whName = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == handover.ReceivedByUserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();
            _context.RadioHistories.Add(new RadioHistory
            {
                RadioId = job.RadioId!.Value,
                Action = "RepairHandoverWarehouse",
                Details = $"Tiket: {job.HelpdeskTicketNumber}, STR: {handover.HandoverNumber}, Penerima WH: {whName}",
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

        private async Task SyncMasterRadioFieldsAsync(int? radioId, EquipmentSnapshot equipment, int currentUserId, string handoverNumber)
        {
            if (!radioId.HasValue) return;
            var radio = await _context.Radios.FirstOrDefaultAsync(r => r.Id == radioId);
            if (radio == null) return;

            var changed = false;
            List<string> details = [];

            if (!string.IsNullOrWhiteSpace(equipment.RadioOwnerLabel) && radio.Company != equipment.RadioOwnerLabel)
            {
                details.Add($"Pemilik: {radio.Company ?? "-"} -> {equipment.RadioOwnerLabel}");
                radio.Company = equipment.RadioOwnerLabel;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(equipment.OwnerDivision) && radio.Division != equipment.OwnerDivision)
            {
                details.Add($"Divisi: {radio.Division ?? "-"} -> {equipment.OwnerDivision}");
                radio.Division = equipment.OwnerDivision;
                changed = true;
            }
            if (!string.IsNullOrWhiteSpace(equipment.OwnerDepartment) && radio.Department != equipment.OwnerDepartment)
            {
                details.Add($"Departemen: {radio.Department ?? "-"} -> {equipment.OwnerDepartment}");
                radio.Department = equipment.OwnerDepartment;
                changed = true;
            }

            if (changed)
            {
                radio.UpdatedAt = DateTime.UtcNow;
                _context.RadioHistories.Add(new RadioHistory
                {
                    RadioId = radio.Id,
                    Action = "Updated",
                    Details = $"Diupdate otomatis dari Serah Terima {handoverNumber}. Perubahan: {string.Join(", ", details)}",
                    CreatedBy = await GetUserDisplayNameAsync(currentUserId),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        public async Task<List<UserOptionDto>> GetTechniciansAsync() =>
            await GetUsersByRolesAsync(OperationalRoleNames.TechnicianRoles);

        public async Task<List<UserOptionDto>> GetWarehouseReceiversAsync() =>
            await GetUsersByRoleAsync(OperationalRoleNames.Warehouse);

        public async Task<List<UserOptionDto>> GetHelpdeskReceiversAsync() =>
            await GetUsersByRoleAsync(OperationalRoleNames.Helpdesk);

        private async Task ValidateTechnicianReceiverAsync(int userId)
        {
            var roles = OperationalRoleNames.TechnicianRoles;
            var ok = await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .AnyAsync(u => u.UserId == userId && u.IsActive && u.Role != null &&
                               roles.Contains(u.Role.RoleName));
            if (!ok)
                throw new ArgumentException(
                    $"User harus aktif dengan role teknisi ({OperationalRoleNames.Technician} atau Teknisi).");
        }

        private async Task<List<UserOptionDto>> GetUsersByRolesAsync(IEnumerable<string> roleNames) =>
            await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.IsActive && u.Role != null && roleNames.Contains(u.Role.RoleName))
                .OrderBy(u => u.FullName)
                .Select(u => new UserOptionDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Username = u.Username
                })
                .ToListAsync();

        private async Task<List<UserOptionDto>> GetUsersByRoleAsync(string roleName) =>
            await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.IsActive && u.Role != null && u.Role.RoleName == roleName)
                .OrderBy(u => u.FullName)
                .Select(u => new UserOptionDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Username = u.Username
                })
                .ToListAsync();

        private static RadioHandoverDetailDto MapDetail(Models.RadioHandover h) => new()
        {
            Id = h.Id,
            HandoverNumber = h.HandoverNumber,
            HandoverType = h.HandoverType.ToString(),
            RadioRepairJobId = h.RadioRepairJobId,
            HelpdeskTicketNumber = h.RadioRepairJob.HelpdeskTicketNumber,
            NoJobErp = h.NoJobErp,
            JobStatus = h.RadioRepairJob.Status.ToString(),
            RadioSerialNumber = h.RadioSerialNumber,
            RadioId = h.RadioId,
            RadioMasterRadioId = h.Radio?.RadioId,
            RadioFleet = h.Radio?.Fleet,
            EquipmentName = h.EquipmentName ?? h.Radio?.Type,
            UnitNumber = h.UnitNumber ?? h.Radio?.NomorUnit,
            RadioOwnerLabel = h.RadioOwnerLabel ?? (h.Radio != null ? FormatRadioOwnerLabel(h.Radio) : null),
            OwnerDivision = h.OwnerDivision ?? h.Radio?.Division,
            OwnerDepartment = h.OwnerDepartment ?? h.Radio?.Department,
            BatterySerialNumber = h.BatterySerialNumber,
            DamageDescription = h.RadioRepairJob.DamageDescription,
            ReceivedByUserId = h.ReceivedByUserId,
            HandedOverByName = h.HandedOverByWorkshopTechnician?.Name ?? h.HandedOverBy.FullName,
            // Tek→WH: penerima adalah akun Warehouse (ReceivedBy), bukan WorkshopTechnician (itu nama teknisi penyerah)
            // WH→HD / HD→Tek: penerima bisa berupa WorkshopTechnician atau akun sistem
            ReceivedByName = h.HandoverType == RadioHandoverType.TechnicianToWarehouse || h.HandoverType == RadioHandoverType.HelpdeskToWarehouse
                ? h.ReceivedBy.FullName
                : h.WorkshopTechnician?.Name ?? h.ReceivedBy.FullName,
            WorkshopTechnicianId = h.WorkshopTechnicianId,
            WorkshopTechnicianName = h.WorkshopTechnician?.Name,
            HandedOverByWorkshopTechnicianId = h.HandedOverByWorkshopTechnicianId,
            HandedOverByWorkshopTechnicianName = h.HandedOverByWorkshopTechnician?.Name,
            HandoverAt = h.HandoverAt,
            SignedAt = h.SignedAt,
            EquipmentTagType = h.EquipmentTagType.ToString(),
            OriginFrom = h.OriginFrom,
            RepairDataDescription = h.RepairDataDescription,
            RepairedByName = h.RepairedByName,
            FrequencyError = h.FrequencyError,
            AfReading = h.AfReading,
            PowerReading = h.PowerReading,
            VoltageOutNoLoad = h.VoltageOutNoLoad,
            VoltageOutWithLoad = h.VoltageOutWithLoad,
            PhysicalCondition = h.PhysicalCondition,
            DisplayCondition = h.DisplayCondition,
            HasRadioPhoto = h.Photos.Count > 0 || !string.IsNullOrEmpty(h.RadioPhotoBase64),
            HasHandedOverSignature = !string.IsNullOrEmpty(h.HandedOverSignatureBase64),
            HasReceiverSignature = !string.IsNullOrEmpty(h.ReceiverSignatureBase64),
            Status = h.Status,
            PhotoCount = h.Photos.Count > 0 ? h.Photos.Count : (string.IsNullOrEmpty(h.RadioPhotoBase64) ? 0 : 1),
            PreviewPhotoBase64 = h.Photos.OrderBy(p => p.SortOrder).Select(p => p.PhotoBase64).FirstOrDefault()
                ?? h.RadioPhotoBase64,
            RadioPhotoBase64 = h.RadioPhotoBase64,
            RadioPhotos = h.Photos.Count > 0
                ? [.. h.Photos.OrderBy(p => p.SortOrder).Select(p => p.PhotoBase64)]
                : (string.IsNullOrEmpty(h.RadioPhotoBase64) ? [] : [h.RadioPhotoBase64]),
            HandedOverSignatureBase64 = h.HandedOverSignatureBase64,
            ReceiverSignatureBase64 = h.ReceiverSignatureBase64,
            Remarks = h.Remarks,
            PicReceiverName = h.PicReceiverName,
            IsDeleted = h.IsDeleted,
            DeletedAt = h.DeletedAt,
            IsWarranty = h.RadioRepairJob.IsWarranty,
            IsPartial = h.IsPartial,
            ContainsMainRadioUnit = h.ContainsMainRadioUnit,
            IsScrap = h.RadioRepairJob != null && (h.RadioRepairJob.Status == RadioRepairJobStatus.ProcessScrap || h.RadioRepairJob.Status == RadioRepairJobStatus.Scrapped || h.RadioRepairJob.Handovers.Any(ho => ho.HandoverType == RadioHandoverType.TechnicianToHelpdesk)),
            IsPendingScrapData = h.Radio != null && h.Radio.IsScrap && !h.Radio.DateScrapped.HasValue,
            HasRemainingItemsForWarehouse =
                h.HandoverType == RadioHandoverType.TechnicianToHelpdesk &&
                h.Status == "Completed" &&
                h.RadioRepairJob != null &&
                (
                    !h.RadioRepairJob.Handovers.Any(ho =>
                        ho.HandoverType == RadioHandoverType.HelpdeskToWarehouse &&
                        !ho.IsDeleted &&
                        ho.ContainsMainRadioUnit)
                    ||
                    h.Accessories.Count >
                    h.RadioRepairJob.Handovers
                        .Where(ho => ho.HandoverType == RadioHandoverType.HelpdeskToWarehouse && !ho.IsDeleted)
                        .SelectMany(ho => ho.Accessories)
                        .Count()
                ),
            Accessories = [.. h.Accessories.Select(a => new HandoverAccessoryItemDto
            {
                ItemName = string.IsNullOrWhiteSpace(a.ItemName) ? (a.AccessoryCode ?? "") : a.ItemName,
                Quantity = a.Quantity,
                Unit = a.Unit,
                Description = a.Description,
                SerialNumber = a.SerialNumber
            })]
        };

        public async Task<RadioHandoverDetailDto> UpdateAsync(int id, UpdateRadioHandoverDto dto, int userId)
        {
            var h = await _context.RadioHandovers
                .Include(x => x.RadioRepairJob)
                .Include(x => x.HandedOverBy)
                .Include(x => x.ReceivedBy)
                .Include(x => x.WorkshopTechnician)
                .Include(x => x.HandedOverByWorkshopTechnician)
                .Include(x => x.Photos)
                .Include(x => x.Accessories)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
                ?? throw new KeyNotFoundException("Serah terima tidak ditemukan.");

            if (h.HandoverType == RadioHandoverType.WarehouseToHelpdesk)
            {
                // WH→HD: update receiver helpdesk jika belum complete (belum ada TTD penerima)
                if (h.Status != "Completed" && dto.ReceivedByUserId != 0 && dto.ReceivedByUserId != h.ReceivedByUserId)
                {
                    await ValidateUserRoleAsync(dto.ReceivedByUserId, OperationalRoleNames.Helpdesk);
                    h.ReceivedByUserId = dto.ReceivedByUserId;
                }
                h.Remarks = dto.Remarks?.Trim();
                h.PicReceiverName = dto.PicReceiverName?.Trim();
            }
            else if (h.HandoverType != RadioHandoverType.HelpdeskToTechnician && h.HandoverType != RadioHandoverType.TechnicianToWarehouse)
            {
                h.Remarks = dto.Remarks?.Trim();
                h.PicReceiverName = dto.PicReceiverName?.Trim();
            }
            else
            {
                if (h.HandoverType == RadioHandoverType.HelpdeskToTechnician)
                {
                    var newTicket = dto.HelpdeskTicketNumber?.Trim();
                    var newSerial = dto.RadioSerialNumber?.Trim();
                    if (!string.IsNullOrEmpty(newTicket) && !string.IsNullOrEmpty(newSerial) &&
                        (h.RadioRepairJob.HelpdeskTicketNumber != newTicket || h.RadioRepairJob.RadioSerialNumber != newSerial))
                    {
                        await RadioRepairJobService.ValidateDuplicateTicketSerialAsync(
                            _context, newTicket, newSerial, h.RadioRepairJobId);
                        h.RadioRepairJob.HelpdeskTicketNumber = newTicket;
                        h.RadioRepairJob.RadioSerialNumber = newSerial;
                        h.RadioRepairJob.JobNumber = Pm.Helper.RepairJobReference.InternalKey(newTicket, newSerial);
                    }

                    if (h.Status != "Completed")
                    {
                        h.ReceivedByUserId = dto.ReceivedByUserId;
                        h.RadioRepairJob.AssignedTechnicianUserId = dto.ReceivedByUserId;

                        if (!string.IsNullOrWhiteSpace(dto.ReceiverSignatureBase64))
                        {
                            var now = DateTime.UtcNow;
                            h.ReceiverSignatureBase64 = dto.ReceiverSignatureBase64;
                            h.Status = "Completed";
                            h.SignedAt = now;
                            h.RadioRepairJob.Status = RadioRepairJobStatus.InProgress;
                            
                            _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
                            {
                                JobId = h.RadioRepairJob.Id,
                                FromStatus = RadioRepairJobStatus.Received,
                                ToStatus = RadioRepairJobStatus.InProgress,
                                Note = "Teknisi melengkapi TTD via edit HD",
                                UserId = userId,
                                At = now
                            });
                        }
                    }
                }

                h.RadioId = dto.RadioId;
                h.RadioSerialNumber = dto.RadioSerialNumber?.Trim() ?? string.Empty;
                h.BatterySerialNumber = dto.BatterySerialNumber?.Trim();
                h.NoJobErp = dto.NoJobErp?.Trim();
                h.EquipmentName = dto.EquipmentName?.Trim();
                h.UnitNumber = dto.UnitNumber?.Trim();
                h.RadioOwnerLabel = dto.RadioOwnerLabel?.Trim();
                h.OwnerDivision = dto.OwnerDivision?.Trim();
                h.OwnerDepartment = dto.OwnerDepartment?.Trim();
                h.Remarks = dto.Remarks?.Trim();
                h.PicReceiverName = dto.PicReceiverName?.Trim();
                h.EquipmentTagType = dto.EquipmentTagType;
                if (h.RadioRepairJob != null)
                {
                    h.RadioRepairJob.DamageDescription = dto.EquipmentTagType == EquipmentTagType.Damaged
                        ? dto.DamageDescription?.Trim() ?? h.RadioRepairJob.DamageDescription
                        : string.IsNullOrWhiteSpace(dto.DamageDescription)
                            ? dto.RepairDataDescription?.Trim() ?? h.RadioRepairJob.DamageDescription
                            : dto.DamageDescription.Trim();

                    h.RadioRepairJob.BatterySerialNumber = h.BatterySerialNumber;
                    h.RadioRepairJob.EquipmentName = h.EquipmentName;
                    h.RadioRepairJob.UnitNumber = h.UnitNumber;
                    h.RadioRepairJob.RadioOwnerLabel = h.RadioOwnerLabel;
                    h.RadioRepairJob.OwnerDivision = h.OwnerDivision;
                    h.RadioRepairJob.OwnerDepartment = h.OwnerDepartment;
                    h.RadioRepairJob.RadioId = h.RadioId;
                    h.RadioRepairJob.IsWarranty = dto.IsWarranty;

                    if (dto.ReceivedByUserId != 0 && dto.ReceivedByUserId != h.ReceivedByUserId)
                    {
                        // TechnicianToWarehouse: penerima adalah Warehouse, bukan teknisi
                        await ValidateUserRoleAsync(dto.ReceivedByUserId, OperationalRoleNames.Warehouse);
                        h.ReceivedByUserId = dto.ReceivedByUserId;
                        // AssignedTechnicianUserId tidak diubah — tetap teknisi yang mengerjakan job
                    }

                    if (dto.WorkshopTechnicianId != null && dto.WorkshopTechnicianId != h.WorkshopTechnicianId)
                    {
                        h.WorkshopTechnicianId = dto.WorkshopTechnicianId;
                        h.RadioRepairJob.WorkshopTechnicianId = dto.WorkshopTechnicianId;
                    }

                    // Update teknisi penyerah jika berubah
                    if (dto.HandedOverByWorkshopTechnicianId.HasValue &&
                        dto.HandedOverByWorkshopTechnicianId != h.HandedOverByWorkshopTechnicianId)
                    {
                        h.HandedOverByWorkshopTechnicianId = dto.HandedOverByWorkshopTechnicianId;
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.ReceiverSignatureBase64) && dto.ReceiverSignatureBase64 != h.ReceiverSignatureBase64)
                {
                    _imageValidator.Validate(dto.ReceiverSignatureBase64, Pm.Enums.StoredImageKind.Signature, "TTD penerima");
                    h.ReceiverSignatureBase64 = dto.ReceiverSignatureBase64;
                    if (h.Status != "Completed")
                    {
                        h.Status = "Completed";
                        h.SignedAt = DateTime.UtcNow;
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.HandedOverSignatureBase64) && dto.HandedOverSignatureBase64 != h.HandedOverSignatureBase64)
                {
                    _imageValidator.Validate(dto.HandedOverSignatureBase64, Pm.Enums.StoredImageKind.Signature, "TTD penyerah");
                    h.HandedOverSignatureBase64 = dto.HandedOverSignatureBase64;
                }
            }

            // Data perbaikan (Green Tag) can be updated by Warehouse during WH->HD
            h.OriginFrom = dto.OriginFrom?.Trim();
            h.RepairDataDescription = dto.RepairDataDescription?.Trim();
            h.RepairedByName = dto.RepairedByName?.Trim();
            h.FrequencyError = dto.FrequencyError?.Trim();
            h.AfReading = dto.AfReading?.Trim();
            h.PowerReading = dto.PowerReading?.Trim();
            h.VoltageOutNoLoad = dto.VoltageOutNoLoad?.Trim();
            h.VoltageOutWithLoad = dto.VoltageOutWithLoad?.Trim();
            h.PhysicalCondition = dto.PhysicalCondition?.Trim();
            h.DisplayCondition = dto.DisplayCondition?.Trim();


            List<string> photos = [];
            if (dto.RadioPhotos != null && dto.RadioPhotos.Count > 0)
                photos = [.. dto.RadioPhotos.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!)];
            else if (!string.IsNullOrWhiteSpace(dto.RadioPhotoBase64))
                photos = [dto.RadioPhotoBase64];

            if (photos.Count > 0)
            {
                _context.RadioHandoverPhotos.RemoveRange(h.Photos);
                h.Photos.Clear();
                h.RadioPhotoBase64 = photos[0];
                for (var i = 0; i < photos.Count; i++)
                {
                    h.Photos.Add(new RadioHandoverPhoto
                    {
                        SortOrder = i,
                        PhotoBase64 = photos[i]
                    });
                }
            }

            if (dto.Accessories != null)
            {
                _context.RadioHandoverAccessories.RemoveRange(h.Accessories);
                h.Accessories.Clear();
                foreach (var item in dto.Accessories)
                {
                    if (string.IsNullOrWhiteSpace(item.ItemName)) continue;
                    h.Accessories.Add(new RadioHandoverAccessory
                    {
                        ItemName = item.ItemName.Trim(),
                        Quantity = item.Quantity < 1 ? 1 : item.Quantity,
                        Unit = string.IsNullOrWhiteSpace(item.Unit) ? "EA" : item.Unit.Trim(),
                        Description = item.Description?.Trim(),
                        SerialNumber = item.SerialNumber?.Trim()
                    });
                }
            }

            // ✅ Sinkronisasi Data Tag Hijau + Aksesoris ke Job & Serah Terima WH->HD
            // Dijalankan SETELAH foto & aksesoris di-update pada h agar data terbaru yang disalin
            if (h.RadioRepairJob != null)
            {
                // Update master data perbaikan di tiket
                h.RadioRepairJob.OriginFrom = h.OriginFrom;
                h.RadioRepairJob.RepairDataDescription = h.RepairDataDescription;
                h.RadioRepairJob.RepairedByName = h.RepairedByName;
                h.RadioRepairJob.FrequencyError = h.FrequencyError;
                h.RadioRepairJob.AfReading = h.AfReading;
                h.RadioRepairJob.PowerReading = h.PowerReading;
                h.RadioRepairJob.VoltageOutNoLoad = h.VoltageOutNoLoad;
                h.RadioRepairJob.VoltageOutWithLoad = h.VoltageOutWithLoad;
                h.RadioRepairJob.PhysicalCondition = h.PhysicalCondition;
                h.RadioRepairJob.DisplayCondition = h.DisplayCondition;

                // Jika ini adalah Tek->WH dan ada WH->HD yang belum selesai, maka ikut di-update
                if (h.HandoverType == RadioHandoverType.TechnicianToWarehouse)
                {
                    var subsequentHandover = await _context.RadioHandovers
                        .Include(sh => sh.Accessories)
                        .FirstOrDefaultAsync(sh => sh.RadioRepairJobId == h.RadioRepairJobId 
                                                && sh.HandoverType == RadioHandoverType.WarehouseToHelpdesk
                                                && sh.Status != "Completed");
                    if (subsequentHandover != null)
                    {
                        // Sinkronisasi Data Perbaikan (Tag Hijau)
                        subsequentHandover.OriginFrom = h.OriginFrom;
                        subsequentHandover.RepairDataDescription = h.RepairDataDescription;
                        subsequentHandover.RepairedByName = h.RepairedByName;
                        subsequentHandover.FrequencyError = h.FrequencyError;
                        subsequentHandover.AfReading = h.AfReading;
                        subsequentHandover.PowerReading = h.PowerReading;
                        subsequentHandover.VoltageOutNoLoad = h.VoltageOutNoLoad;
                        subsequentHandover.VoltageOutWithLoad = h.VoltageOutWithLoad;
                        subsequentHandover.PhysicalCondition = h.PhysicalCondition;
                        subsequentHandover.DisplayCondition = h.DisplayCondition;

                        // Sinkronisasi Aksesoris (data terbaru dari h.Accessories)
                        _context.RadioHandoverAccessories.RemoveRange(subsequentHandover.Accessories);
                        subsequentHandover.Accessories.Clear();
                        foreach (var acc in h.Accessories)
                        {
                            subsequentHandover.Accessories.Add(new RadioHandoverAccessory
                            {
                                ItemName = acc.ItemName,
                                Quantity = acc.Quantity,
                                Unit = acc.Unit,
                                Description = acc.Description,
                                SerialNumber = acc.SerialNumber
                            });
                        }

                        // ⚠️ Foto TIDAK disinkronkan — tiap serah terima punya foto sendiri
                    }
                }
            }

            h.UpdatedAt = DateTime.UtcNow;
            
            // ✅ Smart Activity Log: Detect editor name based on handover type
            string editorName;
            
            if (h.HandoverType == RadioHandoverType.TechnicianToWarehouse)
            {
                // Case 1: Teknisi → Warehouse (SHARED ACCOUNT)
                // Prioritaskan teknisi yang tercatat menyerahkan radio ini
                if (h.HandedOverByWorkshopTechnicianId.HasValue)
                {
                    var technician = h.HandedOverByWorkshopTechnician ?? await _context.WorkshopTechnicians
                        .FirstOrDefaultAsync(t => t.Id == h.HandedOverByWorkshopTechnicianId.Value);
                    
                    editorName = technician?.Name ?? "Teknisi";
                }
                else if (h.RadioRepairJob?.WorkshopTechnicianId.HasValue == true)
                {
                    // Fallback ke teknisi yang diassign ke job perbaikan
                    var technician = h.WorkshopTechnician ?? await _context.WorkshopTechnicians
                        .FirstOrDefaultAsync(t => t.Id == h.RadioRepairJob.WorkshopTechnicianId.Value);
                    
                    editorName = technician?.Name ?? "Teknisi";
                }
                else
                {
                    editorName = "Teknisi";
                }
            }
            else
            {
                // Case 2: Helpdesk/Warehouse handover (INDIVIDUAL ACCOUNT)
                // Ambil nama dari user yang login
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);
                
                editorName = user?.FullName ?? user?.Username ?? "Unknown";
            }
            
            await _activityLog.LogAsync("RadioHandover", h.Id, "Update", userId, $"Edit STR {h.HandoverNumber} oleh {editorName}");
            await _context.SaveChangesAsync();

            // Kirim notif ke penerima baru jika receivedByUserId berubah (HD→Tek)
            if (h.HandoverType == RadioHandoverType.HelpdeskToTechnician)
            {
                var serial = h.RadioSerialNumber ?? "";
                var ticket = h.RadioRepairJob?.HelpdeskTicketNumber ?? "";
                // Ambil nama teknisi dari DB langsung menggunakan ID dari DTO (lebih reliable dari navigation property)
                var techName = "Teknisi";
                // Untuk HD→Tek: teknisi penerima = dto.WorkshopTechnicianId atau h.WorkshopTechnicianId
                var workshopTechIdForNotif = dto.WorkshopTechnicianId ?? h.WorkshopTechnicianId;
                if (workshopTechIdForNotif.HasValue)
                {
                    var tech = await _context.WorkshopTechnicians.AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == workshopTechIdForNotif.Value);
                    techName = tech?.Name ?? "Teknisi";
                }
                await _notificationService.UpdateOrCreateAsync(new DTOs.Notification.CreateNotificationDto
                {
                    RecipientUserId = h.ReceivedByUserId,
                    Title = "Radio Masuk Workshop — Untuk Anda",
                    Message = $"Radio SN {serial} diserahkan ke Workshop dari Helpdesk (Tiket {ticket}). Anda ditunjuk sebagai penerima: {techName}.",
                    Category = "handover",
                    LinkUrl = "/radio-handover",
                    ReferenceId = h.Id,
                    ReferenceType = "RadioHandover"
                });
            }

            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            return (await GetByIdAsync(id))!;
        }

        public async Task SoftDeleteAsync(int id, int userId)
        {
            var h = await _context.RadioHandovers
                .Include(x => x.RadioRepairJob)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
                ?? throw new KeyNotFoundException("Serah terima tidak ditemukan.");

            if (h.RadioRepairJob.Status is RadioRepairJobStatus.HandedToWarehouse or RadioRepairJobStatus.ReturnedToHelpdesk)
                throw new InvalidOperationException("Serah terima pada job yang sudah selesai siklus tidak dapat dihapus.");

            var now = DateTime.UtcNow;
            h.IsDeleted = true;
            h.DeletedAt = now;
            h.DeletedByUserId = userId;
            h.UpdatedAt = now;

            if (h.RadioRepairJob != null && h.RadioRepairJob.CurrentHandoverId == h.Id)
            {
                h.RadioRepairJob.CurrentHandoverId = null;
                h.RadioRepairJob.UpdatedAt = now;
            }

            await _activityLog.LogAsync("RadioHandover", h.Id, "SoftDelete", userId,
                $"Arsip STR {h.HandoverNumber}, tiket {h.RadioRepairJob?.HelpdeskTicketNumber}");

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
        }

        public async Task CancelPendingHandoverAsync(int id, int userId)
        {
            var h = await _context.RadioHandovers
                .Include(x => x.RadioRepairJob)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
                ?? throw new KeyNotFoundException("Serah terima tidak ditemukan.");

            if (h.Status != "PendingReceiverSignature")
                throw new InvalidOperationException("Hanya serah terima yang masih menunggu TTD penerima yang dapat dibatalkan.");

            if (h.HandedOverByUserId != userId)
            {
                // Also allow if userId matches the workshop technician
                var isOwner = h.RadioRepairJob?.WorkshopTechnicianId.HasValue == true &&
                              await _context.WorkshopTechnicians.AnyAsync(wt => wt.Id == h.RadioRepairJob.WorkshopTechnicianId && wt.UserId == userId);
                if (!isOwner)
                    throw new InvalidOperationException("Anda tidak memiliki hak untuk membatalkan serah terima ini.");
            }

            var now = DateTime.UtcNow;
            h.IsDeleted = true;
            h.DeletedAt = now;
            h.DeletedByUserId = userId;
            h.UpdatedAt = now;

            if (h.RadioRepairJob != null && h.RadioRepairJob.CurrentHandoverId == h.Id)
            {
                h.RadioRepairJob.CurrentHandoverId = null;
                h.RadioRepairJob.UpdatedAt = now;
            }

            await _activityLog.LogAsync("RadioHandover", h.Id, "CancelPending", userId,
                $"Batalkan STR pending {h.HandoverNumber}, tiket {h.RadioRepairJob?.HelpdeskTicketNumber}");

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");
        }

        public async Task<RadioHandoverDetailDto> ChangeReceiverAsync(int id, int newReceiverUserId, int currentUserId)
        {
            var h = await _context.RadioHandovers
                .Include(x => x.HandedOverBy)
                .Include(x => x.ReceivedBy)
                .Include(x => x.RadioRepairJob)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
                ?? throw new KeyNotFoundException("Serah terima tidak ditemukan.");

            if (h.Status != "PendingReceiverSignature")
                throw new InvalidOperationException("Hanya serah terima yang masih menunggu TTD penerima yang dapat diubah penerimanya.");

            if (h.HandedOverByUserId != currentUserId)
            {
                var isOwner = h.RadioRepairJob?.WorkshopTechnicianId.HasValue == true &&
                              await _context.WorkshopTechnicians.AnyAsync(wt => wt.Id == h.RadioRepairJob.WorkshopTechnicianId && wt.UserId == currentUserId);
                if (!isOwner)
                    throw new InvalidOperationException("Anda tidak memiliki hak untuk mengubah serah terima ini.");
            }

            var newUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == newReceiverUserId)
                ?? throw new InvalidOperationException("User penerima baru tidak valid.");

            h.ReceivedByUserId = newReceiverUserId;
            h.ReceiverSignatureBase64 = null;   // Reset TTD lama agar penerima baru bisa tanda tangan ulang
            h.UpdatedAt = DateTime.UtcNow;

            var receiverName = newUser.FullName ?? newUser.Username;
            await _activityLog.LogAsync("RadioHandover", h.Id, "ChangeReceiver", currentUserId, $"Ubah penerima menjadi {receiverName}");
            await _context.SaveChangesAsync();

            // Kirim notifikasi ke penerima BARU untuk tanda tangan
            await _notificationService.CreateAsync(new CreateNotificationDto
            {
                RecipientUserId = newReceiverUserId,
                Title = "Tanda Tangan Serah Terima",
                Message = $"Anda ditunjuk sebagai penerima baru untuk STR {h.HandoverNumber} (SN: {h.RadioSerialNumber}). Mohon lengkapi tanda tangan Anda.",
                Category = "handover",
                LinkUrl = "/radio-handover/warehouse",
                ReferenceId = h.Id,
                ReferenceType = "RadioHandover"
            });

            // Broadcast changes
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
            await _notificationService.BroadcastRefreshDataAsync("RadioRepairJob");

            return MapDetail(h);
        }

        public async Task RestoreAsync(int id, int userId)
        {
            var h = await _context.RadioHandovers.FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted)
                ?? throw new KeyNotFoundException("Arsip serah terima tidak ditemukan.");

            h.IsDeleted = false;
            h.DeletedAt = null;
            h.DeletedByUserId = null;
            h.UpdatedAt = DateTime.UtcNow;

            await _activityLog.LogAsync("RadioHandover", h.Id, "Restore", userId, $"Pulihkan STR {h.HandoverNumber}");
            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
        }

        public async Task DeletePermanentAsync(int id, int userId)
        {
            var h = await _context.RadioHandovers
                .Include(x => x.Accessories)
                .Include(x => x.Photos)
                .Include(x => x.RadioRepairJob)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("Arsip serah terima tidak ditemukan.");

            if (!h.IsDeleted)
                throw new InvalidOperationException("Serah terima harus berada di arsip sebelum dihapus permanen.");

            var handoverNumber = h.HandoverNumber;

            if (h.RadioRepairJob.CurrentHandoverId == h.Id)
            {
                h.RadioRepairJob.CurrentHandoverId = null;
                h.RadioRepairJob.UpdatedAt = DateTime.UtcNow;
            }

            _context.RadioHandoverAccessories.RemoveRange(h.Accessories);
            _context.RadioHandoverPhotos.RemoveRange(h.Photos);
            _context.RadioHandovers.Remove(h);

            await _activityLog.LogAsync("RadioHandover", id, "PermanentlyDeleted", userId,
                $"Hapus permanen STR {handoverNumber}");

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
        }
    }
}
