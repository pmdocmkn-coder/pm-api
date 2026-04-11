using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.KpiDocument;
using Pm.Helper;
using Pm.Services;
using System.Security.Claims;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/kpi-documents")]
    [Authorize]
    public class KpiDocumentController : ControllerBase
    {
        private readonly IKpiDocumentService _service;

        public KpiDocumentController(IKpiDocumentService service)
        {
            _service = service;
        }

        private int GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        [HttpGet]
        [Authorize(Policy = "CanViewKpi")]
        public async Task<IActionResult> GetAll([FromQuery] KpiDocumentQueryDto query)
        {
            var result = await _service.GetAllAsync(query);
            return ApiResponse.Success(result);
        }

        [HttpPost]
        [Authorize(Policy = "CanCreateKpi")]
        public async Task<IActionResult> Create([FromBody] CreateKpiDocumentDto dto)
        {
            var result = await _service.CreateAsync(dto, GetUserId());
            return ApiResponse.Created(result, "Dokumen KPI berhasil ditambahkan");
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "CanUpdateKpi")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateKpiDocumentDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto, GetUserId());
                return ApiResponse.Success(result, "Data dokumen KPI diubah");
            }
            catch (Exception ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
        }

        [HttpPatch("{id}/dates")]
        [Authorize(Policy = "CanUpdateKpi")]
        public async Task<IActionResult> UpdateDates(int id, [FromBody] UpdateKpiDocumentDatesDto dto)
        {
            try
            {
                var result = await _service.UpdateDatesAsync(id, dto, GetUserId());
                return ApiResponse.Success(result, "Status/Tanggal KPI berhasil diupdate");
            }
            catch (Exception ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "CanDeleteKpi")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id, GetUserId());
                return ApiResponse.Success(null, "Dokumen KPI berhasil dihapus");
            }
            catch (Exception ex)
            {
                return ApiResponse.NotFound(ex.Message);
            }
        }

        [HttpPost("clone")]
        [Authorize(Policy = "CanCreateKpi")]
        public async Task<IActionResult> CloneFromPreviousMonth([FromQuery] string sourceMonth, [FromQuery] string targetMonth)
        {
            if (!DateTime.TryParse(sourceMonth, out var parsedSource) || 
                !DateTime.TryParse(targetMonth, out var parsedTarget))
            {
                return ApiResponse.BadRequest("Invalid date format", "Format tanggal tidak valid");
            }

            try
            {
                var result = await _service.CloneFromPreviousMonthAsync(parsedSource, parsedTarget, GetUserId());
                return ApiResponse.Created(result, $"Berhasil menyalin {result.Count} data dari {parsedSource:MMM yyyy} ke {parsedTarget:MMM yyyy}");
            }
            catch (Exception ex)
            {
                return ApiResponse.InternalServerError(ex.Message);
            }
        }

        [HttpDelete("clone")]
        [Authorize(Policy = "CanDeleteKpi")]
        public async Task<IActionResult> DeleteClonedMonth([FromQuery] string targetMonth)
        {
            if (!DateTime.TryParse(targetMonth, out var parsedTarget))
            {
                return ApiResponse.BadRequest("Invalid format", "Format parameter targetMonth tidak valid. Gunakan yyyy-MM-dd");
            }

            try
            {
                await _service.DeleteMonthDataAsync(parsedTarget, GetUserId());
                return ApiResponse.Success(null, $"Berhasil menghapus data untuk bulan {parsedTarget:MMM yyyy}");
            }
            catch (InvalidOperationException ex)
            {
                return ApiResponse.BadRequest("Invalid Operation", ex.Message);
            }
            catch (Exception ex)
            {
                return ApiResponse.InternalServerError(ex.Message);
            }
        }

        [HttpGet("export")]
        [Authorize(Policy = "CanViewKpi")] // Standard view access allows export
        public async Task<IActionResult> ExportExcel([FromQuery] KpiDocumentQueryDto query)
        {
            var excelData = await _service.ExportExcelAsync(query);
            return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"KPI_Tracking_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
        }

        [HttpPost("import")]
        [Authorize(Policy = "CanCreateKpi")] // Needs Create access since it modifies DB
        public async Task<IActionResult> ImportExcel(Microsoft.AspNetCore.Http.IFormFile file)
        {
            try
            {
                var rowCount = await _service.ImportExcelAsync(file, GetUserId());
                return ApiResponse.Created(null, $"Berhasil mengimpor {rowCount} data pelacakan KPI baru dari berkas");
            }
            catch (Exception ex)
            {
                return ApiResponse.BadRequest(ex.Message, "Gagal memproses file Excel");
            }
        }
    }
}
