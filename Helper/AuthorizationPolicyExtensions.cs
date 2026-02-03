using Microsoft.AspNetCore.Authorization;

namespace Pm.Helper
{
    public static class AuthorizationPolicyExtensions
    {
        public static void AddCustomAuthorizationPolicies(this AuthorizationOptions options)
        {
            // Permission policies
            options.AddPolicy("CanViewPermissions", policy =>
                policy.RequireClaim("Permission", "permission.view"));
            options.AddPolicy("CanEditPermissions", policy =>
                policy.RequireClaim("Permission", "permission.edit"));
            options.AddPolicy("CanCreatePermission", policy =>
                policy.RequireClaim("Permission", "permission.create"));

            // Role policies
            options.AddPolicy("CanViewRoles", policy =>
                policy.RequireClaim("Permission", "role.view-any"));
            options.AddPolicy("CanViewDetailRoles", policy =>
                policy.RequireClaim("Permission", "role.view"));
            options.AddPolicy("CanCreateRoles", policy =>
                policy.RequireClaim("Permission", "role.create"));
            options.AddPolicy("CanUpdateRoles", policy =>
                policy.RequireClaim("Permission", "role.update"));
            options.AddPolicy("CanDeleteRoles", policy =>
                policy.RequireClaim("Permission", "role.delete"));

            // User policies
            options.AddPolicy("CanViewUsers", policy =>
                policy.RequireClaim("Permission", "user.view-any"));
            options.AddPolicy("CanViewDetailUsers", policy =>
                policy.RequireClaim("Permission", "user.view"));
            options.AddPolicy("CanCreateUsers", policy =>
                policy.RequireClaim("Permission", "user.create"));
            options.AddPolicy("CanUpdateUsers", policy =>
                policy.RequireClaim("Permission", "user.update"));
            options.AddPolicy("CanDeleteUsers", policy =>
                policy.RequireClaim("Permission", "user.delete"));

            // Call Record policies
            options.AddPolicy("CanImportCallRecords", policy =>
                policy.RequireClaim("Permission", "callrecord.import"));
            options.AddPolicy("CanViewCallRecords", policy =>
                policy.RequireClaim("Permission", "callrecord.view-any"));
            options.AddPolicy("CanViewDetailCallRecords", policy =>
                policy.RequireClaim("Permission", "callrecord.view"));
            options.AddPolicy("CanExportCallRecordsExcel", policy =>
                policy.RequireClaim("Permission", "callrecord.export-excel"));
            options.AddPolicy("CanExportCallRecordsCsv", policy =>
                policy.RequireClaim("Permission", "callrecord.export-csv"));
            options.AddPolicy("CanDeleteCallRecords", policy =>
                policy.RequireClaim("Permission", "callrecord.delete"));
            options.AddPolicy("CanDeleteAllData", policy =>
                policy.RequireClaim("Permission", "delete.all-data"));
            options.AddPolicy("CanRebuildFleetStatistics", policy =>
                policy.RequireClaim("Permission", "callrecord.rebuild-fleet-stats"));


            // Setting
            options.AddPolicy("CanViewSettings", policy =>
                policy.RequireClaim("Permission", "system.settings.view"));
            options.AddPolicy("CanEditSettings", policy =>
                policy.RequireClaim("Permission", "system.settings.edit"));
            options.AddPolicy("CanViewAuditLog", policy =>
                policy.RequireClaim("Permission", "system.audit.view"));
            options.AddPolicy("CanManageUsers", policy =>
                policy.RequireClaim("Permission", "manage.user.create", "manage.user.update", "manage.user.delete"));
            options.AddPolicy("CanManageRoles", policy =>
                policy.RequireClaim("Permission", "manage.role.create", "manage.role.update", "manage.role.delete"));

            // Inspeksi Temuan KPC
            options.AddPolicy("InspeksiTemuanKpcView", policy =>
                policy.RequireClaim("Permission", "inspeksi.temuan-kpc.view"));
            options.AddPolicy("InspeksiTemuanKpcCreate", policy =>
                policy.RequireClaim("Permission", "inspeksi.temuan-kpc.create"));
            options.AddPolicy("InspeksiTemuanKpcDelete", policy =>
                policy.RequireClaim("Permission", "inspeksi.temuan-kpc.delete"));
            options.AddPolicy("InspeksiTemuanKpcRestore", policy =>
                policy.RequireClaim("Permission", "inspeksi.temuan-kpc.restore"));
            options.AddPolicy("InspeksiTemuanKpcUpdate", policy =>
                policy.RequireClaim("Permission", "inspeksi.temuan-kpc.update"));


            options.AddPolicy("NecSignalView", policy =>
                policy.RequireClaim("Permission", "nec.signal.view"));
            options.AddPolicy("NecSignalDelete", policy =>
                policy.RequireClaim("Permission", "nec.signal.delete"));

            // SWR Signal policies
            options.AddPolicy("SwrSignalView", policy =>
                policy.RequireClaim("Permission", "swr.signal.view"));
            options.AddPolicy("SwrSignalDelete", policy =>
                policy.RequireClaim("Permission", "swr.signal.delete"));

            // Letter
            options.AddPolicy("LetterNumberView", policy =>
                policy.RequireClaim("Permission", "letter.view"));
            options.AddPolicy("LetterNumberCreate", policy =>
                policy.RequireClaim("Permission", "letter.create"));
            options.AddPolicy("LetterNumberUpdate", policy =>
                policy.RequireClaim("Permission", "letter.update"));
            options.AddPolicy("LetterNumberDelete", policy =>
                policy.RequireClaim("Permission", "letter.delete"));

        }


    }
}