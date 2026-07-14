using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pm.Helper;
using Pm.Services.PmSchedule;

namespace Pm.Controllers.PmSchedule
{
    [ApiController]
    [Route("api/pm-schedules/dashboard")]
    [Authorize]
    public class PmScheduleDashboardController : ControllerBase
    {
        private readonly IPmScheduleService _pmScheduleService;
        private readonly ILogger<PmScheduleDashboardController> _logger;

        public PmScheduleDashboardController(IPmScheduleService pmScheduleService, ILogger<PmScheduleDashboardController> logger)
        {
            _pmScheduleService = pmScheduleService;
            _logger = logger;
        }

        [HttpGet("compliance")]
        [Authorize(Policy = "PmScheduleView")]
        public async Task<IActionResult> GetComplianceDashboard([FromQuery] int year)
        {
            if (year <= 0)
                year = DateTime.UtcNow.AddHours(7).Year; // Default to current year in WIB

            try
            {
                var result = await _pmScheduleService.GetComplianceDashboardAsync(year);
                return ApiResponse.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PM compliance dashboard for year {Year}", year);
                return ApiResponse.InternalServerError("Gagal mengambil data dashboard PM: " + ex.Message);
            }
        }
    }
}
