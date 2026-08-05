using System.Security.Claims;

namespace Pm.Helper
{
    public static class HandoverPermissionHelper
    {
        public const string CreateHd = "radio.handover.create.hd";
        public const string CreateTekWh = "radio.handover.create.tek_wh";
        public const string CreateWhHd = "radio.handover.create.wh_hd";
        public const string CreateLegacy = "radio.handover.create";

        public static bool CanCreateHelpdeskToTechnician(ClaimsPrincipal user) =>
            HasAny(user, CreateHd, CreateLegacy);

        public static bool CanCreateTechnicianToWarehouse(ClaimsPrincipal user) =>
            HasAny(user, CreateTekWh, CreateLegacy);

        public static bool CanCreateWarehouseToHelpdesk(ClaimsPrincipal user) =>
            HasAny(user, CreateWhHd, CreateLegacy);

        public static bool CanCreateTechnicianToHelpdesk(ClaimsPrincipal user) =>
            HasAny(user, CreateTekWh, CreateLegacy);

        public static bool CanCreateHelpdeskToWarehouse(ClaimsPrincipal user) =>
            HasAny(user, CreateHd, CreateLegacy);

        public static bool CanLookupRadioSerial(ClaimsPrincipal user) =>
            user.HasClaim("Permission", "radio.view") ||
            CanCreateHelpdeskToTechnician(user) ||
            CanCreateTechnicianToWarehouse(user) ||
            CanCreateWarehouseToHelpdesk(user) ||
            CanCreateTechnicianToHelpdesk(user);

        private static bool HasAny(ClaimsPrincipal user, params string[] permissions) =>
            permissions.Any(p => user.HasClaim("Permission", p));
    }
}
