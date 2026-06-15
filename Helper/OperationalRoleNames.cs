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

        /// <summary>
        /// Supervisor Workshop — menerima notifikasi radio management, serah terima, dan warehouse.
        /// Role ini menggantikan "Supv MKN" yang terlalu luas cakupannya.
        /// </summary>
        public const string SupervisorWorkshop = "Supv WKS";

        /// <summary>
        /// Supervisor MKN — role lama, dipertahankan untuk backward compatibility.
        /// Sebaiknya gunakan SupervisorWorkshop untuk notif operasional.
        /// </summary>
        public const string SupervisorMkn = "Supv MKN";

        /// <summary>
        /// Role-role yang menerima notifikasi operasional radio dan workshop.
        /// Helpdesk, Teknisi WSK, dan Supervisor Workshop.
        /// </summary>
        public static readonly string[] RadioOperationalRoles = { Helpdesk, Technician, SupervisorWorkshop, Warehouse };

        /// <summary>Role yang dianggap "teknisi" untuk filter job & handover.</summary>
        public static readonly string[] TechnicianRoles = { Technician, "Teknisi WSK" };

        public static bool IsTechnicianRole(string? roleName) =>
            !string.IsNullOrWhiteSpace(roleName) &&
            TechnicianRoles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
    }
}
