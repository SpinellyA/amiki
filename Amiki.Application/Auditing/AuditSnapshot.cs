using System.Globalization;
using System.Text.Json;
using Amiki.Domain;

namespace Amiki.Application.Auditing;

/// <summary>An item as it was at one moment, kept as JSON in the audit log.</summary>
public sealed class AuditSnapshot
{
    private static readonly JsonSerializerOptions Options = AmikiJson.Create();

    // Bookkeeping fields, and values calculated from other fields (their change is already shown).
    private static readonly HashSet<string> Ignored = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt", "updatedAt", "signed", "totalOut", "difference", "matched",
    };

    public string Json { get; }

    private AuditSnapshot(string json) => Json = json;

    public static AuditSnapshot Of<T>(T item) => new(JsonSerializer.Serialize(item, Options));

    /// <summary>
    /// Whether anything real changed. Compares values, not text: the database hands back 15.00
    /// where the app sent 15, and a retried save must not show up as an edit.
    /// </summary>
    public bool DiffersFrom(AuditSnapshot before) => ChangesSince(before).Count > 0;

    /// <summary>"amount 2,000 → 2,500", "note "Lunch" → "Lunch with org"".</summary>
    public IReadOnlyList<string> ChangesSince(AuditSnapshot before)
    {
        using var a = JsonDocument.Parse(before.Json);
        using var b = JsonDocument.Parse(Json);
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
