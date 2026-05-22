using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs.Common;
using Pm.DTOs.RadioHandover;
using Pm.Enums;
using Pm.Helper;
using Pm.Models;
using Pm.Services;
using Pm.Services.Media;

namespace Pm.Services.RadioHandover
{
    public class RadioHandoverService : IRadioHandoverService
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLog;
        private readonly IImageBase64Validator _imageValidator;

        public RadioHandoverService(
            AppDbContext context,
            IActivityLogService activityLog,
            IImageBase64Validator imageValidator)
        {
            _context = context;
            _activityLog = activityLog;
            _imageValidator = imageValidator;
        }

        public async Task<PagedResultDto<RadioHandoverListDto>> GetAllAsync(
            RadioHandoverQueryDto query, int currentUserId, string? roleName)
        {
            var q = _context.RadioHandovers.AsNoTracking()
                .Include(h => h.HandedOverBy)
                .Include(h => h.ReceivedBy)
                .Include(h => h.RadioRepairJob)
                .Include(h => h.Photos)
                .AsQueryable();

            if (string.Equals(roleName, "Warehouse", StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(h => h.HandoverType == RadioHandoverType.TechnicianToWarehouse);
                if (query.ReceivedByUserId == null)
                    q = q.Where(h => h.ReceivedByUserId == currentUserId);
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
                var s = query.Search.Trim().ToLower();
                q = q.Where(h =>
                    h.HandoverNumber.ToLower().Contains(s) ||
                    h.RadioSerialNumber.ToLower().Contains(s) ||
                    h.RadioRepairJob.JobNumber.ToLower().Contains(s));
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
                    JobNumber = h.RadioRepairJob.JobNumber,
                    RadioSerialNumber = h.RadioSerialNumber,
                    HandedOverByName = h.HandedOverBy.FullName,
                    ReceivedByName = h.ReceivedBy.FullName,
                    HandoverAt = h.HandoverAt,
                    HasRadioPhoto = h.RadioPhotoBase64 != null && h.RadioPhotoBase64.Length > 0,
                    HasHandedOverSignature = h.HandedOverSignatureBase64 != null && h.HandedOverSignatureBase64.Length > 0,
                    HasReceiverSignature = h.ReceiverSignatureBase64 != null && h.ReceiverSignatureBase64.Length > 0,
                    Status = h.Status,
                    PhotoCount = h.Photos.Count > 0 ? h.Photos.Count : (h.RadioPhotoBase64 != null ? 1 : 0),
                    PreviewPhotoBase64 = h.Photos.OrderBy(p => p.SortOrder).Select(p => p.PhotoBase64).FirstOrDefault()
                        ?? h.RadioPhotoBase64
                })
                .ToListAsync();

            return new PagedResultDto<RadioHandoverListDto>(items, query, total);
        }

        public async Task<RadioHandoverDetailDto?> GetByIdAsync(int id)
        {
            var h = await _context.RadioHandovers
                .Include(x => x.HandedOverBy)
                .Include(x => x.ReceivedBy)
                .Include(x => x.RadioRepairJob)
                .Include(x => x.Accessories)
                .Include(x => x.Photos)
                .FirstOrDefaultAsync(x => x.Id == id);
            return h == null ? null : MapDetail(h);
        }

        public async Task<RadioHandoverDetailDto> CreateAsync(CreateRadioHandoverDto dto, int currentUserId)
        {
            var photos = ResolvePhotoList(dto);
            _imageValidator.ValidatePhotoList(photos, "Foto radio");
            _imageValidator.ValidateRequired(dto.HandedOverSignatureBase64, StoredImageKind.Signature, "TTD penyerah");

            if (dto.HandoverType == RadioHandoverType.HelpdeskToTechnician)
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
                return dto.RadioPhotos.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            if (!string.IsNullOrWhiteSpace(dto.RadioPhotoBase64))
                return new List<string> { dto.RadioPhotoBase64 };
            return new List<string>();
        }

        private async Task<RadioHandoverDetailDto> CreateHelpdeskToTechnicianAsync(
            CreateRadioHandoverDto dto, List<string> photos, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(dto.HelpdeskTicketNumber))
                throw new ArgumentException("No tiket helpdesk wajib diisi.");
            if (string.IsNullOrWhiteSpace(dto.DamageDescription))
                throw new ArgumentException("Keterangan kerusakan wajib diisi.");

            await ValidateRadioSerialAsync(dto.RadioId, dto.RadioSerialNumber);
            await ValidateTechnicianReceiverAsync(dto.ReceivedByUserId);

            var jobNumber = await DocumentNumberHelper.NextRadioRepairJobNumberAsync(_context);
            var strNumber = await DocumentNumberHelper.NextHandoverNumberAsync(_context);
            var now = DateTime.UtcNow;

            var job = new Models.RadioRepairJob
            {
                JobNumber = jobNumber,
                HelpdeskTicketNumber = dto.HelpdeskTicketNumber.Trim(),
                RadioId = dto.RadioId,
                RadioSerialNumber = dto.RadioSerialNumber.Trim(),
                BatterySerialNumber = dto.BatterySerialNumber?.Trim(),
                DamageDescription = dto.DamageDescription.Trim(),
                Status = RadioRepairJobStatus.Received,
                AssignedTechnicianUserId = dto.ReceivedByUserId,
                OpenedByUserId = currentUserId,
                OpenedAt = now,
                CreatedAt = now
            };
            _context.RadioRepairJobs.Add(job);
            await _context.SaveChangesAsync();

            _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
            {
                JobId = job.Id,
                FromStatus = null,
                ToStatus = RadioRepairJobStatus.Received,
                Note = "Job dibuat dari serah terima HD→Tek",
                UserId = currentUserId,
                At = now
            });

            var receiverComplete = !string.IsNullOrWhiteSpace(dto.ReceiverSignatureBase64);
            var handover = BuildHandover(dto, photos, strNumber, job.Id, currentUserId, dto.ReceivedByUserId, now, receiverComplete);
            _context.RadioHandovers.Add(handover);
            await _context.SaveChangesAsync();

            job.CurrentHandoverId = handover.Id;
            job.UpdatedAt = now;

            if (job.RadioId.HasValue)
                await AddRepairOpenedHistoryAsync(job, handover, currentUserId);

            var statusNote = receiverComplete ? "lengkap" : "menunggu TTD teknisi";
            await _activityLog.LogAsync("RadioHandover", handover.Id, "Create",
                currentUserId, $"STR {strNumber} HD→Tek ({statusNote}), Job {jobNumber}");

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(handover.Id))!;
        }

        private async Task<RadioHandoverDetailDto> CreateTechnicianToWarehouseAsync(
            CreateRadioHandoverDto dto, List<string> photos, int currentUserId)
        {
            if (!dto.RadioRepairJobId.HasValue)
                throw new ArgumentException("RadioRepairJobId wajib untuk serah terima Tek→WH.");

            var job = await _context.RadioRepairJobs.FirstOrDefaultAsync(j => j.Id == dto.RadioRepairJobId)
                ?? throw new KeyNotFoundException("Job tidak ditemukan.");

            if (job.Status != RadioRepairJobStatus.RepairCompleted)
                throw new InvalidOperationException("Job harus berstatus RepairCompleted.");

            if (job.AssignedTechnicianUserId != currentUserId)
                throw new UnauthorizedAccessException("Hanya teknisi penanggung job yang dapat serah terima ke warehouse.");

            var currentRole = await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.UserId == currentUserId)
                .Select(u => u.Role!.RoleName)
                .FirstOrDefaultAsync();
            if (!OperationalRoleNames.IsTechnicianRole(currentRole))
                throw new UnauthorizedAccessException("Hanya user dengan role teknisi yang dapat serah terima ke warehouse.");

            await ValidateUserRoleAsync(dto.ReceivedByUserId, OperationalRoleNames.Warehouse);

            var strNumber = await DocumentNumberHelper.NextHandoverNumberAsync(_context);
            var now = DateTime.UtcNow;

            var handover = BuildHandover(dto, photos, strNumber, job.Id, currentUserId, dto.ReceivedByUserId, now, true);
            handover.RadioId = job.RadioId ?? dto.RadioId;
            handover.RadioSerialNumber = job.RadioSerialNumber;
            handover.BatterySerialNumber = job.BatterySerialNumber ?? dto.BatterySerialNumber;

            _context.RadioHandovers.Add(handover);

            var fromStatus = job.Status;
            job.Status = RadioRepairJobStatus.HandedToWarehouse;
            job.CurrentHandoverId = handover.Id;
            job.UpdatedAt = now;

            _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
            {
                JobId = job.Id,
                FromStatus = fromStatus,
                ToStatus = RadioRepairJobStatus.HandedToWarehouse,
                Note = $"Serah terima {strNumber}",
                UserId = currentUserId,
                At = now
            });

            await _context.SaveChangesAsync();

            if (job.RadioId.HasValue)
                await AddRepairWarehouseHistoryAsync(job, handover, currentUserId);

            await _activityLog.LogAsync("RadioHandover", handover.Id, "Create",
                currentUserId, $"STR {strNumber} Tek→WH, Job {job.JobNumber}");

            await _context.SaveChangesAsync();
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

            var handover = BuildHandover(dto, photos, strNumber, job.Id, currentUserId, dto.ReceivedByUserId, now, true);
            handover.HandoverType = RadioHandoverType.WarehouseToHelpdesk;
            handover.RadioId = job.RadioId ?? dto.RadioId;
            handover.RadioSerialNumber = job.RadioSerialNumber;
            handover.BatterySerialNumber = job.BatterySerialNumber ?? dto.BatterySerialNumber;

            _context.RadioHandovers.Add(handover);

            var fromStatus = job.Status;
            job.Status = RadioRepairJobStatus.ReturnedToHelpdesk;
            job.ClosedAt = now;
            job.CurrentHandoverId = handover.Id;
            job.UpdatedAt = now;

            _context.RadioRepairJobStatusLogs.Add(new RadioRepairJobStatusLog
            {
                JobId = job.Id,
                FromStatus = fromStatus,
                ToStatus = RadioRepairJobStatus.ReturnedToHelpdesk,
                Note = $"Serah terima {strNumber} ke Helpdesk",
                UserId = currentUserId,
                At = now
            });

            await _context.SaveChangesAsync();

            if (job.RadioId.HasValue)
                await AddRepairReturnedToHelpdeskHistoryAsync(job, handover, currentUserId);

            await _activityLog.LogAsync("RadioHandover", handover.Id, "Create",
                currentUserId, $"STR {strNumber} WH→HD, Job {job.JobNumber}");

            await _context.SaveChangesAsync();
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

            if (handover.HandoverType != RadioHandoverType.HelpdeskToTechnician)
                throw new InvalidOperationException("Hanya serah terima HD→Tek yang dapat dilengkapi TTD penerima.");

            if (handover.Status == "Completed")
                throw new InvalidOperationException("Serah terima sudah selesai.");

            if (handover.ReceivedByUserId != currentUserId)
                throw new UnauthorizedAccessException("Hanya teknisi penerima yang dapat menandatangani.");

            var now = DateTime.UtcNow;
            handover.ReceiverSignatureBase64 = dto.ReceiverSignatureBase64;
            handover.Status = "Completed";
            handover.SignedAt = now;
            handover.UpdatedAt = now;

            await _activityLog.LogAsync("RadioHandover", handover.Id, "CompleteReceiver",
                currentUserId, $"STR {handover.HandoverNumber} — TTD teknisi dilengkapi");

            await _context.SaveChangesAsync();
            return (await GetByIdAsync(handover.Id))!;
        }

        private Models.RadioHandover BuildHandover(
            CreateRadioHandoverDto dto, List<string> photos, string strNumber, int jobId,
            int handedOverByUserId, int receivedByUserId, DateTime now, bool receiverSignatureComplete = true)
        {
            var handover = new Models.RadioHandover
            {
                HandoverNumber = strNumber,
                HandoverType = dto.HandoverType,
                RadioRepairJobId = jobId,
                RadioId = dto.RadioId,
                RadioSerialNumber = dto.RadioSerialNumber.Trim(),
                BatterySerialNumber = dto.BatterySerialNumber?.Trim(),
                RadioPhotoBase64 = photos[0],
                HandedOverSignatureBase64 = dto.HandedOverSignatureBase64,
                ReceiverSignatureBase64 = dto.ReceiverSignatureBase64,
                Remarks = dto.Remarks?.Trim(),
                HandedOverByUserId = handedOverByUserId,
                ReceivedByUserId = receivedByUserId,
                HandoverAt = now,
                SignedAt = receiverSignatureComplete ? now : null,
                Status = receiverSignatureComplete ? "Completed" : "PendingReceiverSignature",
                CreatedAt = now
            };

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

        private async Task ValidateRadioSerialAsync(int? radioId, string serialNumber)
        {
            if (!radioId.HasValue) return;
            var radio = await _context.Radios.AsNoTracking().FirstOrDefaultAsync(r => r.Id == radioId);
            if (radio == null) throw new KeyNotFoundException("Radio tidak ditemukan.");
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
                Details = $"Tiket: {job.HelpdeskTicketNumber}, Job: {job.JobNumber}, STR: {handover.HandoverNumber}, Kerusakan: {job.DamageDescription}, Teknisi: {techName}",
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
                Details = $"Job: {job.JobNumber}, STR: {handover.HandoverNumber}, Penerima Helpdesk: {hdName}",
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
                Details = $"Job: {job.JobNumber}, STR: {handover.HandoverNumber}, Penerima WH: {whName}",
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
            JobNumber = h.RadioRepairJob.JobNumber,
            HelpdeskTicketNumber = h.RadioRepairJob.HelpdeskTicketNumber,
            JobStatus = h.RadioRepairJob.Status.ToString(),
            RadioSerialNumber = h.RadioSerialNumber,
            RadioId = h.RadioId,
            BatterySerialNumber = h.BatterySerialNumber,
            HandedOverByName = h.HandedOverBy.FullName,
            ReceivedByName = h.ReceivedBy.FullName,
            HandoverAt = h.HandoverAt,
            HasRadioPhoto = h.Photos.Count > 0 || !string.IsNullOrEmpty(h.RadioPhotoBase64),
            HasHandedOverSignature = !string.IsNullOrEmpty(h.HandedOverSignatureBase64),
            HasReceiverSignature = !string.IsNullOrEmpty(h.ReceiverSignatureBase64),
            Status = h.Status,
            PhotoCount = h.Photos.Count > 0 ? h.Photos.Count : (string.IsNullOrEmpty(h.RadioPhotoBase64) ? 0 : 1),
            PreviewPhotoBase64 = h.Photos.OrderBy(p => p.SortOrder).Select(p => p.PhotoBase64).FirstOrDefault()
                ?? h.RadioPhotoBase64,
            RadioPhotoBase64 = h.RadioPhotoBase64,
            RadioPhotos = h.Photos.Count > 0
                ? h.Photos.OrderBy(p => p.SortOrder).Select(p => p.PhotoBase64).ToList()
                : (string.IsNullOrEmpty(h.RadioPhotoBase64) ? new List<string>() : new List<string> { h.RadioPhotoBase64 }),
            HandedOverSignatureBase64 = h.HandedOverSignatureBase64,
            ReceiverSignatureBase64 = h.ReceiverSignatureBase64,
            Remarks = h.Remarks,
            Accessories = h.Accessories.Select(a => new HandoverAccessoryItemDto
            {
                ItemName = string.IsNullOrWhiteSpace(a.ItemName) ? (a.AccessoryCode ?? "") : a.ItemName,
                Quantity = a.Quantity,
                Unit = a.Unit,
                Description = a.Description,
                SerialNumber = a.SerialNumber
            }).ToList()
        };
    }
}
