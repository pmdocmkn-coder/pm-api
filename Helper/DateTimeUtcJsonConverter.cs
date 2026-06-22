using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pm.Helper;

/// <summary>
/// JSON converter yang memastikan semua DateTime di-serialize dengan suffix "Z" (UTC ISO 8601).
/// Ini penting agar JavaScript di browser bisa otomatis mengkonversi waktu UTC
/// ke timezone lokal user (misalnya WITA = UTC+8).
///
/// Tanpa converter ini, EF Core mengembalikan DateTime dari MySQL sebagai DateTimeKind.Unspecified,
/// yang di-serialize tanpa "Z". JavaScript lalu menampilkan jam UTC mentah, bukan jam lokal.
/// </summary>
public class DateTimeUtcJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dt = reader.GetDateTime();
        // Pastikan selalu UTC
        return dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Paksa semua DateTime ditulis sebagai UTC dengan suffix "Z"
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}

/// <summary>
/// Versi nullable dari DateTimeUtcJsonConverter untuk DateTime? fields.
/// </summary>
public class NullableDateTimeUtcJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var dt = reader.GetDateTime();
        return dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var utc = value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);

        writer.WriteStringValue(utc.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }
}
