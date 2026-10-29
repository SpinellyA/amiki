using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Amiki.Application.Assistant.Tools;

/// <summary>
/// The arguments the model chose for a tool, read safely: anything missing or malformed becomes
/// a <see cref="ToolException"/> the model sees and can correct.
/// </summary>
public sealed class ToolArguments(JsonObject args)
{
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>The longest date range a lookup accepts.</summary>
    public const int MaxRangeDays = 400;

    /// <summary>Trimmed text, or null when missing or blank.</summary>
    public string? Text(string key) =>
        args[key] is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s.Trim() : null;

    /// <summary>Text that must be there; cut to <paramref name="max"/> characters.</summary>
    public string RequiredText(string key, int max)
    {
        var value = Text(key) ?? throw new ToolException($"'{key}' is required.");
        return value.Length <= max ? value : value[..max];
    }

    public DateTime? Date(string key) =>
        Text(key) is { } text
            ? DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d
                : throw new ToolException($"'{key}' must be a date like 2026-10-21.")
            : null;

    public bool Flag(string key) => args[key] is JsonValue v && v.TryGetValue<bool>(out var b) && b;

    public int? Number(string key) => Amount(key) is { } d && d == Math.Floor(d) && d is >= int.MinValue and <= int.MaxValue ? (int)d : null;

    /// <summary>A number, however the model wrote it: 1200, 1200.5, or "1,200" as text.</summary>
    public decimal? Amount(string key) => args[key] switch
    {
        JsonValue v when v.GetValueKind() == JsonValueKind.Number =>
            decimal.TryParse(v.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null,
        JsonValue v when v.TryGetValue<string>(out var s) =>
            decimal.TryParse(s.Replace(",", "").Replace("₱", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null,
        _ => null,
    };

    public IReadOnlyList<string> TextList(string key) =>
        args[key] is JsonArray array
            ? array.Select(t => t is JsonValue v && v.TryGetValue<string>(out var s) ? s : null).OfType<string>().ToList()
            : [];

    /// <summary>
    /// "from" and "to" as [from, day after to). Without them, this month so far (or from
    /// <paramref name="defaultFrom"/>). At most about a year.
    /// </summary>
    public (DateTime From, DateTime To) DateRange(DateTime today, DateTime? defaultFrom = null)
    {
        var from = Date("from") ?? defaultFrom ?? new DateTime(today.Year, today.Month, 1);
        var to = (Date("to") ?? today).AddDays(1);
        if (to <= from) throw new ToolException("'to' must be on or after 'from'.");
        if ((to - from).TotalDays > MaxRangeDays) throw new ToolException("That range is too long; ask for a year or less.");
        return (from, to);
    }
}
