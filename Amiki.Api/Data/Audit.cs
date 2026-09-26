using System.Globalization;
using System.Text.Json;
using Amiki.Core;

namespace Amiki.Api.Data;

/// <summary>Builds audit log entries: a readable summary plus the item before and after.</summary>
public static class Audit
{
    private static readonly JsonSerializerOptions Json = AmikiJson.Create();

    // Bookkeeping fields, and values calculated from other fields (their change is already shown).
    private static readonly HashSet<string> Ignored = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt", "updatedAt", "signed", "totalOut", "difference", "matched",
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json);

    /// <summary>
    /// Whether anything real changed. Compares values, not text: the database hands back 15.00
    /// where the app sent 15, and a retried save must not show up as an edit.
    /// </summary>
    public static bool HasChanges(string? before, string? after) => before is null || after is null || Changes(before, after).Count > 0;

    public static AuditEntry Entry(string entity, Guid id, string action, string summary, string? before, string? after) => new()
    {
        At = DateTime.Now,
        Entity = entity,
        EntityId = id,
        Action = action,
        Summary = action == "edited" && Changes(before, after) is { Count: > 0 } changes
            ? $"{summary} · changed {string.Join(", ", changes)}"
            : summary,
        Before = before,
        After = after,
    };

    /// <summary>"amount 2,000 → 2,500", "note "Lunch" → "Lunch with org"", from the two JSON snapshots.</summary>
    private static List<string> Changes(string? before, string? after)
    {
        if (before is null || after is null) return [];
        using var a = JsonDocument.Parse(before);
        using var b = JsonDocument.Parse(after);
        var changes = new List<string>();
        foreach (var property in b.RootElement.EnumerateObject())
        {
            if (Ignored.Contains(property.Name)) continue;
            var had = a.RootElement.TryGetProperty(property.Name, out var old);
            if (had && Same(old, property.Value)) continue;
            changes.Add($"{property.Name} {(had ? Show(old) : "(none)")} → {Show(property.Value)}");
        }
        return changes;
    }

    private static bool Same(JsonElement x, JsonElement y) =>
        x.ValueKind == JsonValueKind.Number && y.ValueKind == JsonValueKind.Number
            ? x.GetDecimal() == y.GetDecimal()
            : x.GetRawText() == y.GetRawText();

    private static string Show(JsonElement value)
    {
        var text = value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal().ToString("#,0.##", CultureInfo.InvariantCulture)
            : value.GetRawText();
        return text.Length > 60 ? text[..57] + "…" : text;
    }
}
