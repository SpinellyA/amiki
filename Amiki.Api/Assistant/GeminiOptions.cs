namespace Amiki.Api.Assistant;

/// <summary>
/// Settings under "Gemini" (environment variables Gemini__ApiKey, Gemini__Model). Without a key,
/// the assistant features are simply off.
/// </summary>
public sealed class GeminiOptions
{
    public string? ApiKey { get; set; }

    /// <summary>
    /// Any model your key can use. The default is Google's "latest Flash" alias, so a retired model
    /// can't break Amiki (gemini-2.5-flash was closed to new keys in 2026); pin an exact one like
    /// "gemini-3.8-flash" if you'd rather her behaviour never shifts.
    /// </summary>
    public string Model { get; set; } = "gemini-flash-latest";

    public bool Enabled => !string.IsNullOrWhiteSpace(ApiKey);
}
