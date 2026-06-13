namespace Pm.Helper
{
    /// <summary>
    /// Permission names untuk notifikasi.
    /// Gunakan ini bersama INotificationService.CreateForPermissionAsync() 
    /// agar notif dikirim hanya ke user/role yang memiliki permission tersebut.
    ///
    /// Cara assign permission ke role:
    ///   Settings → Permission → pilih role → centang permission yang diinginkan
    ///
    /// Cara pakai di service:
    ///   await _notificationService.CreateForPermissionAsync(
    ///       NotificationPermissions.RadioRepair, dto);
    /// </summary>
    public static class NotificationPermissions
    {
        // ── Radio Repair (Status Perbaikan) ──────────────────────────────
        /// <summary>
        /// Terima notif saat ada perubahan status perbaikan radio
        /// (mulai, selesai, monitoring, scrap, material, dll).
        /// Assign ke: Supv WKS, Helpdesk
        /// </summary>
        public const string RadioRepair = "notification.radio.repair";

        // ── Radio Handover (Serah Terima) — dipecah per alur ─────────────

        /// <summary>
        /// Terima notif saat serah terima Helpdesk → Teknisi (HD buat STR, teknisi TTD).
        /// Assign ke: Supv WKS, Helpdesk, Teknisi WSK
        /// </summary>
        public const string RadioHandoverHdTek = "notification.radio.handover.hd_tek";

        /// <summary>
        /// Terima notif saat serah terima Teknisi → Warehouse (radio masuk WH dari workshop).
        /// Assign ke: Supv WKS, Warehouse
        /// </summary>
        public const string RadioHandoverTekWh = "notification.radio.handover.tek_wh";

        /// <summary>
        /// Terima notif saat serah terima Warehouse → Helpdesk (radio keluar WH kembali ke HD).
        /// Assign ke: Supv WKS, Helpdesk, Warehouse
        /// </summary>
        public const string RadioHandoverWhHd = "notification.radio.handover.wh_hd";

        // ── Warehouse ────────────────────────────────────────────────────
        /// <summary>
        /// Terima notif saat ada pengajuan / persetujuan / pengembalian part warehouse.
        /// Assign ke: Supv WKS, Supervisor Warehouse, Warehouse
        /// </summary>
        public const string WarehouseBorrow = "notification.warehouse.borrow";
    }
}
