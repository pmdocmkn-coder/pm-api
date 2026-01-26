using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.Models;

namespace Pm.Data;

public static class DatabaseSeeder
{
    /// <summary>
    /// Seed initial data untuk Roles, Permissions, RolePermissions, dan Super Admin
    /// Panggil di Program.cs setelah EnsureCreated()
    /// </summary>
    public static async Task SeedInitialDataAsync(this AppDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("🌱 Starting database seeding...");

            // 1. Seed Roles
            if (!await context.Roles.AnyAsync())
            {
                logger.LogInformation("Seeding Roles...");
                var roles = new List<Role>
                    {
                        new() { RoleId = 1, RoleName = "Super Admin", Description = "Full system access with all permissions", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new() { RoleId = 2, RoleName = "Admin", Description = "Administrative access with limited permissions", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new() { RoleId = 3, RoleName = "User", Description = "Standard user access", IsActive = true, CreatedAt = DateTime.UtcNow }
                    };
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
                logger.LogInformation("✅ Roles seeded successfully");
            }

            // 2. Seed Permissions
            var requiredPermissions = new List<Permission>
                {
                    // User Management
                    new() { PermissionId = 1, PermissionName = "user.view-any", Description = "View all users", Group = "User", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 2, PermissionName = "user.view", Description = "View user detail", Group = "User", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 3, PermissionName = "user.create", Description = "Create new user", Group = "User", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 4, PermissionName = "user.update", Description = "Update user", Group = "User", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 5, PermissionName = "user.delete", Description = "Delete user", Group = "User", CreatedAt = DateTime.UtcNow },
                    
                    // Role Management
                    new() { PermissionId = 6, PermissionName = "role.view-any", Description = "View all roles", Group = "Role", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 7, PermissionName = "role.view", Description = "View role detail", Group = "Role", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 8, PermissionName = "role.create", Description = "Create new role", Group = "Role", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 9, PermissionName = "role.update", Description = "Update role", Group = "Role", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 10, PermissionName = "role.delete", Description = "Delete role", Group = "Role", CreatedAt = DateTime.UtcNow },
                    
                    // Permission Management
                    new() { PermissionId = 11, PermissionName = "permission.view", Description = "View permissions", Group = "Permission", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 12, PermissionName = "permission.edit", Description = "Edit permissions", Group = "Permission", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 23, PermissionName = "permission.create", Description = "Create new permissions", Group = "Permission", CreatedAt = DateTime.UtcNow },

                    // Call Record Management
                    new() { PermissionId = 13, PermissionName = "callrecord.import", Description = "Import call records from CSV", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 14, PermissionName = "callrecord.view-any", Description = "View all call records", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 15, PermissionName = "callrecord.view", Description = "View call record details and summaries", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 16, PermissionName = "callrecord.export-excel", Description = "Export call records to Excel", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 17, PermissionName = "callrecord.export-csv", Description = "Export call records to CSV", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 18, PermissionName = "callrecord.delete", Description = "Delete call records by date", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                    
                    // System Management
                    new() { PermissionId = 19, PermissionName = "system.settings.view", Description = "View system settings", Group = "System", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 20, PermissionName = "system.settings.edit", Description = "Edit system settings", Group = "System", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 21, PermissionName = "system.audit.view", Description = "View audit logs", Group = "System", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 22, PermissionName = "delete.all-data", Description = "Reset all data (DANGER)", Group = "System", CreatedAt = DateTime.UtcNow },

                    // Inspeksi Temuan KPC
                    new() { PermissionId = 24, PermissionName = "inspeksi.temuan-kpc.view", Description = "View inspeksi temuan", Group = "Inspeksi", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 25, PermissionName = "inspeksi.temuan-kpc.create", Description = "Create inspeksi temuan", Group = "Inspeksi", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 26, PermissionName = "inspeksi.temuan-kpc.update", Description = "Update inspeksi temuan", Group = "Inspeksi", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 27, PermissionName = "inspeksi.temuan-kpc.delete", Description = "Delete inspeksi temuan", Group = "Inspeksi", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 28, PermissionName = "inspeksi.temuan-kpc.restore", Description = "Restore deleted inspeksi temuan", Group = "Inspeksi", CreatedAt = DateTime.UtcNow },

                    // Signals (NEC & SWR)
                    new() { PermissionId = 29, PermissionName = "nec.signal.view", Description = "View NEC signals", Group = "Signal", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 30, PermissionName = "nec.signal.delete", Description = "Delete NEC signals", Group = "Signal", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 31, PermissionName = "swr.signal.view", Description = "View SWR signals", Group = "Signal", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 32, PermissionName = "swr.signal.delete", Description = "Delete SWR signals", Group = "Signal", CreatedAt = DateTime.UtcNow },

                    // Letter Numbering
                    new() { PermissionId = 33, PermissionName = "letter.view", Description = "View letter numbers", Group = "Letter", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 34, PermissionName = "letter.create", Description = "Generate letter numbers", Group = "Letter", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 35, PermissionName = "letter.update", Description = "Update letter details", Group = "Letter", CreatedAt = DateTime.UtcNow },
                    new() { PermissionId = 36, PermissionName = "letter.delete", Description = "Delete letter numbers", Group = "Letter", CreatedAt = DateTime.UtcNow }
                };

            var existingPermissions = await context.Permissions.ToListAsync();
            var newPermissions = requiredPermissions
                .Where(rp => !existingPermissions.Any(ep => ep.PermissionName == rp.PermissionName))
                .ToList();

            if (newPermissions.Count > 0)
            {
                logger.LogInformation("Adding {Count} new permissions...", newPermissions.Count);
                foreach (var p in newPermissions)
                {
                    p.PermissionId = 0; // Let DB generate ID
                }
                await context.Permissions.AddRangeAsync(newPermissions);
                await context.SaveChangesAsync();
                logger.LogInformation("✅ New permissions added successfully");
            }

            // 3. Seed RolePermissions
            var allPermissions = await context.Permissions.ToListAsync();
            var existingRolePermissions = await context.RolePermissions
                .Where(rp => rp.RoleId == 1)
                .ToListAsync();

            var missingRolePermissions = allPermissions
                .Where(p => !existingRolePermissions.Any(erp => erp.PermissionId == p.PermissionId))
                .Select(p => new RolePermission
                {
                    RoleId = 1,
                    PermissionId = p.PermissionId,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (missingRolePermissions.Count > 0)
            {
                logger.LogInformation("Adding {Count} missing permissions to Super Admin role...", missingRolePermissions.Count);
                await context.RolePermissions.AddRangeAsync(missingRolePermissions);
                await context.SaveChangesAsync();
            }

            var adminRolePermissions = await context.RolePermissions
                .Where(rp => rp.RoleId == 2)
                .ToListAsync();

            var criticalPermissionNames = new[] { "delete.all-data", "permission.edit", "permission.create" };
            var adminAllowedPermissions = allPermissions
                .Where(p => !criticalPermissionNames.Contains(p.PermissionName))
                .Where(p => !adminRolePermissions.Any(arp => arp.PermissionId == p.PermissionId))
                .Select(p => new RolePermission
                {
                    RoleId = 2,
                    PermissionId = p.PermissionId,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (adminAllowedPermissions.Count > 0)
            {
                logger.LogInformation("Adding {Count} missing permissions to Admin role...", adminAllowedPermissions.Count);
                await context.RolePermissions.AddRangeAsync(adminAllowedPermissions);
                await context.SaveChangesAsync();
            }

            // 4. Seed Super Admin User
            if (!await context.Users.AnyAsync())
            {
                logger.LogInformation("Seeding Super Admin user...");
                var superAdmin = new User
                {
                    Username = "superadmin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password!"),
                    FullName = "System Administrator",
                    Email = "jupriekapratama@gmail.com",
                    IsActive = true,
                    RoleId = 1,
                    CreatedAt = DateTime.UtcNow
                };
                await context.Users.AddAsync(superAdmin);
                await context.SaveChangesAsync();
                logger.LogInformation("✅ Super Admin user seeded successfully");
            }

            logger.LogInformation("🎉 Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error seeding database");
            throw;
        }
    }
}
