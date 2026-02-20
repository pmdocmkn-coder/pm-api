using Microsoft.AspNetCore.Authorization;

namespace Pm.Helper
{
    public static class AuthorizationPolicyExtensions
    {
        public static void AddCustomAuthorizationPolicies(this AuthorizationOptions options)
        {
            //Permission Management
            options.AddPolicy("CanViewPermissions", policy =>
                policy.RequireClaim("Permission", "permission.view"));
            options.AddPolicy("CanCreatePermission", policy =>
                policy.RequireClaim("Permission", "permission.create"));
            options.AddPolicy("CanEditPermissions", policy =>
                policy.RequireClaim("Permission", "permission.edit"));
            options.AddPolicy("CanDeletePermission", policy =>
                policy.RequireClaim("Permission", "permission.delete"));



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

            // Gatepass
            options.AddPolicy("GatepassView", policy =>
                policy.RequireClaim("Permission", "gatepass.view"));
            options.AddPolicy("GatepassCreate", policy =>
                policy.RequireClaim("Permission", "gatepass.create"));
            options.AddPolicy("GatepassUpdate", policy =>
                policy.RequireClaim("Permission", "gatepass.update"));
            options.AddPolicy("GatepassDelete", policy =>
                policy.RequireClaim("Permission", "gatepass.delete"));

            // Quotation
            options.AddPolicy("QuotationView", policy =>
                policy.RequireClaim("Permission", "quotation.view"));
            options.AddPolicy("QuotationCreate", policy =>
                policy.RequireClaim("Permission", "quotation.create"));
            options.AddPolicy("QuotationUpdate", policy =>
                policy.RequireClaim("Permission", "quotation.update"));
            options.AddPolicy("QuotationDelete", policy =>
                policy.RequireClaim("Permission", "quotation.delete"));

            //Radio
            options.AddPolicy("RadioView", policy =>
                policy.RequireClaim("Permission", "radio.view"));
            options.AddPolicy("RadioCreate", policy =>
                policy.RequireClaim("Permission", "radio.create"));
            options.AddPolicy("RadioUpdate", policy =>
                policy.RequireClaim("Permission", "radio.update"));
            options.AddPolicy("RadioDelete", policy =>
                policy.RequireClaim("Permission", "radio.delete"));
            options.AddPolicy("RadioImport", policy =>
                policy.RequireClaim("Permission", "radio.import"));
            options.AddPolicy("RadioExport", policy =>
                policy.RequireClaim("Permission", "radio.export"));
            options.AddPolicy("RadioScrapView", policy =>
            policy.RequireClaim("Permission", "radio.scrap.view"));
            options.AddPolicy("RadioScrapDelete", policy =>
            policy.RequireClaim("Permission", "radio.scrap.delete"));
            options.AddPolicy("RadioScrapUpdate", policy =>
            policy.RequireClaim("Permission", "radio.scrap.update"));
            options.AddPolicy("RadioScrapCreate", policy =>
            policy.RequireClaim("Permission", "radio.scrap.create"));
            options.AddPolicy("RadioScrapExport", policy =>
            policy.RequireClaim("Permission", "radio.scrap.export"));
            options.AddPolicy("RadioScrapImport", policy =>
            policy.RequireClaim("Permission", "radio.scrap.import"));


            //Divisi
            options.AddPolicy("DivisiView", policy =>
                policy.RequireClaim("Permission", "division.view"));
            options.AddPolicy("DivisiCreate", policy =>
                policy.RequireClaim("Permission", "division.create"));
            options.AddPolicy("DivisiUpdate", policy =>
                policy.RequireClaim("Permission", "division.update"));
            options.AddPolicy("DivisiDelete", policy =>
                policy.RequireClaim("Permission", "division.delete"));

            // NEC Signal
            options.AddPolicy("NecSignalView", policy =>
                policy.RequireClaim("Permission", "nec.view"));
            options.AddPolicy("NecSignalCreate", policy =>
                policy.RequireClaim("Permission", "nec.create"));
            options.AddPolicy("NecSignalUpdate", policy =>
                policy.RequireClaim("Permission", "nec.update"));
            options.AddPolicy("NecSignalDelete", policy =>
                policy.RequireClaim("Permission", "nec.delete"));
            options.AddPolicy("NecSignalImport", policy =>
                policy.RequireClaim("Permission", "nec.import"));
            options.AddPolicy("NecSignalExport", policy =>
                policy.RequireClaim("Permission", "nec.export"));

            // SWR Signal
            options.AddPolicy("SwrSignalView", policy =>
                policy.RequireClaim("Permission", "swr.view"));
            options.AddPolicy("SwrSignalCreate", policy =>
                policy.RequireClaim("Permission", "swr.create"));
            options.AddPolicy("SwrSignalUpdate", policy =>
                policy.RequireClaim("Permission", "swr.update"));
            options.AddPolicy("SwrSignalDelete", policy =>
                policy.RequireClaim("Permission", "swr.delete"));
            options.AddPolicy("SwrSignalImport", policy =>
                policy.RequireClaim("Permission", "swr.import"));
            options.AddPolicy("SwrSignalExport", policy =>
                policy.RequireClaim("Permission", "swr.export"));

            // Inspeksi KPC
            options.AddPolicy("InspeksiView", policy =>
                policy.RequireClaim("Permission", "inspeksi.view"));
            options.AddPolicy("InspeksiCreate", policy =>
                policy.RequireClaim("Permission", "inspeksi.create"));
            options.AddPolicy("InspeksiUpdate", policy =>
                policy.RequireClaim("Permission", "inspeksi.update"));
            options.AddPolicy("InspeksiDelete", policy =>
                policy.RequireClaim("Permission", "inspeksi.delete"));
            options.AddPolicy("InspeksiExport", policy =>
                policy.RequireClaim("Permission", "inspeksi.export"));
            options.AddPolicy("InspeksiRestore", policy =>
                policy.RequireClaim("Permission", "inspeksi.restore"));


        }


    }
}