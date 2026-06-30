using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Helper;
using Pm.Services;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/workshop-technicians")]
    [Authorize]
    public class WorkshopTechnicianController : ControllerBase
    {
        private readonly IWorkshopTechnicianService _service;

        public WorkshopTechnicianController(IWorkshopTechnicianService service)
        {
            _service = service;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirst("UserId")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException());

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, [FromQuery] string? role = null)
        {
            var result = await _service.GetAllAsync(includeInactive, role);
            return Ok(new { data = result });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(new { data = result });
        }

        [HttpPost]
        [Authorize(Policy = "RadioRepairSupervise")]
        public async Task<IActionResult> Create([FromBody] CreateWorkshopTechnicianDto dto)
        {
            var result = await _service.CreateAsync(dto, CurrentUserId);
            return Ok(new { data = result, message = "Teknisi berhasil ditambahkan" });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RadioRepairSupervise")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkshopTechnicianDto dto)
        {
            var result = await _service.UpdateAsync(id, dto, CurrentUserId);
            return Ok(new { data = result, message = "Teknisi berhasil diupdate" });
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RadioRepairSupervise")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id, CurrentUserId);
            return Ok(new { message = "Teknisi berhasil dihapus" });
        }
    }
}
