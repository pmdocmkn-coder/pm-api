using Microsoft.AspNetCore.Mvc;
using Pm.Services;
using Pm.Helper;

namespace Pm.Controllers
{
    /// <summary>
    /// Public endpoint for QR code verification — no auth required.
    /// </summary>
    [ApiController]
    [Route("api/verify")]
    [Produces("application/json")]
    public class VerificationController : ControllerBase
    {
        private readonly IGatepassService _gatepassService;
        private readonly ILogger<VerificationController> _logger;

        public VerificationController(IGatepassService gatepassService, ILogger<VerificationController> logger)
        {
            _gatepassService = gatepassService;
            _logger = logger;
        }

        /// <summary>
        /// Verify a gatepass by its verification token (from QR code scan).
        /// This is a PUBLIC endpoint — no auth required.
        /// </summary>
        [HttpGet("gatepass/{token}")]
        public async Task<IActionResult> VerifyGatepass(string token)
        {
            try
            {
                var result = await _gatepassService.GetGatepassByVerificationTokenAsync(token);
                if (result == null)
                {
                    return ApiResponse.NotFound("Token verifikasi tidak valid atau gatepass tidak ditemukan");
                }

                return ApiResponse.Success(new
                {
                    verified = true,
                    gatepass = result
                }, "Verifikasi berhasil — tanda tangan digital valid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying gatepass token: {Token}", token);
                return ApiResponse.InternalServerError("Verifikasi gagal: " + ex.Message);
            }
        }
    }
}
