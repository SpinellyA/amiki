using System.Globalization;
using System.Text.Json.Nodes;

namespace Amiki.Application.Assistant.Tools;

/// <summary>The shape lookups answer the model with: a capped list of rows, dates spelled out.</summary>
public static class ToolResult
{
    /// <summary>Most rows a lookup sends, so one answer can't eat the model's whole budget.</summary>
    public const int MaxRows = 40;

    public static JsonObject Rows(IEnumerable<JsonObject> rows)
    {
        var list = rows.Take(MaxRows + 1).ToList();
        var shown = list.Take(MaxRows).ToList();
        return new JsonObject
        {
            ["count"] = shown.Count,
            ["items"] = new JsonArray(shown.Cast<JsonNode>().ToArray()),
            ["note"] = list.Count > MaxRows ? $"Only the first {MaxRows} are shown." : null,
        };
    }

    /// <summary>"2026-10-21 (Wed)": the weekday saves the model working it out (and getting it wrong).</summary>
    public static string? Day(DateTime? date) => date?.ToString("yyyy-MM-dd (ddd)", CultureInfo.InvariantCulture);
}
