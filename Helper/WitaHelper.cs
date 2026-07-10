namespace Pm.Helper;

/// <summary>
/// Helper terpusat untuk pengelolaan zona waktu WITA (UTC+8).
///
/// Aturan penggunaan:
///   - Simpan ke database    → selalu gunakan DateTime.UtcNow (UTC).
///   - Tampilkan ke user     → gunakan WitaHelper.Now atau WitaHelper.ToWita(utcDateTime).
///   - Filter/query tanggal  → gunakan WitaHelper.Today agar hari tidak meleset 8 jam.
///
/// Jangan pernah menggunakan DateTime.Now atau DateTime.Today secara langsung
/// kecuali untuk keperluan non-database (nama file, copyright, log lokal).
/// </summary>
public static class WitaHelper
{
    // WITA = UTC+8 (Kalimantan Timur, Sulawesi, Bali, dst)
    private static readonly TimeSpan WitaOffset = TimeSpan.FromHours(8);
    private static readonly TimeZoneInfo WitaZone =
        TimeZoneInfo.CreateCustomTimeZone("WITA", WitaOffset, "WITA (UTC+8)", "WITA");

    // ──────────────────────────────────────────────────────────────
    // Waktu sekarang dalam WITA
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Waktu saat ini dalam zona WITA (UTC+8).
    /// Gunakan untuk keperluan display / label response. JANGAN disimpan ke DB (simpan UtcNow).
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, WitaZone);

    /// <summary>
    /// Tanggal hari ini dalam WITA (tanpa komponen jam).
    /// Gunakan sebagai pengganti <c>DateTime.Today</c> agar tidak meleset
    /// saat server berjalan di UTC dan user berada di WITA (UTC+8).
    /// </summary>
    public static DateTime Today => Now.Date;

    // ──────────────────────────────────────────────────────────────
    // Konversi
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Konversi DateTime UTC ke waktu WITA.
    /// </summary>
    public static DateTime ToWita(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utc, WitaZone);
    }

    /// <summary>
    /// Konversi DateTime? UTC ke waktu WITA. Return null jika null.
    /// </summary>
    public static DateTime? ToWita(DateTime? utcDateTime)
        => utcDateTime.HasValue ? ToWita(utcDateTime.Value) : null;

    /// <summary>
    /// Konversi waktu WITA ke UTC (untuk disimpan ke DB).
    /// </summary>
    public static DateTime ToUtc(DateTime witaDateTime)
        => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(witaDateTime, DateTimeKind.Unspecified),
            WitaZone);

    // ──────────────────────────────────────────────────────────────
    // Format display
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Format DateTime UTC ke string yang human-readable dalam WITA.
    /// Contoh output: "10 Jul 2026, 16:27 WITA"
    /// </summary>
    public static string Format(DateTime utcDateTime, string format = "dd MMM yyyy, HH:mm 'WITA'")
        => ToWita(utcDateTime).ToString(format);

    /// <summary>
    /// Format DateTime? UTC ke string WITA. Return "-" jika null.
    /// </summary>
    public static string Format(DateTime? utcDateTime, string format = "dd MMM yyyy, HH:mm 'WITA'")
        => utcDateTime.HasValue ? Format(utcDateTime.Value, format) : "-";
}
