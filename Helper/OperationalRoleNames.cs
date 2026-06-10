namespace Pm.Helper
{
    /// <summary>
    /// Nama role operasional — harus sama persis dengan kolom Roles.RoleName di database.
    /// Ubah di sini jika di Settings memakai nama lain (mis. "Teknisi WSK" bukan "Teknisi").
    /// </summary>
    public static class OperationalRoleNames
    {
        public const string Helpdesk = "Helpdesk";
        public const string Technician = "Teknisi WSK";
        public const string Warehouse = "Warehouse";
        public const string SupervisorWarehouse = "Supervisor Warehouse";
        public const string SupervisorMkn = "Supv MKN";

        /// <summary>Role yang dianggap "teknisi" untuk filter job & handover.</summary>
        public static readonly string[] TechnicianRoles = { Technician, "Teknisi WSK" };

        public static bool IsTechnicianRole(string? roleName) =>
            !string.IsNullOrWhiteSpace(roleName) &&
            TechnicianRoles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
    }
}
