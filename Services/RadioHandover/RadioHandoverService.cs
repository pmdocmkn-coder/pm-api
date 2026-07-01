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
                    h.HandoverType == RadioHandoverType.WarehouseToHelpdesk
                );
            }

            if (query.HandoverType.HasValue)
                q = q.Where(h => h.HandoverType == query.HandoverType);

            if (query.JobId.HasValue)
                q = q.Where(h => h.RadioRepairJobId == query.JobId);

            if (query.ReceivedByUserId.HasValue)
                q = q.Where(h => h.ReceivedByUserId == query.ReceivedByUserId);

            if (query.FromDate.HasValue)
                q = q.Where(h => h.HandoverAt >= query.FromDate.Value);
            if (query.ToDate.HasValue)
                q = q.Where(h => h.HandoverAt <= query.ToDate.Value.AddDays(1));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                q = q.Where(h =>
                    h.HandoverNumber.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    h.RadioSerialNumber.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                    h.RadioRepairJob.HelpdeskTicketNumber.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(h => h.HandoverAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(h => new RadioHandoverListDto
                {
                    Id = h.Id,
                    HandoverNumber = h.HandoverNumber,
                    HandoverType = h.HandoverType.ToString(),
                    RadioRepairJobId = h.RadioRepairJobId,
                    HelpdeskTicketNumber = h.RadioRepairJob.HelpdeskTicketNumber,
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
                    PreviewPhotoBase64 = h.Photos.OrderBy(p => p.SortOrder).Select(p => p.PhotoBase64).FirstOrDefault()
                        ?? h.RadioPhotoBase64,
                    PicReceiverName = h.PicReceiverName
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
                dto.HandoverType == RadioHandoverType.WarehouseToHelpdesk)
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

            if (handover.Accessories.Count == 0)
                await CopyAccessoriesFromHelpdeskHandoverAsync(handover, job.Id);

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
                    ReferenceId = job.Id,
                    ReferenceType = "RadioRepairJob"
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
                h.RadioRepairJobId == job.Id && h.HandoverType == RadioHandoverType.WarehouseToHelpdesk);
            if (alreadyReturned)
                throw new InvalidOperationException("Radio job ini sudah diserahkan ke Helpdesk.");

            await ValidateUserRoleAsync(currentUserId, OperationalRoleNames.Warehouse);
            await ValidateUserRoleAsync(dto.ReceivedByUserId, OperationalRoleNames.Helpdesk);

            var strNumber = await DocumentNumberHelper.NextHandoverNumberAsync(_context);
            var now = DateTime.UtcNow;

            if (dto.EquipmentTagType != EquipmentTagType.Damaged)
                dto.EquipmentTagType = EquipmentTagType.Good;

            await ApplyInheritedTagFieldsAsync(dto, job.Id, RadioHandoverType.WarehouseToHelpdesk);

            var receiverComplete = !string.IsNullOrWhiteSpace(dto.ReceiverSignatureBase64);
            var handover = BuildHandover(dto, photos, strNumber, job.Id, currentUserId, dto.ReceivedByUserId, now, receiverComplete);
            handover.HandoverType = RadioHandoverType.WarehouseToHelpdesk;
            handover.RadioId = job.RadioId ?? dto.RadioId;
            handover.RadioSerialNumber = job.RadioSerialNumber;
            handover.BatterySerialNumber = job.BatterySerialNumber ?? dto.BatterySerialNumber;

            if (handover.Accessories.Count == 0)
                await CopyAccessoriesFromHelpdeskHandoverAsync(handover, job.Id);

            _context.RadioHandovers.Add(handover);
            await _context.SaveChangesAsync();

            job.CurrentHandoverId = handover.Id;
            job.UpdatedAt = now;

            if (receiverComplete)
            {
                var fromStatus = job.Status;
                job.Status = RadioRepairJobStatus.ReturnedToHelpdesk;
                job.ClosedAt = now;

                _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
                {
                    JobId = job.Id,
                    FromStatus = fromStatus,
                    ToStatus = RadioRepairJobStatus.ReturnedToHelpdesk,
                    Note = $"Serah terima {strNumber} ke Helpdesk",
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
                ReferenceId = job.Id,
                ReferenceType = "RadioRepairJob"
            }, excludeUserIds: [currentUserId, dto.ReceivedByUserId]);

            // Notif ke Teknisi workshop yang mengerjakan radio ini
            if (job.AssignedTechnicianUserId != 0)
            {
                await _notificationService.CreateAsync(new CreateNotificationDto
                {
                    RecipientUserId = job.AssignedTechnicianUserId,
                    Title = "Radio Sudah Kembali ke Helpdesk",
                    Message = $"Radio SN {job.RadioSerialNumber} yang Anda kerjakan telah diserahkan kembali ke Helpdesk oleh Warehouse. Proses selesai.",
                    Category = "handover",
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
            if (!string.IsNullOrWhiteSpace(dto.PicReceiverName))
            {
                handover.PicReceiverName = dto.PicReceiverName;
            }
            if (!string.IsNullOrWhiteSpace(dto.Remarks))
            {
                handover.Remarks = string.IsNullOrWhiteSpace(handover.Remarks) 
                    ? dto.Remarks 
                    : $"{handover.Remarks}\n{dto.Remarks}";
            }
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
                    handover.RadioRepairJob.Status = RadioRepairJobStatus.ReturnedToHelpdesk;
                    handover.RadioRepairJob.ClosedAt = now;
                }

                string statusNote = handover.HandoverType switch
                {
                    RadioHandoverType.HelpdeskToTechnician => "Teknisi melengkapi TTD penerima (Radio diterima, menunggu assign teknisi)",
                    RadioHandoverType.TechnicianToWarehouse => "Warehouse melengkapi TTD penerima",
                    RadioHandoverType.WarehouseToHelpdesk => "Helpdesk melengkapi TTD penerima",
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

            return (await GetByIdAsync(handover.Id))!;
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
                CreatedAt = now
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

        /// <summary>Salin aksesoris dari STR HD→Tek terakhir jika Tek→WH tidak mengirim daftar baru.</summary>
        private async Task CopyAccessoriesFromHelpdeskHandoverAsync(Models.RadioHandover target, int jobId)
        {
            var prev = await _context.RadioHandovers
                .AsNoTracking()
                .Include(h => h.Accessories)
                .Where(h => h.RadioRepairJobId == jobId
                            && h.HandoverType == RadioHandoverType.HelpdeskToTechnician
                            && !h.IsDeleted)
                .OrderByDescending(h => h.HandoverAt)
                .FirstOrDefaultAsync();

            if (prev == null) return;

            foreach (var item in prev.Accessories)
            {
                if (string.IsNullOrWhiteSpace(item.ItemName)) continue;
                target.Accessories.Add(new RadioHandoverAccessory
                {
                    ItemName = item.ItemName.Trim(),
                    Quantity = item.Quantity < 1 ? 1 : item.Quantity,
                    Unit = string.IsNullOrWhiteSpace(item.Unit) ? "EA" : item.Unit.Trim(),
                    Description = item.Description?.Trim(),
                    SerialNumber = item.SerialNumber?.Trim()
                });
            }

            if (string.IsNullOrWhiteSpace(target.BatterySerialNumber) && !string.IsNullOrWhiteSpace(prev.BatterySerialNumber))
                target.BatterySerialNumber = prev.BatterySerialNumber.Trim();
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
            ReceivedByName = h.WorkshopTechnician?.Name ?? h.ReceivedBy.FullName,
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

            if (h.HandoverType != RadioHandoverType.HelpdeskToTechnician)
            {
                h.Remarks = dto.Remarks?.Trim();
                h.PicReceiverName = dto.PicReceiverName?.Trim();
            }
            else
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

                    if (dto.ReceivedByUserId != 0 && dto.ReceivedByUserId != h.ReceivedByUserId)
                    {
                        await ValidateTechnicianReceiverAsync(dto.ReceivedByUserId);
                        h.ReceivedByUserId = dto.ReceivedByUserId;
                        h.RadioRepairJob.AssignedTechnicianUserId = dto.ReceivedByUserId;
                    }

                    if (dto.WorkshopTechnicianId != null && dto.WorkshopTechnicianId != h.WorkshopTechnicianId)
                    {
                        h.WorkshopTechnicianId = dto.WorkshopTechnicianId;
                        h.RadioRepairJob.WorkshopTechnicianId = dto.WorkshopTechnicianId;
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

            h.UpdatedAt = DateTime.UtcNow;
            await _activityLog.LogAsync("RadioHandover", h.Id, "Update", userId, $"Edit STR {h.HandoverNumber}");
            await _context.SaveChangesAsync();
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

            await _activityLog.LogAsync("RadioHandover", h.Id, "SoftDelete", userId,
                $"Arsip STR {h.HandoverNumber}, tiket {h.RadioRepairJob.HelpdeskTicketNumber}");

            await _context.SaveChangesAsync();
            await _notificationService.BroadcastRefreshDataAsync("RadioHandover");
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
