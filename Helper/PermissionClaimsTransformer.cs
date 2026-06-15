using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pm.Data;

namespace Pm.Helper
{
    /// <summary>
    /// Mengambil permissions dari database dan menambahkannya sebagai claims
    /// pada setiap request yang sudah terautentikasi.
    /// Ini menggantikan pendekatan lama yang menyimpan permissions di dalam JWT token.
    /// Cache per-user selama 5 menit agar tidak query DB di setiap request.
    /// </summary>
    public class PermissionClaimsTransformer : IClaimsTransformation
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public PermissionClaimsTransformer(IServiceScopeFactory scopeFactory, IMemoryCache cache)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Hanya proses jika user sudah terautentikasi
            if (principal.Identity?.IsAuthenticated != true)
                return principal;

            // Ambil userId dari token
            var userIdStr = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId))
                return principal;

            // Cek apakah claims Permission sudah ada (hindari double-inject)
            if (principal.HasClaim(c => c.Type == "Permission"))
                return principal;

            // Ambil permissions dari cache atau DB
            var cacheKey = $"UserPermissions_{userId}";
            if (!_cache.TryGetValue(cacheKey, out List<string>? permissions))
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Query: User -> Role -> RolePermissions -> Permission
                var roleId = await context.Users
                    .AsNoTracking()
                    .Where(u => u.UserId == userId && u.IsActive)
                    .Select(u => u.RoleId)
                    .FirstOrDefaultAsync();

                if (roleId > 0)
                {
                    permissions = await context.RolePermissions
                        .AsNoTracking()
                        .Where(rp => rp.RoleId == roleId)
                        .Select(rp => rp.Permission.PermissionName)
                        .ToListAsync();
                }
                else
                {
                    permissions = new List<string>();
                }

                // Simpan ke cache
                _cache.Set(cacheKey, permissions, CacheDuration);
            }

            // Tambahkan permission claims ke principal
            if (permissions != null && permissions.Count > 0)
            {
                var identity = new ClaimsIdentity();
                foreach (var permission in permissions)
                {
                    identity.AddClaim(new Claim("Permission", permission));
                }
                principal.AddIdentity(identity);
            }

            return principal;
        }

        /// <summary>
        /// Panggil method ini untuk menghapus cache permission user tertentu,
        /// misalnya saat admin mengubah permission role.
        /// </summary>
        public static void InvalidateCache(IMemoryCache cache, int userId)
        {
            cache.Remove($"UserPermissions_{userId}");
        }

        /// <summary>
        /// Panggil method ini untuk menghapus cache permission semua user di role tertentu.
        /// </summary>
        public static void InvalidateCacheForRole(IMemoryCache cache, AppDbContext context, int roleId)
        {
            var userIds = context.Users
                .Where(u => u.RoleId == roleId)
                .Select(u => u.UserId)
                .ToList();

            foreach (var uid in userIds)
            {
                cache.Remove($"UserPermissions_{uid}");
            }
        }
    }
}
