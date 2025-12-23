using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pm.DTOs;
using Pm.Services;
using Pm.Helper;

namespace Pm.Controllers
{
    [Route("api/role-permissions")]
    [ApiController]
    [Produces("application/json")]
    [ApiConventionType(typeof(DefaultApiConventions))]
    public class RolePermissionController : ControllerBase
    {
        private readonly IRolePermissionService _rolePermissionService;
        private readonly ILogger<RolePermissionController> _logger;

        public RolePermissionController(
            IRolePermissionService rolePermissionService,
            ILogger<RolePermissionController> logger)
        {
            _rolePermissionService = rolePermissionService;
            _logger = logger;
        }

        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet]
        public async Task<IActionResult> GetAllRolePermissions()
        {
            var rolePermissions = await _rolePermissionService.GetAllRolePermissionsAsync();
            return ApiResponse.Success(rolePermissions, "Role permissions berhasil dimuat");
        }

        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-role/{roleId}")]
        public async Task<IActionResult> GetPermissionsByRole(int roleId)
        {
            var result = await _rolePermissionService.GetPermissionsByRoleAsync(roleId);
            if (result == null)
            {
                return ApiResponse.NotFound("Role tidak ditemukan");
            }

            return ApiResponse.Success(result, "Permissions untuk role berhasil dimuat");
        }

        [HttpPost]
        [Authorize(Policy = "CanEditPermissions")]
        public async Task<IActionResult> AddPermissionToRole([FromBody] CreateRolePermissionDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _rolePermissionService.AddPermissionToRoleAsync(dto);
                if (result == null)
                {
                    return ApiResponse.BadRequest("message", "Permission sudah ada untuk role ini");
                }

                return ApiResponse.Created(result, "Permission berhasil ditambahkan ke role");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding permission to role");
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        [HttpPut("role/{roleId}")]
        [Authorize(Policy = "CanEditPermissions")]
        public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { data = ModelState });
            }

            try
            {
                var result = await _rolePermissionService.UpdateRolePermissionsAsync(roleId, dto.PermissionIds);
                if (result == null)
                {
                    return ApiResponse.NotFound("Role tidak ditemukan");
                }

                return ApiResponse.Success(result, "Permissions untuk role berhasil diperbarui");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role permissions");
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        [HttpDelete("role/{roleId}/permission/{permissionId}")]
        [Authorize(Policy = "CanEditPermissions")]
        public async Task<IActionResult> RemovePermissionFromRole(int roleId, int permissionId)
        {
            try
            {
                var result = await _rolePermissionService.RemovePermissionFromRoleAsync(roleId, permissionId);
                if (!result)
                {
                    return ApiResponse.NotFound("Role permission tidak ditemukan");
                }

                return ApiResponse.Success(new { }, "Permission berhasil dihapus dari role");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing permission from role");
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }

        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("by-permission/{permissionId}")]
        public async Task<IActionResult> GetRolesByPermission(int permissionId)
        {
            var roles = await _rolePermissionService.GetRolesByPermissionAsync(permissionId);
            if (roles == null)
            {
                return ApiResponse.NotFound("Permission tidak ditemukan");
            }

            return ApiResponse.Success(roles, "Roles untuk permission berhasil dimuat");
        }

        [Authorize(Policy = "CanViewPermissions")]
        [HttpGet("matrix")]
        public async Task<IActionResult> GetPermissionMatrix()
        {
            try
            {
                _logger.LogInformation("📊 Fetching permission matrix...");
                var matrix = await _rolePermissionService.GetPermissionMatrixAsync();
                _logger.LogInformation($"✅ Matrix loaded: {matrix.Count} roles");

                return ApiResponse.Success(matrix, "Permission matrix berhasil dimuat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting permission matrix");
                return ApiResponse.InternalServerError(ex.Message);
            }
        }
    }
}