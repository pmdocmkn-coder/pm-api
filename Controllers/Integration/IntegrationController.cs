using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Services;
using Pm.Helper;

namespace Pm.Controllers
{
    [ApiController]
    [Route("api/integration")]
    [Authorize]
    public class IntegrationController : ControllerBase
    {
        private readonly ISihepiIntegrationService _sihepiIntegrationService;

        public IntegrationController(ISihepiIntegrationService sihepiIntegrationService)
        {
            _sihepiIntegrationService = sihepiIntegrationService;
        }

        [HttpGet("mkn-tickets")]
        public async Task<IActionResult> GetMknTickets()
        {
            var tickets = await _sihepiIntegrationService.GetTicketsAsync();
            return ApiResponse.Success(tickets, "Berhasil mengambil data tiket SIHEPI dari MknSmart.");
        }
    }
}
