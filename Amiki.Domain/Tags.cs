namespace Amiki.Domain;

/// <summary>Tags and project names: lowercase, without the '#', one word ("#CMSC128" → "cmsc128").</summary>
public static class Tags
{
    /// <summary>The tag in its stored form, or null if it isn't a usable tag (empty, or several words).</summary>
    public static string? Clean(string? value)
    {
        var tag = value?.Trim().TrimStart('#').ToLowerInvariant();
        return string.IsNullOrEmpty(tag) || tag.Contains(' ') ? null : tag;
    }
}
