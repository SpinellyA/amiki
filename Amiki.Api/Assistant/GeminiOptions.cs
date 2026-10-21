namespace Amiki.Api.Assistant;

/// <summary>
/// Settings under "Gemini" (environment variables Gemini__ApiKey, Gemini__Model). Without a key,
/// the assistant features are simply off.
/// </summary>
public sealed class GeminiOptions
{
    public string? ApiKey { get; set; }

    /// <summary>Any model your key can use; check Google AI Studio for the current free-tier ones.</summary>
    public string Model { get; set; } = "gemini-2.5-flash";

    public bool Enabled => !string.IsNullOrWhiteSpace(ApiKey);
}
