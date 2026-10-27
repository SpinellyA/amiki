namespace Amiki.Domain;

/// <summary>Matching a name someone (or the model) typed to one that really exists, like an account or category.</summary>
public static class KnownNames
{
    /// <summary>The real name, matched without caring about case ("gcash" → "GCash"), or null if there's no such thing.</summary>
    public static string? Match(string? value, IEnumerable<string> known) =>
        value is null ? null : known.FirstOrDefault(k => string.Equals(k, value.Trim(), StringComparison.OrdinalIgnoreCase));
}
