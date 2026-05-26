namespace Pm.Helper
{
    /// <summary>
    /// Kunci internal unik per pekerjaan perbaikan (bukan nomor dokumen tampilan).
    /// Referensi pengguna: No. Tiket Helpdesk + STR + SN.
    /// </summary>
    public static class RepairJobReference
    {
        public static string InternalKey(string helpdeskTicketNumber, string radioSerialNumber) =>
            $"{helpdeskTicketNumber.Trim()}::{radioSerialNumber.Trim()}";
    }
}
