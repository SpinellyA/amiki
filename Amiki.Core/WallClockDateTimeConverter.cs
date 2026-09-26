using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amiki.Core;

/// <summary>
/// Sends dates as plain wall-clock values ("2026-09-26T00:00:00"), no time zone attached.
///
/// Amiki's dates mean "your day": a task due Friday is due on your Friday. With the default
/// JSON behaviour the browser sends Friday 00:00+08:00, and a server running in UTC reads that
/// as Thursday 16:00, which moves the task to the wrong day. Both client and server use this
/// converter, so the value that leaves the browser is the value that gets stored.
/// </summary>
public sealed class WallClockDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString() ?? throw new JsonException("Expected a date.");
        // Tolerate a trailing offset from older clients by keeping the wall-clock part only.
        var parsed = DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var withOffset)
            ? withOffset.DateTime
            : DateTime.Parse(text, CultureInfo.InvariantCulture);
        return DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}
