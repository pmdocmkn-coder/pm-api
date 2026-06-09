using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.WorkshopTechnician;
using Pm.Services.WorkshopTechnician;

namespace Pm.Controllers.WorkshopTechnician
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

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            var result = await _service.GetAllAsync(includeInactive);
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
        [Authorize(Policy = "radio.repair.supervise")]
        public async Task<IActionResult> Create([FromBody] CreateWorkshopTechnicianDto dto)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var result = await _service.CreateAsync(dto, userId);
            return Ok(new { data = result, message = "Teknisi berhasil ditambahkan" });
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "radio.repair.supervise")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkshopTechnicianDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var result = await _service.UpdateAsync(id, dto, userId);
                return Ok(new { data = result, message = "Teknisi berhasil diupdate" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "radio.repair.supervise")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                await _service.DeleteAsync(id, userId);
                return Ok(new { message = "Teknisi berhasil dihapus" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
