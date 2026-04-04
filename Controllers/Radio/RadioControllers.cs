using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.Radio;
using Pm.Helper;
using Pm.Services;
using System.Security.Claims;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/radio-trunking")]
    public class RadioTrunkingController : ControllerBase
    {
        private readonly IRadioTrunkingService _service;

        public RadioTrunkingController(IRadioTrunkingService service)
        {
            _service = service;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        [Authorize(Policy = "RadioView")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] RadioTrunkingQueryDto query)
        {
            var result = await _service.GetAllAsync(query);
            return ApiResponse.Success(result);
        }

        [Authorize(Policy = "RadioView")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return ApiResponse.NotFound("Radio Trunking tidak ditemukan");
            return ApiResponse.Success(result);
        }

        [HttpPost]
        [Authorize(Policy = "RadioCreate")]
        public async Task<IActionResult> Create([FromBody] CreateRadioTrunkingDto dto)
        {
            var result = await _service.CreateAsync(dto, GetUserId());
            return ApiResponse.Created(result, "Radio Trunking berhasil ditambahkan");
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "RadioUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRadioTrunkingDto dto)
        {
            var result = await _service.UpdateAsync(id, dto, GetUserId());
            if (result == null) return ApiResponse.NotFound("Radio Trunking tidak ditemukan");
            return ApiResponse.Success(result, "Radio Trunking berhasil diupdate");
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "RadioDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return ApiResponse.NotFound("Radio Trunking tidak ditemukan");
            return ApiResponse.Success(null, "Radio Trunking berhasil dihapus");
        }

        [HttpDelete("clear")]
        [Authorize(Policy = "RadioDelete")]
        public async Task<IActionResult> ClearAll()
        {
            var userId = GetUserId();
            var deletedCount = await _service.ClearAllAsync(userId);
            return ApiResponse.Success(new { deletedCount }, $"Berhasil menghapus seluruh {deletedCount} data Trunking.");
        }

        [Authorize(Policy = "RadioView")]
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(int id)
        {
            var result = await _service.GetHistoryAsync(id);
            return ApiResponse.Success(result);
        }

        [HttpPost("import")]
        [Authorize(Policy = "RadioImport")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ApiResponse.BadRequest("file", "File tidak valid");

            using var stream = file.OpenReadStream();
            var (success, failed, errors) = await _service.ImportCsvAsync(stream, GetUserId());

            return ApiResponse.Success(new { success, failed, errors }, $"Import selesai: {success} berhasil, {failed} gagal");
        }

        [HttpGet("export")]
        [Authorize(Policy = "RadioExport")]
        public async Task<IActionResult> ExportCsv([FromQuery] RadioTrunkingQueryDto? query)
        {
            var bytes = await _service.ExportCsvAsync(query);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"radio_trunking_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpGet("template")]
        public IActionResult GetTemplate()
        {
            var bytes = _service.GetImportTemplate();
            return File(bytes, "text/csv", "radio_trunking_template.csv");
        }
    }

    [ApiController]
    [Route("api/radio-conventional")]
    public class RadioConventionalController : ControllerBase
    {
        private readonly IRadioConventionalService _service;

        public RadioConventionalController(IRadioConventionalService service)
        {
            _service = service;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        [Authorize(Policy = "RadioView")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] RadioConventionalQueryDto query)
        {
            var result = await _service.GetAllAsync(query);
            return ApiResponse.Success(result);
        }

        [Authorize(Policy = "RadioView")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return ApiResponse.NotFound("Radio Conventional tidak ditemukan");
            return ApiResponse.Success(result);
        }

        [HttpPost]
        [Authorize(Policy = "RadioCreate")]
        public async Task<IActionResult> Create([FromBody] CreateRadioConventionalDto dto)
        {
            var result = await _service.CreateAsync(dto, GetUserId());
            return ApiResponse.Created(result, "Radio Conventional berhasil ditambahkan");
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "RadioUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRadioConventionalDto dto)
        {
            var result = await _service.UpdateAsync(id, dto, GetUserId());
            if (result == null) return ApiResponse.NotFound("Radio Conventional tidak ditemukan");
            return ApiResponse.Success(result, "Radio Conventional berhasil diupdate");
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "RadioDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return ApiResponse.NotFound("Radio Conventional tidak ditemukan");
            return ApiResponse.Success(null, "Radio Conventional berhasil dihapus");
        }

        [HttpDelete("clear")]
        [Authorize(Policy = "RadioDelete")]
        public async Task<IActionResult> ClearAll()
        {
            var userId = GetUserId();
            var deletedCount = await _service.ClearAllAsync(userId);
            return ApiResponse.Success(new { deletedCount }, $"Berhasil menghapus seluruh {deletedCount} data Conventional.");
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetHistory(int id)
        {
            var result = await _service.GetHistoryAsync(id);
            return ApiResponse.Success(result);
        }

        [HttpPost("import")]
        [Authorize(Policy = "RadioImport")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ApiResponse.BadRequest("file", "File tidak valid");

            using var stream = file.OpenReadStream();
            var (success, failed, errors) = await _service.ImportCsvAsync(stream, GetUserId());
            return ApiResponse.Success(new { success, failed, errors });
        }

        [HttpGet("export")]
        [Authorize(Policy = "RadioExport")]
        public async Task<IActionResult> ExportCsv([FromQuery] RadioConventionalQueryDto? query)
        {
            var bytes = await _service.ExportCsvAsync(query);
            return File(bytes, "text/csv", $"radio_conventional_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet("template")]
        public IActionResult GetTemplate()
        {
            var bytes = _service.GetImportTemplate();
            return File(bytes, "text/csv", "radio_conventional_template.csv");
        }
    }

    [ApiController]
    [Route("api/radio-grafir")]
    public class RadioGrafirController : ControllerBase
    {
        private readonly IRadioGrafirService _service;

        public RadioGrafirController(IRadioGrafirService service)
        {
            _service = service;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        [Authorize(Policy = "RadioView")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] RadioGrafirQueryDto query)
        {
            var result = await _service.GetAllAsync(query);
            return ApiResponse.Success(result);
        }

        [Authorize(Policy = "RadioView")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return ApiResponse.NotFound("Radio Grafir tidak ditemukan");
            return ApiResponse.Success(result);
        }

        [HttpPost]
        [Authorize(Policy = "RadioCreate")]
        public async Task<IActionResult> Create([FromBody] CreateRadioGrafirDto dto)
        {
            var result = await _service.CreateAsync(dto, GetUserId());
            return ApiResponse.Created(result, "Radio Grafir berhasil ditambahkan");
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "RadioUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRadioGrafirDto dto)
        {
            var result = await _service.UpdateAsync(id, dto, GetUserId());
            if (result == null) return ApiResponse.NotFound("Radio Grafir tidak ditemukan");
            return ApiResponse.Success(result, "Radio Grafir berhasil diupdate");
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "RadioDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return ApiResponse.NotFound("Radio Grafir tidak ditemukan");
            return ApiResponse.Success(null, "Radio Grafir berhasil dihapus");
        }

        [HttpDelete("clear")]
        [Authorize(Policy = "RadioDelete")]
        public async Task<IActionResult> ClearAll()
        {
            var userId = GetUserId();
            var deletedCount = await _service.ClearAllAsync(userId);
            return ApiResponse.Success(new { deletedCount }, $"Berhasil menghapus seluruh {deletedCount} data Grafir.");
        }

        [Authorize(Policy = "RadioView")]
        [HttpGet("{id}/trunking")]
        public async Task<IActionResult> GetLinkedTrunking(int id)
        {
            var result = await _service.GetLinkedTrunkingAsync(id);
            return ApiResponse.Success(result);
        }

        [Authorize(Policy = "RadioView")]
        [HttpGet("{id}/conventional")]
        public async Task<IActionResult> GetLinkedConventional(int id)
        {
            var result = await _service.GetLinkedConventionalAsync(id);
            return ApiResponse.Success(result);
        }

        [HttpPost("import")]
        [Authorize(Policy = "RadioImport")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ApiResponse.BadRequest("file", "File tidak valid");

            using var stream = file.OpenReadStream();
            var (success, failed, errors) = await _service.ImportCsvAsync(stream, GetUserId());
            return ApiResponse.Success(new { success, failed, errors });
        }

        [HttpGet("export")]
        [Authorize(Policy = "RadioExport")]
        public async Task<IActionResult> ExportCsv([FromQuery] RadioGrafirQueryDto? query)
        {
            var bytes = await _service.ExportCsvAsync(query);
            return File(bytes, "text/csv", $"radio_grafir_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpGet("template")]
        public IActionResult GetTemplate()
        {
            var bytes = _service.GetImportTemplate();
            return File(bytes, "text/csv", "radio_grafir_template.csv");
        }
    }

    [ApiController]
    [Route("api/radio-scrap")]
    [Authorize(Policy = "RadioScrapView")]
    public class RadioScrapController : ControllerBase
    {
        private readonly IRadioScrapService _service;

        public RadioScrapController(IRadioScrapService service)
        {
            _service = service;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        [Authorize(Policy = "RadioScrapView")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] RadioScrapQueryDto query)
        {
            var result = await _service.GetAllAsync(query);
            return ApiResponse.Success(result);
        }

        [Authorize(Policy = "RadioScrapView")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return ApiResponse.NotFound("Radio Scrap tidak ditemukan");
            return ApiResponse.Success(result);
        }

        [HttpPost]
        [Authorize(Policy = "RadioScrapCreate")]
        public async Task<IActionResult> Create([FromBody] CreateRadioScrapDto dto)
        {
            var result = await _service.CreateAsync(dto, GetUserId());
            return ApiResponse.Created(result, "Radio Scrap berhasil ditambahkan");
        }

        [HttpPost("from-trunking/{id}")]
        [Authorize(Policy = "RadioScrapCreate")]
        public async Task<IActionResult> ScrapFromTrunking(int id, [FromBody] ScrapFromRadioDto dto)
        {
            var result = await _service.ScrapFromTrunkingAsync(id, dto, GetUserId());
            if (result == null) return ApiResponse.NotFound("Radio Trunking tidak ditemukan");
            return ApiResponse.Created(result, "Radio berhasil di-scrap");
        }

        [HttpPost("from-conventional/{id}")]
        [Authorize(Policy = "RadioScrapCreate")]
        public async Task<IActionResult> ScrapFromConventional(int id, [FromBody] ScrapFromRadioDto dto)
        {
            var result = await _service.ScrapFromConventionalAsync(id, dto, GetUserId());
            if (result == null) return ApiResponse.NotFound("Radio Conventional tidak ditemukan");
            return ApiResponse.Created(result, "Radio berhasil di-scrap");
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "RadioScrapUpdate")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateRadioScrapDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (result == null) return ApiResponse.NotFound("Radio Scrap tidak ditemukan");
            return ApiResponse.Success(result, "Radio Scrap berhasil diupdate");
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "RadioScrapDelete")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return ApiResponse.NotFound("Radio Scrap tidak ditemukan");
            return ApiResponse.Success(null, "Radio Scrap berhasil dihapus");
        }

        [Authorize(Policy = "RadioScrapView")]
        [HttpGet("yearly-summary")]
        public async Task<IActionResult> GetYearlySummary([FromQuery] int? year)
        {
            var targetYear = year ?? DateTime.Now.Year;
            var result = await _service.GetYearlySummaryAsync(targetYear);
            return ApiResponse.Success(result);
        }

        [HttpGet("export")]
        [Authorize(Policy = "RadioExport")]
        public async Task<IActionResult> ExportCsv([FromQuery] RadioScrapQueryDto? query)
        {
            var bytes = await _service.ExportCsvAsync(query);
            return File(bytes, "text/csv", $"radio_scrap_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
