using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/operational-documents")]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class OperationalDocumentController(
        IOperationalDocumentService _service,
        DocumentExpiryNotificationService _notificationService,
        ILogger<OperationalDocumentController> _logger) : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = "OperationalDocumentView")]
        public async Task<IActionResult> GetAll([FromQuery] OperationalDocumentQueryDto query)
        {
            try
            {
                var result = await _service.GetAllAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operational documents");
                return ApiResponse.InternalServerError("Get Operational Documents gagal: " + ex.Message);
            }
        }

        [HttpGet("summary")]
        [Authorize(Policy = "OperationalDocumentView")]
        public async Task<IActionResult> GetSummary()
        {
            try
            {
                var result = await _service.GetSummaryAsync();
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operational document summary");
                return ApiResponse.InternalServerError("Get Summary gagal: " + ex.Message);
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "OperationalDocumentView")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting operational document {Id}", id);
                return ApiResponse.InternalServerError("Get Document gagal: " + ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Policy = "OperationalDocumentCreate")]
        public async Task<IActionResult> Create([FromBody] OperationalDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var result = await _service.CreateAsync(dto);
                return ApiResponse.Created(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating document");
                return ApiResponse.BadRequest("Create Document", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating operational document");
                return ApiResponse.InternalServerError("Create Document gagal: " + ex.Message);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "OperationalDocumentUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] OperationalDocumentUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var result = await _service.UpdateAsync(id, dto);
                return ApiResponse.Success(result);
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return ApiResponse.BadRequest("Update Document", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating operational document {Id}", id);
                return ApiResponse.InternalServerError("Update Document gagal: " + ex.Message);
            }
        }

        [HttpPatch("{id}/follow-up-status")]
        [Authorize(Policy = "OperationalDocumentUpdate")]
        public async Task<IActionResult> UpdateFollowUpStatus(int id, [FromBody] UpdateFollowUpStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { data = ModelState });

            try
            {
                var result = await _service.UpdateFollowUpStatusAsync(id, dto.Status, dto.Remark);
                return ApiResponse.Success(result, "Status tindak lanjut berhasil diperbarui");
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return ApiResponse.BadRequest("Update Follow Up Status", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating follow-up status for document {Id}", id);
                return ApiResponse.InternalServerError("Update Follow Up Status gagal: " + ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "OperationalDocumentDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return ApiResponse.Success(new { }, "Dokumen berhasil dihapus");
            }
            catch (KeyNotFoundException ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting operational document {Id}", id);
                return ApiResponse.InternalServerError("Delete Document gagal: " + ex.Message);
            }
        }

        /// <summary>
        /// Trigger pengiriman notifikasi WA secara manual (untuk testing).
        /// Menjalankan job yang sama persis dengan yang berjalan tiap jam 7 pagi.
        /// </summary>
        [HttpPost("trigger-notification")]
        [Authorize(Policy = "OperationalDocumentView")]
        public async Task<IActionResult> TriggerNotification()
        {
            try
            {
                _logger.LogInformation("[DocExpiry] Manual trigger oleh user.");
                await _notificationService.RunNotificationJobAsync(CancellationToken.None);
                return ApiResponse.Success(new { triggeredAt = DateTime.Now }, "Job notifikasi berhasil dijalankan. Cek WA Anda!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat manual trigger notifikasi");
                return ApiResponse.InternalServerError("Trigger gagal: " + ex.Message);
            }
        }

        /// <summary>
        /// Kirim notifikasi WA paksa untuk 1 dokumen (by ID).
        /// Khusus Super Admin — mengabaikan threshold tanggal.
        /// </summary>
        [HttpPost("{id}/send-notification")]
        [Authorize(Policy = "OperationalDocumentSendNotification")]
        public async Task<IActionResult> SendNotification(int id)
        {
            try
            {
                var (success, message) = await _notificationService.SendForceNotificationAsync(id);
                if (!success)
                    return ApiResponse.BadRequest("Send Notification", message);

                return ApiResponse.Success(new { documentId = id, sentAt = DateTime.Now }, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mengirim force notifikasi untuk dokumen {Id}", id);
                return ApiResponse.InternalServerError("Gagal: " + ex.Message);
            }
        }

        public class BulkNotificationRequest
        {
            public string? GroupName { get; set; }
            public string? Type { get; set; }
            public string? ExpiryStatus { get; set; }
        }

        /// <summary>
        /// Kirim notifikasi WA paksa untuk dokumen berdasarkan filter.
        /// Khusus Super Admin — mengabaikan threshold tanggal.
        /// </summary>
        [HttpPost("send-notification-bulk")]
        [Authorize(Policy = "OperationalDocumentSendNotification")]
        public async Task<IActionResult> SendNotificationBulk([FromBody] BulkNotificationRequest req)
        {
            try
            {
                var (success, message, sentCount) = await _notificationService.SendForceNotificationBulkAsync(req.GroupName, req.Type, req.ExpiryStatus);
                if (!success)
                    return ApiResponse.BadRequest("Notification", message);
                    
                return ApiResponse.Success(new { sentCount }, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk send notification");
                return ApiResponse.InternalServerError("Gagal: " + ex.Message);
            }
        }
    }

    public class UpdateFollowUpStatusDto
    {
        public required string Status { get; set; }
        public string? Remark { get; set; }
    }
}
